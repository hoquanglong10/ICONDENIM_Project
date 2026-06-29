using ICONDENIM.Web.Data;
using ICONDENIM.Web.Models;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ICONDENIM.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) { _db = db; }

    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32("CustomerUserID").HasValue)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var keyword = vm.EmailOrPhone.Trim();
        var user = await _db.NguoiDungs
            .Include(x => x.VaiTro)
            .FirstOrDefaultAsync(x =>
                x.trangThai == "HoatDong" &&
                x.VaiTro != null && x.VaiTro.tenVaiTro == "KhachHang" &&
                ((x.email != null && x.email == keyword) || x.soDienThoai == keyword));

        if (user == null || !PasswordMatches(vm.MatKhau, user.matKhauHash))
        {
            ModelState.AddModelError(string.Empty, "Email/số điện thoại hoặc mật khẩu không đúng.");
            return View(vm);
        }

        HttpContext.Session.SetInt32("CustomerUserID", user.userID);
        HttpContext.Session.SetString("CustomerName", user.hoTen);
        HttpContext.Session.SetString("CustomerEmail", user.email ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("CustomerUserID").HasValue)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var existed = await _db.NguoiDungs.AnyAsync(x => x.email == vm.Email || x.soDienThoai == vm.SoDienThoai);
        if (existed)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc số điện thoại đã được sử dụng.");
            return View(vm);
        }

        var customerRole = await _db.VaiTros.FirstOrDefaultAsync(x => x.tenVaiTro == "KhachHang");
        var defaultCustomerType = await _db.LoaiKhachHangs.FirstOrDefaultAsync(x => x.tenLoai == "VangLai");
        if (customerRole == null)
        {
            ModelState.AddModelError(string.Empty, "Chưa có vai trò KhachHang trong CSDL. Vui lòng kiểm tra dữ liệu mẫu.");
            return View(vm);
        }

        var user = new NguoiDung
        {
            vaiTroID = customerRole.vaiTroID,
            loaiKhachHangID = defaultCustomerType?.loaiKhachHangID,
            hoTen = vm.HoTen.Trim(),
            soDienThoai = vm.SoDienThoai.Trim(),
            email = vm.Email.Trim(),
            matKhauHash = HashDemo(vm.MatKhau),
            ngaySinh = vm.NgaySinh,
            diemTichLuy = 0,
            trangThai = "HoatDong",
            ngayTao = DateTime.Now
        };

        _db.NguoiDungs.Add(user);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(vm.DiaChiGiaoHang))
        {
            _db.DiaChiNguoiDungs.Add(new DiaChiNguoiDung
            {
                userID = user.userID,
                hoTenNhan = user.hoTen,
                sdtNhan = user.soDienThoai,
                diaChiChiTiet = vm.DiaChiGiaoHang.Trim(),
                laMacDinh = true,
                trangThai = true
            });
            await _db.SaveChangesAsync();
        }

        HttpContext.Session.SetInt32("CustomerUserID", user.userID);
        HttpContext.Session.SetString("CustomerName", user.hoTen);
        HttpContext.Session.SetString("CustomerEmail", user.email ?? string.Empty);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("CustomerUserID");
        HttpContext.Session.Remove("CustomerName");
        HttpContext.Session.Remove("CustomerEmail");
        return RedirectToAction("Index", "Home");
    }

    private static string HashDemo(string password) => $"{password}_HASH_DEMO";
    private static bool PasswordMatches(string password, string savedPassword)
        => savedPassword == password || savedPassword == HashDemo(password);
}
