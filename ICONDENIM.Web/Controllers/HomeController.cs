using ICONDENIM.Web.Data;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var products = await _db.SanPhamTrangChuViews
            .Where(x => x.choPhepHienThi && x.trangThai == "DangBan" && (x.tongTonKho > 0 || x.laHangHot))
            .OrderByDescending(x => x.laHangHot).ThenBy(x => x.giaHienThi).ToListAsync();
        var vm = new HomeViewModel
        {
            Banners = await _db.Banners.Where(x => x.trangThai && x.viTri == "TrangChu").OrderBy(x => x.thuTuHienThi).ToListAsync(),
            DanhMucs = await _db.DanhMucs.Where(x => x.trangThai).OrderBy(x => x.thuTuHienThi).ToListAsync(),
            HangHot = products.Where(x => x.laHangHot).Take(8).ToList(),
            HangBanChay = products.Where(x => x.laHangBanChay && x.tongTonKho > 0).Take(8).ToList(),
            TatCaSanPham = products.Take(12).ToList()
        };
        return View(vm);
    }
}
