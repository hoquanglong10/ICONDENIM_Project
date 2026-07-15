using System.Text.Json;
using ICONDENIM.Web.Data;
using ICONDENIM.Web.Models;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "ICONDENIM_CART";
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        return View(await BuildCartViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Add(int bienTheID, int soLuong = 1, bool muaNgay = false)
    {
        if (soLuong <= 0) soLuong = 1;
        var variant = await _db.BienTheSanPhams.Include(x => x.SanPham)
            .FirstOrDefaultAsync(x => x.bienTheID == bienTheID && x.trangThai && x.SanPham != null && x.SanPham.choPhepHienThi && x.SanPham.trangThai == "DangBan");
        if (variant == null)
        {
            TempData["Error"] = "Biến thể sản phẩm không hợp lệ.";
            return RedirectToAction("Index", "Home");
        }
        if (variant.soLuongTon <= 0)
        {
            TempData["Error"] = "Sản phẩm đã cháy hàng, không thể thêm vào giỏ.";
            return RedirectToAction("Details", "Products", new { id = variant.sanPhamID });
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.bienTheID == bienTheID);
        var newQty = (item?.soLuong ?? 0) + soLuong;
        if (newQty > variant.soLuongTon) newQty = variant.soLuongTon;
        if (item == null) cart.Add(new CartSessionItem { bienTheID = bienTheID, soLuong = newQty });
        else item.soLuong = newQty;
        SaveCart(cart);
        TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
        return muaNgay ? RedirectToAction(nameof(Checkout)) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Update(int bienTheID, int soLuong)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.bienTheID == bienTheID);
        if (item != null)
        {
            if (soLuong <= 0) cart.Remove(item);
            else item.soLuong = soLuong;
            SaveCart(cart);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Remove(int bienTheID)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.bienTheID == bienTheID);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewPromotion(string? maKhuyenMai)
    {
        var cart = await BuildCartViewModel();
        if (!cart.Items.Any())
        {
            return Json(new
            {
                success = false,
                message = "Giỏ hàng đang trống.",
                subtotal = 0m,
                shipping = 0m,
                discount = 0m,
                total = 0m
            });
        }

        if (string.IsNullOrWhiteSpace(maKhuyenMai))
        {
            return Json(new
            {
                success = false,
                message = "Vui lòng nhập mã khuyến mãi.",
                subtotal = cart.TongTienHang,
                shipping = cart.PhiVanChuyen,
                discount = 0m,
                total = cart.TongTienHang + cart.PhiVanChuyen
            });
        }

        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        var discountInfo = await CalculateDiscount(maKhuyenMai, cart.TongTienHang, userId);
        if (discountInfo.Promotion == null || discountInfo.Discount <= 0)
        {
            return Json(new
            {
                success = false,
                message = "Mã khuyến mãi không hợp lệ, đã hết hạn, hết lượt hoặc chưa đủ điều kiện áp dụng.",
                subtotal = cart.TongTienHang,
                shipping = cart.PhiVanChuyen,
                discount = 0m,
                total = cart.TongTienHang + cart.PhiVanChuyen
            });
        }

        var total = Math.Max(0m, cart.TongTienHang + cart.PhiVanChuyen - discountInfo.Discount);
        return Json(new
        {
            success = true,
            message = $"Đã áp dụng mã {discountInfo.Promotion.maCode} - {discountInfo.Promotion.tenChuongTrinh}.",
            promotionCode = discountInfo.Promotion.maCode,
            promotionName = discountInfo.Promotion.tenChuongTrinh,
            subtotal = cart.TongTienHang,
            shipping = cart.PhiVanChuyen,
            discount = discountInfo.Discount,
            total
        });
    }

    public async Task<IActionResult> Checkout()
    {
        var cart = await BuildCartViewModel();
        if (!cart.Items.Any())
        {
            TempData["Error"] = "Giỏ hàng đang trống.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new CheckoutViewModel { Cart = cart };
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        if (userId.HasValue)
        {
            var user = await _db.NguoiDungs.FindAsync(userId.Value);
            var address = await _db.DiaChiNguoiDungs.FirstOrDefaultAsync(x => x.userID == userId.Value && x.laMacDinh && x.trangThai);
            if (user != null)
            {
                vm.HoTenNhan = user.hoTen;
                vm.SdtNhan = user.soDienThoai;
                vm.DiaChiNhan = address?.diaChiChiTiet ?? string.Empty;
            }
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        var cart = await BuildCartViewModel();
        vm.Cart = cart;
        if (!cart.Items.Any()) ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống.");
        foreach (var item in cart.Items)
        {
            if (item.soLuong > item.soLuongTon)
                ModelState.AddModelError(string.Empty, $"Sản phẩm {item.tenSanPham} chỉ còn {item.soLuongTon} trong kho.");
        }
        var onlineMethods = new[] { "VNPay", "MoMo", "Banking" };
        if (vm.PhuongThucThanhToan == "PayPal")
            ModelState.AddModelError(nameof(vm.PhuongThucThanhToan), "Vui lòng hoàn tất thanh toán bằng nút PayPal Sandbox trên trang checkout.");
        if (onlineMethods.Contains(vm.PhuongThucThanhToan))
        {
            if (string.IsNullOrWhiteSpace(vm.TaiKhoanThanhToanOnline))
                ModelState.AddModelError(nameof(vm.TaiKhoanThanhToanOnline), "Vui lòng nhập tài khoản thanh toán online demo.");
            if (string.IsNullOrWhiteSpace(vm.MaOtpThanhToan))
                ModelState.AddModelError(nameof(vm.MaOtpThanhToan), "Vui lòng nhập mã xác nhận thanh toán demo.");
        }

        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        var discountInfo = await CalculateDiscount(vm.MaKhuyenMai, cart.TongTienHang, userId);
        cart.GiamGia = discountInfo.Discount;
        if (!string.IsNullOrWhiteSpace(vm.MaKhuyenMai) && discountInfo.Promotion == null)
            ModelState.AddModelError(nameof(vm.MaKhuyenMai), "Mã khuyến mãi không hợp lệ, hết hạn, hết lượt hoặc chưa đủ điều kiện áp dụng.");

        if (!ModelState.IsValid) return View(vm);

        await using var tran = await _db.Database.BeginTransactionAsync();
        try
        {
            var orderTotal = cart.TongTienHang + cart.PhiVanChuyen - discountInfo.Discount;
            var order = new DonHang
            {
                maDonHang = GenerateOrderCode(),
                userID = userId,
                khuyenMaiID = discountInfo.Promotion?.khuyenMaiID,
                hoTenNhan = vm.HoTenNhan.Trim(),
                sdtNhan = vm.SdtNhan.Trim(),
                diaChiNhan = vm.DiaChiNhan.Trim(),
                tenKhachVangLai = userId.HasValue ? null : vm.HoTenNhan.Trim(),
                sdtKhachVangLai = userId.HasValue ? null : vm.SdtNhan.Trim(),
                ngayDat = DateTime.Now,
                trangThaiDonHang = "ChoXacNhan",
                trangThaiGiaoHang = "ChuaGiao",
                phuongThucThanhToan = vm.PhuongThucThanhToan,
                trangThaiThanhToan = vm.PhuongThucThanhToan == "COD" ? "ChuaThanhToan" : "DaThanhToan",
                daThuTien = vm.PhuongThucThanhToan != "COD",
                tongTienHang = cart.TongTienHang,
                phiVanChuyen = cart.PhiVanChuyen,
                giamGia = discountInfo.Discount,
                ghiChu = "Đơn hàng tạo từ website"
            };
            _db.DonHangs.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in cart.Items)
            {
                var variant = await _db.BienTheSanPhams.Include(x => x.SanPham).FirstAsync(x => x.bienTheID == item.bienTheID);
                if (variant.soLuongTon < item.soLuong)
                    throw new InvalidOperationException($"Tồn kho của {item.tenSanPham} không đủ.");

                var tonTruoc = variant.soLuongTon;
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
                    commentPro = "Đặt hàng từ giỏ hàng"
                });
                _db.LichSuTonKhos.Add(new LichSuTonKho
                {
                    bienTheID = item.bienTheID,
                    loaiGiaoDich = "DatHang",
                    soLuongThayDoi = -item.soLuong,
                    soLuongTruoc = tonTruoc,
                    soLuongSau = variant.soLuongTon,
                    donHangID = order.donHangID,
                    ghiChu = "Trừ kho khi đặt hàng thành công",
                    ngayTao = DateTime.Now
                });
            }

            _db.ThanhToans.Add(new ThanhToan
            {
                donHangID = order.donHangID,
                phuongThuc = vm.PhuongThucThanhToan,
                soTien = orderTotal,
                trangThai = vm.PhuongThucThanhToan == "COD" ? "ChoThanhToan" : "DaThanhToan",
                maGiaoDich = vm.PhuongThucThanhToan == "COD" ? null : "DEMO" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                noiDungThanhToan = vm.PhuongThucThanhToan == "COD"
                    ? "Thanh toán khi nhận hàng"
                    : $"Thanh toán trực tuyến demo qua {vm.PhuongThucThanhToan}. Tài khoản: {vm.TaiKhoanThanhToanOnline}",
                thoiGianThanhToan = vm.PhuongThucThanhToan == "COD" ? null : DateTime.Now
            });

            if (discountInfo.Promotion != null) discountInfo.Promotion.daSuDung += 1;
            if (userId.HasValue)
            {
                var user = await _db.NguoiDungs.FindAsync(userId.Value);
                if (user != null) user.diemTichLuy += (int)Math.Floor(orderTotal / 100000m);
            }

            await _db.SaveChangesAsync();
            await tran.CommitAsync();
            SaveCart(new List<CartSessionItem>());
            TempData["Success"] = $"Đặt hàng thành công. Mã đơn hàng của bạn là {order.maDonHang}.";
            return RedirectToAction(nameof(Success), new { id = order.donHangID });
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Không thể tạo đơn hàng: " + ex.Message);
            return View(vm);
        }
    }

    public async Task<IActionResult> Success(int id)
    {
        var order = await _db.DonHangs.Include(x => x.ChiTietDonHangs).FirstOrDefaultAsync(x => x.donHangID == id);
        if (order == null) return NotFound();
        return View(order);
    }

    private List<CartSessionItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        return string.IsNullOrWhiteSpace(json) ? new List<CartSessionItem>() : JsonSerializer.Deserialize<List<CartSessionItem>>(json) ?? new List<CartSessionItem>();
    }

    private void SaveCart(List<CartSessionItem> cart)
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        HttpContext.Session.SetInt32("CartCount", cart.Sum(x => x.soLuong));
    }

    private async Task<CartViewModel> BuildCartViewModel()
    {
        var cart = GetCart();
        var ids = cart.Select(x => x.bienTheID).ToList();
        var variants = await _db.BienTheSanPhams.Include(x => x.SanPham).Where(x => ids.Contains(x.bienTheID)).ToListAsync();
        var vm = new CartViewModel();
        foreach (var ci in cart.ToList())
        {
            var v = variants.FirstOrDefault(x => x.bienTheID == ci.bienTheID);
            if (v?.SanPham == null) continue;
            var qty = Math.Min(Math.Max(ci.soLuong, 1), Math.Max(v.soLuongTon, 1));
            vm.Items.Add(new CartItemViewModel
            {
                bienTheID = v.bienTheID,
                sanPhamID = v.sanPhamID,
                tenSanPham = v.SanPham.tenSanPham,
                sku = v.sku,
                size = v.size,
                mauSac = v.mauSac,
                hinhAnh = v.hinhAnh ?? v.SanPham.hinhAnhDaiDien,
                soLuong = qty,
                soLuongTon = v.soLuongTon,
                donGia = v.giaKhuyenMai ?? v.giaBan
            });
        }
        return vm;
    }

    private async Task<(KhuyenMai? Promotion, decimal Discount)> CalculateDiscount(string? code, decimal subtotal, int? userId)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, 0);
        var today = DateTime.Today;
        var km = await _db.KhuyenMais.FirstOrDefaultAsync(x => x.maCode == code.Trim() && x.trangThai && x.ngayBatDau <= today && x.ngayKetThuc >= today);
        if (km == null || subtotal < km.dieuKienToiThieu || (km.soLuotSuDung > 0 && km.daSuDung >= km.soLuotSuDung))
            return (null, 0);
        if (km.apDungLoaiKhachHangID.HasValue)
        {
            if (!userId.HasValue) return (null, 0);
            var userType = await _db.NguoiDungs.Where(x => x.userID == userId.Value).Select(x => x.loaiKhachHangID).FirstOrDefaultAsync();
            if (userType != km.apDungLoaiKhachHangID) return (null, 0);
        }
        var discount = km.loaiGiam == "PhanTram" ? subtotal * km.giaTriGiam / 100m : km.giaTriGiam;
        if (km.giaTriGiamToiDa.HasValue) discount = Math.Min(discount, km.giaTriGiamToiDa.Value);
        discount = Math.Min(discount, subtotal);
        return (km, discount);
    }

    private static string GenerateOrderCode() => "DH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
}
