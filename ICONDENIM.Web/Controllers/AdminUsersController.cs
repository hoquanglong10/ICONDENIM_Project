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
    public async Task<IActionResult> Create() { await LoadSelects(); ViewBag.DiaChiGiaoHang = string.Empty; return View(new NguoiDung()); }
    [HttpPost] public async Task<IActionResult> Create(NguoiDung model, string? diaChiGiaoHang) { if (!ModelState.IsValid) { await LoadSelects(); ViewBag.DiaChiGiaoHang = diaChiGiaoHang; return View(model); } model.matKhauHash = string.IsNullOrWhiteSpace(model.matKhauHash) ? "123456_HASH_DEMO" : model.matKhauHash; _db.NguoiDungs.Add(model); await _db.SaveChangesAsync(); if (!string.IsNullOrWhiteSpace(diaChiGiaoHang)) { _db.DiaChiNguoiDungs.Add(new DiaChiNguoiDung { userID = model.userID, hoTenNhan = model.hoTen, sdtNhan = model.soDienThoai, diaChiChiTiet = diaChiGiaoHang.Trim(), laMacDinh = true, trangThai = true }); await _db.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int id) { var user = await _db.NguoiDungs.FindAsync(id); if (user == null) return NotFound(); await LoadSelects(); var address = await _db.DiaChiNguoiDungs.FirstOrDefaultAsync(x => x.userID == id && x.laMacDinh && x.trangThai); ViewBag.DiaChiGiaoHang = address?.diaChiChiTiet ?? string.Empty; return View(user); }
    [HttpPost] public async Task<IActionResult> Edit(NguoiDung model, string? diaChiGiaoHang) { if (!ModelState.IsValid) { await LoadSelects(); ViewBag.DiaChiGiaoHang = diaChiGiaoHang; return View(model); } model.ngayCapNhat = DateTime.Now; _db.NguoiDungs.Update(model); var address = await _db.DiaChiNguoiDungs.FirstOrDefaultAsync(x => x.userID == model.userID && x.laMacDinh && x.trangThai); if (!string.IsNullOrWhiteSpace(diaChiGiaoHang)) { if (address == null) _db.DiaChiNguoiDungs.Add(new DiaChiNguoiDung { userID = model.userID, hoTenNhan = model.hoTen, sdtNhan = model.soDienThoai, diaChiChiTiet = diaChiGiaoHang.Trim(), laMacDinh = true, trangThai = true }); else { address.hoTenNhan = model.hoTen; address.sdtNhan = model.soDienThoai; address.diaChiChiTiet = diaChiGiaoHang.Trim(); } } await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    [HttpPost] public async Task<IActionResult> ToggleLock(int id) { var user = await _db.NguoiDungs.FindAsync(id); if (user == null) return NotFound(); user.trangThai = user.trangThai == "HoatDong" ? "BiKhoa" : "HoatDong"; user.ngayCapNhat = DateTime.Now; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
}
