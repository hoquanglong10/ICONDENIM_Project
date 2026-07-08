using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;

[AdminAuthorize]
public class AdminRevenueController : Controller
{
    private readonly AppDbContext _db;
    public AdminRevenueController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string? trangThaiDonHang, string? phuongThucThanhToan)
    {
        var query = _db.DonHangs.AsQueryable();
        if (tuNgay.HasValue) query = query.Where(x => x.ngayDat >= tuNgay.Value);
        if (denNgay.HasValue) query = query.Where(x => x.ngayDat < denNgay.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(trangThaiDonHang)) query = query.Where(x => x.trangThaiDonHang == trangThaiDonHang);
        if (!string.IsNullOrWhiteSpace(phuongThucThanhToan)) query = query.Where(x => x.phuongThucThanhToan == phuongThucThanhToan);
        return View(new RevenueReportViewModel
        {
            TuNgay = tuNgay,
            DenNgay = denNgay,
            TrangThaiDonHang = trangThaiDonHang,
            PhuongThucThanhToan = phuongThucThanhToan,
            DonHangs = await query.OrderByDescending(x => x.ngayDat).ToListAsync()
        });
    }
}
