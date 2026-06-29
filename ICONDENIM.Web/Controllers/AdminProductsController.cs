using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.Models;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;
[AdminAuthorize]
public class AdminProductsController : Controller
{
    private readonly AppDbContext _db;
    public AdminProductsController(AppDbContext db) { _db = db; }
    private async Task LoadCategories() => ViewBag.DanhMucs = new SelectList(await _db.DanhMucs.Where(x => x.trangThai).OrderBy(x => x.thuTuHienThi).ToListAsync(), "danhMucID", "tenDanhMuc");

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.SanPhams.Include(x => x.DanhMuc).Include(x => x.BienThes).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.tenSanPham.Contains(q) || x.maSanPham.Contains(q));
        ViewBag.Keyword = q;
        return View(await query.OrderByDescending(x => x.ngayTao).ToListAsync());
    }

    public async Task<IActionResult> Create() { await LoadCategories(); return View(new ProductAdminViewModel()); }
    [HttpPost]
    public async Task<IActionResult> Create(ProductAdminViewModel vm)
    {
        if (!ModelState.IsValid) { await LoadCategories(); return View(vm); }
        var product = new SanPham { danhMucID = vm.danhMucID, maSanPham = vm.maSanPham, tenSanPham = vm.tenSanPham, slug = string.IsNullOrWhiteSpace(vm.slug) ? vm.maSanPham.ToLower() : vm.slug, moTaNgan = vm.moTaNgan, moTaChiTiet = vm.moTaChiTiet, giaGoc = vm.giaGoc, giaKhuyenMai = vm.giaKhuyenMai, hinhAnhDaiDien = vm.hinhAnhDaiDien, laHangHot = vm.laHangHot, laHangBanChay = vm.laHangBanChay, choPhepHienThi = vm.choPhepHienThi, trangThai = vm.trangThai };
        _db.SanPhams.Add(product); await _db.SaveChangesAsync();
        _db.BienTheSanPhams.Add(new BienTheSanPham { sanPhamID = product.sanPhamID, sku = string.IsNullOrWhiteSpace(vm.sku) ? vm.maSanPham + "-DEFAULT" : vm.sku, size = vm.size, mauSac = vm.mauSac, giaBan = vm.giaBan == 0 ? vm.giaGoc : vm.giaBan, giaKhuyenMai = vm.giaKhuyenMai, soLuongTon = vm.soLuongTon, hinhAnh = vm.hinhAnhDaiDien });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.SanPhams.Include(x => x.BienThes).FirstOrDefaultAsync(x => x.sanPhamID == id);
        if (p == null) return NotFound();
        var bt = p.BienThes?.FirstOrDefault();
        var vm = new ProductAdminViewModel { sanPhamID = p.sanPhamID, danhMucID = p.danhMucID, maSanPham = p.maSanPham, tenSanPham = p.tenSanPham, slug = p.slug, moTaNgan = p.moTaNgan, moTaChiTiet = p.moTaChiTiet, giaGoc = p.giaGoc, giaKhuyenMai = p.giaKhuyenMai, hinhAnhDaiDien = p.hinhAnhDaiDien, laHangHot = p.laHangHot, laHangBanChay = p.laHangBanChay, choPhepHienThi = p.choPhepHienThi, trangThai = p.trangThai, sku = bt?.sku ?? "", size = bt?.size ?? "", mauSac = bt?.mauSac ?? "", giaBan = bt?.giaBan ?? p.giaGoc, soLuongTon = bt?.soLuongTon ?? 0 };
        await LoadCategories(); return View(vm);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(ProductAdminViewModel vm)
    {
        var p = await _db.SanPhams.Include(x => x.BienThes).FirstOrDefaultAsync(x => x.sanPhamID == vm.sanPhamID);
        if (p == null) return NotFound();
        p.danhMucID = vm.danhMucID; p.maSanPham = vm.maSanPham; p.tenSanPham = vm.tenSanPham; p.slug = vm.slug; p.moTaNgan = vm.moTaNgan; p.moTaChiTiet = vm.moTaChiTiet; p.giaGoc = vm.giaGoc; p.giaKhuyenMai = vm.giaKhuyenMai; p.hinhAnhDaiDien = vm.hinhAnhDaiDien; p.laHangHot = vm.laHangHot; p.laHangBanChay = vm.laHangBanChay; p.choPhepHienThi = vm.choPhepHienThi; p.trangThai = vm.trangThai; p.ngayCapNhat = DateTime.Now;
        var bt = p.BienThes?.FirstOrDefault();
        if (bt != null) { bt.sku = vm.sku; bt.size = vm.size; bt.mauSac = vm.mauSac; bt.giaBan = vm.giaBan; bt.giaKhuyenMai = vm.giaKhuyenMai; bt.soLuongTon = vm.soLuongTon; bt.hinhAnh = vm.hinhAnhDaiDien; }
        await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleDisplay(int id)
    {
        var p = await _db.SanPhams.FindAsync(id); if (p == null) return NotFound();
        p.choPhepHienThi = !p.choPhepHienThi; p.ngayCapNhat = DateTime.Now; await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
