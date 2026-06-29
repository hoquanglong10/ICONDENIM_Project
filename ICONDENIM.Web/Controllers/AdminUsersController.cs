using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;
[AdminAuthorize]
public class AdminUsersController : Controller
{
    private readonly AppDbContext _db;
    public AdminUsersController(AppDbContext db) { _db = db; }
    private async Task LoadSelects() { ViewBag.VaiTros = new SelectList(await _db.VaiTros.Where(x => x.trangThai).ToListAsync(), "vaiTroID", "tenVaiTro"); ViewBag.LoaiKhachHangs = new SelectList(await _db.LoaiKhachHangs.Where(x => x.trangThai).ToListAsync(), "loaiKhachHangID", "tenLoai"); }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.NguoiDungs.Include(x => x.VaiTro).Include(x => x.LoaiKhachHang).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.hoTen.Contains(q) || x.soDienThoai.Contains(q) || (x.email != null && x.email.Contains(q)));
        ViewBag.Keyword = q; return View(await query.OrderBy(x => x.hoTen).ToListAsync());
    }
    public async Task<IActionResult> Create() { await LoadSelects(); return View(new NguoiDung()); }
    [HttpPost] public async Task<IActionResult> Create(NguoiDung model) { if (!ModelState.IsValid) { await LoadSelects(); return View(model); } model.matKhauHash = string.IsNullOrWhiteSpace(model.matKhauHash) ? "123456_HASH_DEMO" : model.matKhauHash; _db.NguoiDungs.Add(model); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int id) { var user = await _db.NguoiDungs.FindAsync(id); if (user == null) return NotFound(); await LoadSelects(); return View(user); }
    [HttpPost] public async Task<IActionResult> Edit(NguoiDung model) { if (!ModelState.IsValid) { await LoadSelects(); return View(model); } model.ngayCapNhat = DateTime.Now; _db.NguoiDungs.Update(model); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    [HttpPost] public async Task<IActionResult> ToggleLock(int id) { var user = await _db.NguoiDungs.FindAsync(id); if (user == null) return NotFound(); user.trangThai = user.trangThai == "HoatDong" ? "BiKhoa" : "HoatDong"; user.ngayCapNhat = DateTime.Now; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
}
