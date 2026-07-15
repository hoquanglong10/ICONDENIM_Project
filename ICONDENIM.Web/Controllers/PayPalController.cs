using System.Text.Json;
using ICONDENIM.Web.Configuration;
using ICONDENIM.Web.Data;
using ICONDENIM.Web.Models;
using ICONDENIM.Web.Services;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ICONDENIM.Web.Controllers;

[Route("paypal")]
public sealed class PayPalController : Controller
{
    private const string CartSessionKey = "ICONDENIM_CART";
    private const string PendingPayPalOrderSessionKey = "PAYPAL_PENDING_INTERNAL_ORDER_ID";

    private readonly AppDbContext _db;
    private readonly PayPalService _payPal;
    private readonly PayPalOptions _options;
    private readonly ILogger<PayPalController> _logger;

    public PayPalController(
        AppDbContext db,
        PayPalService payPal,
        IOptions<PayPalOptions> options,
        ILogger<PayPalController> logger)
    {
        _db = db;
        _payPal = payPal;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("create-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(
        [FromBody] PayPalCreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!_payPal.IsConfigured)
            return BadRequest(new { message = "PayPal Sandbox chưa được cấu hình Client ID/Secret." });

        if (!TryValidateModel(request))
            return BadRequest(new { message = FirstModelError() });

        await ReleaseExpiredPayPalOrdersAsync(cancellationToken);
        await CancelPendingOrderInCurrentSessionAsync("Tạo giao dịch PayPal mới", cancellationToken);

        var cart = await BuildCartViewModelAsync(cancellationToken);
        if (!cart.Items.Any())
            return BadRequest(new { message = "Giỏ hàng đang trống." });

        foreach (var item in cart.Items)
        {
            if (item.soLuong <= 0 || item.soLuong > item.soLuongTon)
                return BadRequest(new { message = $"Sản phẩm {item.tenSanPham} không đủ tồn kho." });
        }

        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        var discountInfo = await CalculateDiscountAsync(request.MaKhuyenMai, cart.TongTienHang, userId, cancellationToken);
        var orderTotalVnd = cart.TongTienHang + cart.PhiVanChuyen - discountInfo.Discount;
        var amountPayPal = ConvertVndToPayPalAmount(orderTotalVnd);
        var orderCode = GenerateOrderCode();

        PayPalCreateOrderResult paypalOrder;
        try
        {
            paypalOrder = await _payPal.CreateOrderAsync(
                amountPayPal,
                orderCode,
                $"ICONDENIM - {orderCode}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tạo PayPal order cho {OrderCode}", orderCode);
            return BadRequest(new { message = "Không thể kết nối PayPal Sandbox. Kiểm tra Client ID, Secret và kết nối mạng." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = new DonHang
            {
                maDonHang = orderCode,
                userID = userId,
                khuyenMaiID = discountInfo.Promotion?.khuyenMaiID,
                hoTenNhan = request.HoTenNhan.Trim(),
                sdtNhan = request.SdtNhan.Trim(),
                diaChiNhan = request.DiaChiNhan.Trim(),
                tenKhachVangLai = userId.HasValue ? null : request.HoTenNhan.Trim(),
                sdtKhachVangLai = userId.HasValue ? null : request.SdtNhan.Trim(),
                ngayDat = DateTime.Now,
                trangThaiDonHang = "ChoXacNhan",
                trangThaiThanhToan = "ChuaThanhToan",
                trangThaiGiaoHang = "ChuaGiao",
                phuongThucThanhToan = "PayPal",
                daThuTien = false,
                tongTienHang = cart.TongTienHang,
                phiVanChuyen = cart.PhiVanChuyen,
                giamGia = discountInfo.Discount,
                ghiChu = $"PayPal Sandbox đang chờ duyệt. PayPal Order ID: {paypalOrder.Id}",
                ngayCapNhat = DateTime.Now
            };

            _db.DonHangs.Add(order);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var item in cart.Items)
            {
                var variant = await _db.BienTheSanPhams
                    .Include(x => x.SanPham)
                    .FirstOrDefaultAsync(x => x.bienTheID == item.bienTheID, cancellationToken);

                if (variant?.SanPham == null || !variant.trangThai || !variant.SanPham.choPhepHienThi ||
                    variant.SanPham.trangThai != "DangBan" || variant.soLuongTon < item.soLuong)
                {
                    throw new InvalidOperationException($"Tồn kho của {item.tenSanPham} vừa thay đổi. Vui lòng thử lại.");
                }

                var stockBefore = variant.soLuongTon;
                variant.soLuongTon -= item.soLuong;

                _db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    donHangID = order.donHangID,
                    bienTheID = item.bienTheID,
                    tenSanPhamSnapshot = item.tenSanPham,
                    skuSnapshot = item.sku,
                    sizeSnapshot = item.size,
                    mauSacSnapshot = item.mauSac,
                    soLuong = item.soLuong,
                    donGia = item.donGia,
                    commentPro = "Giữ tồn kho chờ thanh toán PayPal Sandbox"
                });

                _db.LichSuTonKhos.Add(new LichSuTonKho
                {
                    bienTheID = item.bienTheID,
                    loaiGiaoDich = "GiuKhoPayPal",
                    soLuongThayDoi = -item.soLuong,
                    soLuongTruoc = stockBefore,
                    soLuongSau = variant.soLuongTon,
                    donHangID = order.donHangID,
                    ghiChu = "Giữ kho khi khởi tạo PayPal Order",
                    ngayTao = DateTime.Now
                });
            }

            _db.ThanhToans.Add(new ThanhToan
            {
                donHangID = order.donHangID,
                phuongThuc = "PayPal",
                soTien = orderTotalVnd,
                trangThai = "ChoThanhToan",
                maGiaoDich = paypalOrder.Id,
                noiDungThanhToan = $"PayPal Sandbox; số tiền {amountPayPal:0.00} {_payPal.Currency}; tỷ giá demo {_options.VndPerUsd:0.##} VND/USD",
                thoiGianThanhToan = null
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            HttpContext.Session.SetInt32(PendingPayPalOrderSessionKey, order.donHangID);

            return Ok(new
            {
                paypalOrderId = paypalOrder.Id,
                internalOrderId = order.donHangID,
                amount = amountPayPal.ToString("0.00"),
                currency = _payPal.Currency
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Không thể tạo đơn nội bộ cho PayPal order {PayPalOrderId}", paypalOrder.Id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("capture-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureOrder(
        [FromBody] PayPalCaptureOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateModel(request))
            return BadRequest(new { message = FirstModelError() });

        var pendingInternalOrderId = HttpContext.Session.GetInt32(PendingPayPalOrderSessionKey);
        if (!pendingInternalOrderId.HasValue || pendingInternalOrderId.Value != request.InternalOrderId)
            return BadRequest(new { message = "Phiên thanh toán PayPal không hợp lệ hoặc đã hết hạn." });

        var order = await _db.DonHangs.FirstOrDefaultAsync(x => x.donHangID == request.InternalOrderId, cancellationToken);
        var payment = await _db.ThanhToans.FirstOrDefaultAsync(x => x.donHangID == request.InternalOrderId && x.phuongThuc == "PayPal", cancellationToken);

        if (order == null || payment == null || !string.Equals(payment.maGiaoDich, request.PayPalOrderId, StringComparison.Ordinal))
            return BadRequest(new { message = "Không tìm thấy giao dịch PayPal tương ứng." });

        if (order.trangThaiThanhToan == "DaThanhToan" && order.daThuTien)
        {
            ClearCart();
            HttpContext.Session.Remove(PendingPayPalOrderSessionKey);
            return Ok(new { success = true, redirectUrl = Url.Action("Success", "Cart", new { id = order.donHangID }) });
        }

        PayPalCaptureOrderResult capture;
        try
        {
            capture = await _payPal.CaptureOrderAsync(request.PayPalOrderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capture PayPal thất bại cho {PayPalOrderId}", request.PayPalOrderId);
            return BadRequest(new { message = "PayPal chưa xác nhận thanh toán. Bạn có thể thử lại hoặc chọn phương thức khác." });
        }

        if (!string.Equals(capture.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(capture.CaptureStatus) &&
             !string.Equals(capture.CaptureStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { message = $"PayPal trả về trạng thái {capture.Status}. Giao dịch chưa hoàn tất." });
        }

        var expectedAmount = ConvertVndToPayPalAmount(payment.soTien);
        if (!string.Equals(capture.Currency, _payPal.Currency, StringComparison.OrdinalIgnoreCase) ||
            !capture.CapturedAmount.HasValue ||
            Math.Abs(capture.CapturedAmount.Value - expectedAmount) > 0.01m)
        {
            _logger.LogCritical(
                "PayPal amount mismatch. InternalOrder={InternalOrderId}, expected={Expected} {Currency}, actual={Actual} {ActualCurrency}",
                request.InternalOrderId,
                expectedAmount,
                _payPal.Currency,
                capture.CapturedAmount,
                capture.Currency);
            return BadRequest(new { message = "Số tiền PayPal trả về không khớp đơn hàng. Vui lòng liên hệ quản trị viên." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            order.trangThaiThanhToan = "DaThanhToan";
            order.daThuTien = true;
            order.ngayCapNhat = DateTime.Now;
            order.ghiChu = $"PayPal Sandbox thanh toán thành công. Order: {request.PayPalOrderId}; Capture: {capture.CaptureId}";

            payment.trangThai = "DaThanhToan";
            payment.maGiaoDich = BuildTransactionCode(request.PayPalOrderId, capture.CaptureId);
            payment.noiDungThanhToan = $"PayPal Sandbox COMPLETED; {capture.CapturedAmount:0.00} {capture.Currency}; PayPal Order {request.PayPalOrderId}; Capture {capture.CaptureId}";
            payment.thoiGianThanhToan = DateTime.Now;

            if (order.khuyenMaiID.HasValue)
            {
                var promotion = await _db.KhuyenMais.FindAsync(new object[] { order.khuyenMaiID.Value }, cancellationToken);
                if (promotion != null) promotion.daSuDung += 1;
            }

            if (order.userID.HasValue)
            {
                var user = await _db.NguoiDungs.FindAsync(new object[] { order.userID.Value }, cancellationToken);
                if (user != null) user.diemTichLuy += (int)Math.Floor(order.thanhTien / 100000m);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            ClearCart();
            HttpContext.Session.Remove(PendingPayPalOrderSessionKey);
            TempData["Success"] = $"Thanh toán PayPal thành công. Mã đơn hàng: {order.maDonHang}.";

            return Ok(new
            {
                success = true,
                redirectUrl = Url.Action("Success", "Cart", new { id = order.donHangID })
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogCritical(ex, "PayPal đã capture nhưng không cập nhật được DB cho đơn {InternalOrderId}", request.InternalOrderId);
            return StatusCode(500, new
            {
                message = "PayPal đã ghi nhận giao dịch nhưng hệ thống chưa cập nhật được đơn hàng. Không thanh toán lại; hãy liên hệ quản trị viên."
            });
        }
    }

    [HttpPost("cancel-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(
        [FromBody] PayPalCancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var pendingInternalOrderId = HttpContext.Session.GetInt32(PendingPayPalOrderSessionKey);
        if (!pendingInternalOrderId.HasValue || pendingInternalOrderId.Value != request.InternalOrderId)
            return Ok(new { success = true });

        await CancelPendingOrderAsync(request.InternalOrderId, request.PayPalOrderId, "Người mua hủy cửa sổ PayPal", cancellationToken);
        HttpContext.Session.Remove(PendingPayPalOrderSessionKey);
        return Ok(new { success = true });
    }

    private async Task CancelPendingOrderInCurrentSessionAsync(string reason, CancellationToken cancellationToken)
    {
        var pendingOrderId = HttpContext.Session.GetInt32(PendingPayPalOrderSessionKey);
        if (!pendingOrderId.HasValue) return;

        var payment = await _db.ThanhToans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.donHangID == pendingOrderId.Value && x.phuongThuc == "PayPal", cancellationToken);

        await CancelPendingOrderAsync(pendingOrderId.Value, payment?.maGiaoDich, reason, cancellationToken);
        HttpContext.Session.Remove(PendingPayPalOrderSessionKey);
    }

    private async Task CancelPendingOrderAsync(
        int internalOrderId,
        string? paypalOrderId,
        string reason,
        CancellationToken cancellationToken)
    {
        var order = await _db.DonHangs
            .Include(x => x.ChiTietDonHangs)
            .FirstOrDefaultAsync(x => x.donHangID == internalOrderId, cancellationToken);

        if (order == null || order.trangThaiThanhToan == "DaThanhToan" || order.trangThaiDonHang == "DaHuy")
            return;

        var payment = await _db.ThanhToans.FirstOrDefaultAsync(x => x.donHangID == internalOrderId && x.phuongThuc == "PayPal", cancellationToken);
        if (payment != null && !string.IsNullOrWhiteSpace(paypalOrderId) &&
            !string.Equals(payment.maGiaoDich, paypalOrderId, StringComparison.Ordinal))
            return;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var detail in order.ChiTietDonHangs ?? Array.Empty<ChiTietDonHang>())
            {
                var variant = await _db.BienTheSanPhams.FindAsync(new object[] { detail.bienTheID }, cancellationToken);
                if (variant == null) continue;

                var stockBefore = variant.soLuongTon;
                variant.soLuongTon += detail.soLuong;
                _db.LichSuTonKhos.Add(new LichSuTonKho
                {
                    bienTheID = detail.bienTheID,
                    loaiGiaoDich = "HuyPayPal",
                    soLuongThayDoi = detail.soLuong,
                    soLuongTruoc = stockBefore,
                    soLuongSau = variant.soLuongTon,
                    donHangID = order.donHangID,
                    ghiChu = reason,
                    ngayTao = DateTime.Now
                });
            }

            order.trangThaiDonHang = "DaHuy";
            order.trangThaiThanhToan = "ThanhToanThatBai";
            order.ngayCapNhat = DateTime.Now;
            order.ghiChu = $"{reason}. Đã hoàn lại tồn kho.";

            if (payment != null)
            {
                payment.trangThai = "DaHuy";
                payment.noiDungThanhToan = $"{reason}. PayPal Order: {payment.maGiaoDich}";
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ReleaseExpiredPayPalOrdersAsync(CancellationToken cancellationToken)
    {
        var timeoutMinutes = Math.Clamp(_options.PendingOrderTimeoutMinutes, 10, 1440);
        var cutoff = DateTime.Now.AddMinutes(-timeoutMinutes);
        var expiredIds = await _db.DonHangs
            .Where(x => x.phuongThucThanhToan == "PayPal" &&
                        x.trangThaiThanhToan == "ChuaThanhToan" &&
                        x.trangThaiDonHang == "ChoXacNhan" &&
                        x.ngayDat < cutoff)
            .Select(x => x.donHangID)
            .ToListAsync(cancellationToken);

        foreach (var orderId in expiredIds)
        {
            try
            {
                await CancelPendingOrderAsync(orderId, null, "PayPal Order hết thời gian chờ", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể giải phóng PayPal order hết hạn {OrderId}", orderId);
            }
        }
    }

    private List<CartSessionItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        return string.IsNullOrWhiteSpace(json)
            ? new List<CartSessionItem>()
            : JsonSerializer.Deserialize<List<CartSessionItem>>(json) ?? new List<CartSessionItem>();
    }

    private void ClearCart()
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(new List<CartSessionItem>()));
        HttpContext.Session.SetInt32("CartCount", 0);
    }

    private async Task<CartViewModel> BuildCartViewModelAsync(CancellationToken cancellationToken)
    {
        var cart = GetCart();
        var ids = cart.Select(x => x.bienTheID).ToList();
        var variants = await _db.BienTheSanPhams
            .Include(x => x.SanPham)
            .Where(x => ids.Contains(x.bienTheID))
            .ToListAsync(cancellationToken);

        var vm = new CartViewModel();
        foreach (var cartItem in cart)
        {
            var variant = variants.FirstOrDefault(x => x.bienTheID == cartItem.bienTheID);
            if (variant?.SanPham == null) continue;

            vm.Items.Add(new CartItemViewModel
            {
                bienTheID = variant.bienTheID,
                sanPhamID = variant.sanPhamID,
                tenSanPham = variant.SanPham.tenSanPham,
                sku = variant.sku,
                size = variant.size,
                mauSac = variant.mauSac,
                hinhAnh = variant.hinhAnh ?? variant.SanPham.hinhAnhDaiDien,
                soLuong = Math.Max(cartItem.soLuong, 1),
                soLuongTon = variant.soLuongTon,
                donGia = variant.giaKhuyenMai ?? variant.giaBan
            });
        }

        return vm;
    }

    private async Task<(KhuyenMai? Promotion, decimal Discount)> CalculateDiscountAsync(
        string? code,
        decimal subtotal,
        int? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, 0);

        var today = DateTime.Today;
        var promotion = await _db.KhuyenMais.FirstOrDefaultAsync(x =>
            x.maCode == code.Trim() &&
            x.trangThai &&
            x.ngayBatDau <= today &&
            x.ngayKetThuc >= today,
            cancellationToken);

        if (promotion == null || subtotal < promotion.dieuKienToiThieu ||
            (promotion.soLuotSuDung > 0 && promotion.daSuDung >= promotion.soLuotSuDung))
            return (null, 0);

        if (promotion.apDungLoaiKhachHangID.HasValue)
        {
            if (!userId.HasValue) return (null, 0);
            var userType = await _db.NguoiDungs
                .Where(x => x.userID == userId.Value)
                .Select(x => x.loaiKhachHangID)
                .FirstOrDefaultAsync(cancellationToken);
            if (userType != promotion.apDungLoaiKhachHangID) return (null, 0);
        }

        var discount = promotion.loaiGiam == "PhanTram"
            ? subtotal * promotion.giaTriGiam / 100m
            : promotion.giaTriGiam;
        if (promotion.giaTriGiamToiDa.HasValue)
            discount = Math.Min(discount, promotion.giaTriGiamToiDa.Value);

        return (promotion, Math.Min(discount, subtotal));
    }

    private decimal ConvertVndToPayPalAmount(decimal amountVnd)
    {
        var rate = _options.VndPerUsd > 0 ? _options.VndPerUsd : 25000m;
        return Math.Max(1m, Math.Round(amountVnd / rate, 2, MidpointRounding.AwayFromZero));
    }

    private string FirstModelError() =>
        ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault()
        ?? "Dữ liệu thanh toán không hợp lệ.";

    private static string BuildTransactionCode(string orderId, string? captureId)
    {
        var code = $"PPORD:{orderId};CAP:{captureId}";
        return code.Length <= 100 ? code : code[..100];
    }

    private static string GenerateOrderCode() => "DH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
}
