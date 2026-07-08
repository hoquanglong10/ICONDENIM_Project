using ICONDENIM.Web.Data;
using ICONDENIM.Web.Models;
using ICONDENIM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;

public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) { _db = db; }

    public IActionResult Track() => View(new OrderTrackingViewModel());

    [HttpPost]
    public async Task<IActionResult> Track(OrderTrackingViewModel vm)
    {
        var keyword = vm.Keyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            ModelState.AddModelError(nameof(vm.Keyword), "Vui lòng nhập số điện thoại hoặc mã đơn hàng.");
            return View(vm);
        }

        var query = _db.DonHangs.AsQueryable();
        vm.DonHangs = await query
            .Where(x => x.maDonHang == keyword || x.sdtNhan == keyword || x.sdtKhachVangLai == keyword)
            .OrderByDescending(x => x.ngayDat)
            .ToListAsync();
        if (!vm.DonHangs.Any()) TempData["Error"] = "Không tìm thấy đơn hàng phù hợp.";
        return View(vm);
    }

    public async Task<IActionResult> MyOrders(DateTime? tuNgay, DateTime? denNgay)
    {
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        if (!userId.HasValue) return RedirectToAction(nameof(Track));
        var query = _db.DonHangs.Where(x => x.userID == userId.Value);
        if (tuNgay.HasValue) query = query.Where(x => x.ngayDat >= tuNgay.Value);
        if (denNgay.HasValue) query = query.Where(x => x.ngayDat < denNgay.Value.AddDays(1));
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
        return View(await query.OrderByDescending(x => x.ngayDat).ToListAsync());
    }

    public async Task<IActionResult> Details(int id, string? phone = null)
    {
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        var order = await _db.DonHangs.Include(x => x.ChiTietDonHangs).ThenInclude(x => x.BienTheSanPham).FirstOrDefaultAsync(x => x.donHangID == id);
        if (order == null) return NotFound();
        if (userId.HasValue)
        {
            if (order.userID != userId.Value && !string.Equals(order.sdtNhan, phone)) return Forbid();
        }
        else if (string.IsNullOrWhiteSpace(phone) || order.sdtNhan != phone)
        {
            TempData["Error"] = "Vui lòng tra cứu bằng số điện thoại để xem chi tiết đơn hàng.";
            return RedirectToAction(nameof(Track));
        }
        ViewBag.Phone = phone;
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, string? phone = null)
    {
        var order = await _db.DonHangs.FindAsync(id);
        if (order == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        var allowed = (userId.HasValue && order.userID == userId.Value) || (!string.IsNullOrWhiteSpace(phone) && order.sdtNhan == phone);
        if (!allowed) return Forbid();
        if (order.trangThaiDonHang is "GiaoThanhCong" or "DaHuy" or "DangGiao")
        {
            TempData["Error"] = "Đơn hàng đã giao/hủy/đang giao nên không thể hủy từ phía khách hàng.";
            return RedirectToAction(nameof(Details), new { id, phone });
        }
        await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_HuyDonHang @donHangID, @lyDo",
            new SqlParameter("@donHangID", id), new SqlParameter("@lyDo", "Khách hàng yêu cầu hủy đơn"));
        TempData["Success"] = "Đã hủy đơn hàng và cộng lại số lượng tồn kho.";
        return RedirectToAction(nameof(Details), new { id, phone });
    }

    public async Task<IActionResult> Review(int donHangID, int sanPhamID)
    {
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        if (!userId.HasValue) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Review), new { donHangID, sanPhamID }) });
        var order = await _db.DonHangs.Include(x => x.ChiTietDonHangs).FirstOrDefaultAsync(x => x.donHangID == donHangID && x.userID == userId.Value);
        if (order == null || order.trangThaiDonHang != "GiaoThanhCong")
        {
            TempData["Error"] = "Chỉ có thể đánh giá sản phẩm khi đơn hàng đã giao thành công.";
            return RedirectToAction(nameof(MyOrders));
        }
        return View(new ReviewCreateViewModel { donHangID = donHangID, sanPhamID = sanPhamID });
    }

    [HttpPost]
    public async Task<IActionResult> Review(ReviewCreateViewModel vm)
    {
        var userId = HttpContext.Session.GetInt32("CustomerUserID");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(vm);
        var exists = await _db.DanhGias.AnyAsync(x => x.userID == userId.Value && x.donHangID == vm.donHangID && x.sanPhamID == vm.sanPhamID);
        if (exists)
        {
            TempData["Error"] = "Bạn đã đánh giá sản phẩm này trong đơn hàng.";
            return RedirectToAction(nameof(Details), new { id = vm.donHangID });
        }
        _db.DanhGias.Add(new DanhGia
        {
            userID = userId.Value,
            sanPhamID = vm.sanPhamID,
            donHangID = vm.donHangID,
            soSao = vm.soSao,
            noiDung = vm.noiDung,
            trangThai = "ChoDuyet",
            ngayTao = DateTime.Now
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm. Đánh giá đang chờ Admin duyệt.";
        return RedirectToAction(nameof(Details), new { id = vm.donHangID });
    }
}
