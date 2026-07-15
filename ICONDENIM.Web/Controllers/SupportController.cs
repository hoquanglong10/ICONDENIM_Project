using ICONDENIM.Web.Data;
using ICONDENIM.Web.Models;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;

public class SupportController : Controller
{
    private readonly AppDbContext _db;
    public SupportController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Create(int? sanPhamID = null, int? donHangID = null)
    {
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        if (!userId.HasValue)
        {
            TempData["Error"] = "Vui lòng đăng nhập để gửi yêu cầu hỗ trợ, đổi trả hoặc bảo hành.";
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(Create), "Support", new { sanPhamID, donHangID })
            });
        }

        var vm = new SupportRequestViewModel
        {
            sanPhamID = sanPhamID,
            donHangID = donHangID,
            loaiYeuCau = donHangID.HasValue && sanPhamID.HasValue ? "DoiTra" : "HoiDap"
        };
        await LoadSupportOptionsAsync(vm, userId.Value);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupportRequestViewModel vm)
    {
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        if (!userId.HasValue)
        {
            TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Account");
        }

        var allowedTypes = new[] { "HoiDap", "DonHang", "DoiTra", "BaoHanh" };
        if (!allowedTypes.Contains(vm.loaiYeuCau))
            ModelState.AddModelError(nameof(vm.loaiYeuCau), "Loại yêu cầu không hợp lệ.");

        DonHang? selectedOrder = null;
        if (vm.loaiYeuCau is "DonHang" or "DoiTra" or "BaoHanh")
        {
            if (!vm.donHangID.HasValue)
            {
                ModelState.AddModelError(nameof(vm.donHangID), "Vui lòng chọn đơn hàng liên quan.");
            }
            else
            {
                selectedOrder = await _db.DonHangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.donHangID == vm.donHangID.Value && x.userID == userId.Value);
                if (selectedOrder == null)
                    ModelState.AddModelError(nameof(vm.donHangID), "Đơn hàng không thuộc tài khoản của bạn.");
            }
        }

        if (vm.loaiYeuCau is "DoiTra" or "BaoHanh")
        {
            if (!vm.sanPhamID.HasValue)
            {
                ModelState.AddModelError(nameof(vm.sanPhamID), "Vui lòng chọn sản phẩm cần đổi trả/bảo hành.");
            }
            else if (selectedOrder != null)
            {
                var productBelongsToOrder = await _db.ChiTietDonHangs
                    .AsNoTracking()
                    .AnyAsync(x => x.donHangID == selectedOrder.donHangID &&
                                   x.BienTheSanPham != null &&
                                   x.BienTheSanPham.sanPhamID == vm.sanPhamID.Value);
                if (!productBelongsToOrder)
                    ModelState.AddModelError(nameof(vm.sanPhamID), "Sản phẩm không thuộc đơn hàng đã chọn.");
            }
        }

        if (vm.loaiYeuCau == "HoiDap" && vm.sanPhamID.HasValue)
        {
            var productExists = await _db.SanPhams.AsNoTracking()
                .AnyAsync(x => x.sanPhamID == vm.sanPhamID.Value && x.choPhepHienThi);
            if (!productExists)
                ModelState.AddModelError(nameof(vm.sanPhamID), "Sản phẩm được hỏi không còn tồn tại hoặc không hiển thị.");
        }

        if (!ModelState.IsValid)
        {
            await LoadSupportOptionsAsync(vm, userId.Value);
            return View(vm);
        }

        _db.ChamSocKhachHangs.Add(new ChamSocKhachHang
        {
            userID = userId.Value,
            sanPhamID = vm.sanPhamID,
            donHangID = vm.donHangID,
            loaiYeuCau = vm.loaiYeuCau,
            noiDung = vm.noiDung.Trim(),
            trangThai = "Moi",
            ngayGui = DateTime.Now
        });

        if (vm.loaiYeuCau == "BaoHanh" && vm.donHangID.HasValue && vm.sanPhamID.HasValue)
        {
            var duplicated = await _db.BaoHanhs.AnyAsync(x =>
                x.userID == userId.Value &&
                x.donHangID == vm.donHangID.Value &&
                x.sanPhamID == vm.sanPhamID.Value &&
                x.trangThai != "HoanTat" && x.trangThai != "TuChoi");

            if (!duplicated)
            {
                _db.BaoHanhs.Add(new BaoHanh
                {
                    userID = userId.Value,
                    donHangID = vm.donHangID.Value,
                    sanPhamID = vm.sanPhamID.Value,
                    lyDo = vm.noiDung.Trim(),
                    trangThai = "TiepNhan",
                    ngayTiepNhan = DateTime.Now
                });
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã gửi yêu cầu chăm sóc khách hàng. Nhân viên sẽ xử lý trong thời gian sớm nhất.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Chat() => View();

    private async Task LoadSupportOptionsAsync(SupportRequestViewModel vm, int userId)
    {
        var user = await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.userID == userId);
        vm.CustomerDisplayName = user?.hoTen ?? "Khách hàng";
        vm.CustomerContact = string.Join(" · ", new[] { user?.soDienThoai, user?.email }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        var orderEntities = await _db.DonHangs.AsNoTracking()
            .Where(x => x.userID == userId)
            .OrderByDescending(x => x.ngayDat)
            .ToListAsync();
        vm.OrderOptions = orderEntities.Select(x => new SupportOrderOptionViewModel
        {
            DonHangID = x.donHangID,
            Label = x.maDonHang + " · " + x.ngayDat.ToString("dd/MM/yyyy") + " · " + x.trangThaiDonHang
        }).ToList();

        var purchasedProducts = await (
            from detail in _db.ChiTietDonHangs.AsNoTracking()
            join order in _db.DonHangs.AsNoTracking() on detail.donHangID equals order.donHangID
            join variant in _db.BienTheSanPhams.AsNoTracking() on detail.bienTheID equals variant.bienTheID
            join product in _db.SanPhams.AsNoTracking() on variant.sanPhamID equals product.sanPhamID
            where order.userID == userId
            orderby order.ngayDat descending
            select new SupportProductOptionViewModel
            {
                SanPhamID = product.sanPhamID,
                DonHangID = order.donHangID,
                Label = product.tenSanPham + " · " + detail.sizeSnapshot + "/" + detail.mauSacSnapshot
            }).ToListAsync();

        var generalProducts = await _db.SanPhams.AsNoTracking()
            .Where(x => x.choPhepHienThi && x.trangThai == "DangBan")
            .OrderBy(x => x.tenSanPham)
            .Select(x => new SupportProductOptionViewModel
            {
                SanPhamID = x.sanPhamID,
                DonHangID = null,
                Label = x.tenSanPham
            })
            .ToListAsync();

        vm.ProductOptions = generalProducts.Concat(purchasedProducts).ToList();
    }
}
