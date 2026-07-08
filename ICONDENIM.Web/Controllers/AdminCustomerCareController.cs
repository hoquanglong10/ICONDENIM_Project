using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;

[AdminAuthorize]
public class AdminCustomerCareController : Controller
{
    private readonly AppDbContext _db;
    public AdminCustomerCareController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index(string? loaiYeuCau, string? trangThai)
    {
        var query = _db.ChamSocKhachHangs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(loaiYeuCau)) query = query.Where(x => x.loaiYeuCau == loaiYeuCau);
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.trangThai == trangThai);
        ViewBag.LoaiYeuCau = loaiYeuCau;
        ViewBag.TrangThai = trangThai;
        return View(await query.OrderByDescending(x => x.ngayGui).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRequest(int id, string trangThai)
    {
        var request = await _db.ChamSocKhachHangs.FindAsync(id);
        if (request == null) return NotFound();
        request.trangThai = trangThai;
        request.ngayXuLy = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật trạng thái yêu cầu khách hàng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Reviews(string? trangThai)
    {
        var query = _db.DanhGias.AsQueryable();
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.trangThai == trangThai);
        ViewBag.TrangThai = trangThai;
        return View(await query.OrderByDescending(x => x.ngayTao).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> UpdateReview(int id, string trangThai)
    {
        var review = await _db.DanhGias.FindAsync(id);
        if (review == null) return NotFound();
        review.trangThai = trangThai;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật trạng thái đánh giá sản phẩm.";
        return RedirectToAction(nameof(Reviews));
    }

    public async Task<IActionResult> Warranties(string? trangThai)
    {
        var query = _db.BaoHanhs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.trangThai == trangThai);
        ViewBag.TrangThai = trangThai;
        return View(await query.OrderByDescending(x => x.ngayTiepNhan).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> UpdateWarranty(int id, string trangThai)
    {
        var warranty = await _db.BaoHanhs.FindAsync(id);
        if (warranty == null) return NotFound();
        warranty.trangThai = trangThai;
        if (trangThai == "HoanTat") warranty.ngayHoanTat = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật trạng thái bảo hành.";
        return RedirectToAction(nameof(Warranties));
    }
}
