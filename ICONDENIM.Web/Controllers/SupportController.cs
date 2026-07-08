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

    public IActionResult Create(int? sanPhamID = null, int? donHangID = null)
    {
        return View(new SupportRequestViewModel { sanPhamID = sanPhamID, donHangID = donHangID });
    }

    [HttpPost]
    public async Task<IActionResult> Create(SupportRequestViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        _db.ChamSocKhachHangs.Add(new ChamSocKhachHang
        {
            userID = userId,
            sanPhamID = vm.sanPhamID,
            donHangID = vm.donHangID,
            loaiYeuCau = vm.loaiYeuCau,
            noiDung = vm.noiDung,
            trangThai = "Moi",
            ngayGui = DateTime.Now
        });
        if (vm.loaiYeuCau == "BaoHanh" && userId.HasValue && vm.donHangID.HasValue && vm.sanPhamID.HasValue)
        {
            _db.BaoHanhs.Add(new BaoHanh
            {
                userID = userId.Value,
                donHangID = vm.donHangID.Value,
                sanPhamID = vm.sanPhamID.Value,
                lyDo = vm.noiDung,
                trangThai = "TiepNhan",
                ngayTiepNhan = DateTime.Now
            });
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã gửi yêu cầu chăm sóc khách hàng. Nhân viên sẽ xử lý trong thời gian sớm nhất.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Chat()
    {
        return View();
    }
}
