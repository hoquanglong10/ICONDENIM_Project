using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;

[AdminAuthorize]
public class AdminPromotionsController : Controller
{
    private readonly AppDbContext _db;
    public AdminPromotionsController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.KhuyenMais.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.maCode.Contains(q) || x.tenChuongTrinh.Contains(q));
        ViewBag.Keyword = q;
        return View(await query.OrderByDescending(x => x.ngayBatDau).ToListAsync());
    }

    public IActionResult Create() => View(new KhuyenMai { ngayBatDau = DateTime.Today, ngayKetThuc = DateTime.Today.AddMonths(1), soLuotSuDung = 100, trangThai = true });

    [HttpPost]
    public async Task<IActionResult> Create(KhuyenMai km)
    {
        if (!ModelState.IsValid) return View(km);
        _db.KhuyenMais.Add(km);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo mã khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var km = await _db.KhuyenMais.FindAsync(id);
        if (km == null) return NotFound();
        return View(km);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(KhuyenMai km)
    {
        if (!ModelState.IsValid) return View(km);
        _db.KhuyenMais.Update(km);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật mã khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var km = await _db.KhuyenMais.FindAsync(id);
        if (km == null) return NotFound();
        km.trangThai = !km.trangThai;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
