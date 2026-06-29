using ICONDENIM.Web.Data;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;
public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.SanPhams.Include(x => x.DanhMuc).FirstOrDefaultAsync(x => x.sanPhamID == id && x.choPhepHienThi && x.trangThai == "DangBan");
        if (product == null) return NotFound();
        var vm = new ProductDetailsViewModel
        {
            SanPham = product,
            BienThes = await _db.BienTheSanPhams.Where(x => x.sanPhamID == id && x.trangThai).OrderBy(x => x.size).ThenBy(x => x.mauSac).ToListAsync(),
            HinhAnhs = await _db.HinhAnhSanPhams.Where(x => x.sanPhamID == id && x.trangThai).OrderBy(x => x.thuTuHienThi).ToListAsync(),
            DanhGias = await _db.DanhGias.Where(x => x.sanPhamID == id && x.trangThai == "DaDuyet").OrderByDescending(x => x.ngayTao).ToListAsync()
        };
        if (vm.TongTonKho == 0 && !product.laHangHot) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Search(string? q, int? danhMucID)
    {
        var query = _db.SanPhamTrangChuViews.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.tenSanPham.Contains(q) || x.maSanPham.Contains(q) || x.tenDanhMuc.Contains(q));
        if (danhMucID.HasValue)
        {
            var dm = await _db.DanhMucs.FindAsync(danhMucID.Value);
            if (dm != null) query = query.Where(x => x.tenDanhMuc == dm.tenDanhMuc);
        }
        ViewBag.Keyword = q;
        return View(await query.OrderByDescending(x => x.laHangHot).ToListAsync());
    }
}
