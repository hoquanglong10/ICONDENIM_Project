using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ICONDENIM.Web.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) { _db = db; }

    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32("AdminUserID").HasValue)
            return RedirectToAction(nameof(Index));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var keyword = vm.EmailOrPhone.Trim();
        var admin = await _db.NguoiDungs
            .Include(x => x.VaiTro)
            .FirstOrDefaultAsync(x =>
                x.trangThai == "HoatDong" &&
                x.VaiTro != null && x.VaiTro.tenVaiTro == "Admin" &&
                ((x.email != null && x.email == keyword) || x.soDienThoai == keyword));

        if (admin == null || !PasswordMatches(vm.MatKhau, admin.matKhauHash))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản Admin hoặc mật khẩu không đúng.");
            return View(vm);
        }

        HttpContext.Session.SetInt32("AdminUserID", admin.userID);
        HttpContext.Session.SetString("AdminName", admin.hoTen);
        HttpContext.Session.SetString("AdminEmail", admin.email ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);
        return RedirectToAction(nameof(Index));
    }

    [AdminAuthorize]
    public async Task<IActionResult> Index()
    {
        var vm = new AdminDashboardViewModel
        {
            TongSanPham = await _db.SanPhams.CountAsync(),
            TongNguoiDung = await _db.NguoiDungs.CountAsync(),
            DonChoXacNhan = await _db.DonHangs.CountAsync(x => x.trangThaiDonHang == "ChoXacNhan"),
            DonDangGiao = await _db.DonHangs.CountAsync(x => x.trangThaiGiaoHang == "DangGiao"),
            MaKhuyenMaiDangApDung = await _db.KhuyenMais.CountAsync(x => x.trangThai && x.ngayBatDau <= DateTime.Today && x.ngayKetThuc >= DateTime.Today),
            DoanhThuDaThu = await _db.DonHangs
                .Where(x => x.daThuTien || x.trangThaiThanhToan == "DaThanhToan")
                .SumAsync(x => (decimal?)x.thanhTien) ?? 0
        };
        return View(vm);
    }

    [HttpPost]
    [AdminAuthorize]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("AdminUserID");
        HttpContext.Session.Remove("AdminName");
        HttpContext.Session.Remove("AdminEmail");
        return RedirectToAction(nameof(Login));
    }

    private static string HashDemo(string password) => $"{password}_HASH_DEMO";
    private static bool PasswordMatches(string password, string savedPassword)
        => savedPassword == password || savedPassword == HashDemo(password);
}
