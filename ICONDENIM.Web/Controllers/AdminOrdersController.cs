using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;
[AdminAuthorize]
public class AdminOrdersController : Controller
{
    private readonly AppDbContext _db;
    public AdminOrdersController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string? trangThai)
    {
        var query = _db.DonHangs.Include(x => x.NguoiDung).Include(x => x.NguoiGiao).AsQueryable();
        if (tuNgay.HasValue) query = query.Where(x => x.ngayDat >= tuNgay.Value);
        if (denNgay.HasValue) query = query.Where(x => x.ngayDat < denNgay.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.trangThaiDonHang == trangThai);
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd"); ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd"); ViewBag.TrangThai = trangThai;
        return View(await query.OrderByDescending(x => x.ngayDat).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.DonHangs.Include(x => x.NguoiDung).Include(x => x.NguoiGiao).Include(x => x.ChiTietDonHangs).FirstOrDefaultAsync(x => x.donHangID == id);
        if (order == null) return NotFound(); return View(order);
    }

    public async Task<IActionResult> EditStatus(int id)
    {
        var order = await _db.DonHangs.FindAsync(id); if (order == null) return NotFound();
        ViewBag.Shippers = new SelectList(await _db.NguoiDungs.Include(x => x.VaiTro).Where(x => x.VaiTro!.tenVaiTro == "NhanVienGiaoHang").ToListAsync(), "userID", "hoTen");
        return View(new OrderStatusUpdateViewModel { donHangID = id, trangThaiDonHang = order.trangThaiDonHang, trangThaiGiaoHang = order.trangThaiGiaoHang, daThuTien = order.daThuTien, nguoiGiaoID = order.nguoiGiaoID, ghiChu = order.ghiChu });
    }
    [HttpPost]
    public async Task<IActionResult> EditStatus(OrderStatusUpdateViewModel vm)
    {
        await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_CapNhatTrangThaiDonHang @donHangID, @trangThaiDonHang, @trangThaiGiaoHang, @daThuTien, @nguoiGiaoID, @ghiChu",
            new SqlParameter("@donHangID", vm.donHangID), new SqlParameter("@trangThaiDonHang", vm.trangThaiDonHang),
            new SqlParameter("@trangThaiGiaoHang", (object?)vm.trangThaiGiaoHang ?? DBNull.Value), new SqlParameter("@daThuTien", (object?)vm.daThuTien ?? DBNull.Value),
            new SqlParameter("@nguoiGiaoID", (object?)vm.nguoiGiaoID ?? DBNull.Value), new SqlParameter("@ghiChu", (object?)vm.ghiChu ?? DBNull.Value));
        return RedirectToAction(nameof(Details), new { id = vm.donHangID });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_HuyDonHang @donHangID, @lyDo", new SqlParameter("@donHangID", id), new SqlParameter("@lyDo", "Admin hủy đơn"));
        return RedirectToAction(nameof(Details), new { id });
    }
}
