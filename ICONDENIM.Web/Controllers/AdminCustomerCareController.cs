using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.ViewModels;
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
        var requestQuery = _db.ChamSocKhachHangs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(loaiYeuCau))
            requestQuery = requestQuery.Where(x => x.loaiYeuCau == loaiYeuCau);
        if (!string.IsNullOrWhiteSpace(trangThai))
            requestQuery = requestQuery.Where(x => x.trangThai == trangThai);

        var items = await (
            from request in requestQuery
            join user in _db.NguoiDungs.AsNoTracking()
                on request.userID equals (int?)user.userID into userGroup
            from user in userGroup.DefaultIfEmpty()
            join order in _db.DonHangs.AsNoTracking()
                on request.donHangID equals (int?)order.donHangID into orderGroup
            from order in orderGroup.DefaultIfEmpty()
            join product in _db.SanPhams.AsNoTracking()
                on request.sanPhamID equals (int?)product.sanPhamID into productGroup
            from product in productGroup.DefaultIfEmpty()
            orderby request.ngayGui descending
            select new AdminCustomerCareItemViewModel
            {
                YeuCauID = request.yeuCauID,
                UserID = request.userID,
                TenKhachHang = user != null
                    ? user.hoTen
                    : order != null
                        ? (order.tenKhachVangLai ?? order.hoTenNhan)
                        : "Không xác định",
                SoDienThoai = user != null
                    ? user.soDienThoai
                    : order != null
                        ? (order.sdtKhachVangLai ?? order.sdtNhan)
                        : null,
                Email = user != null ? user.email : null,
                LoaiYeuCau = request.loaiYeuCau,
                NoiDung = request.noiDung,
                TrangThai = request.trangThai,
                NgayGui = request.ngayGui,
                NgayXuLy = request.ngayXuLy,
                DonHangID = request.donHangID,
                MaDonHang = order != null ? order.maDonHang : null,
                TrangThaiDonHang = order != null ? order.trangThaiDonHang : null,
                NgayDat = order != null ? order.ngayDat : null,
                SanPhamID = request.sanPhamID,
                TenSanPham = product != null ? product.tenSanPham : null,
                MaSanPham = product != null ? product.maSanPham : null
            }).ToListAsync();

        return View(new AdminCustomerCareIndexViewModel
        {
            Items = items,
            LoaiYeuCau = loaiYeuCau,
            TrangThai = trangThai
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequest(int id, string trangThai)
    {
        var allowedStatuses = new[] { "Moi", "DangXuLy", "DaXuLy", "Dong", "TuChoi" };
        if (!allowedStatuses.Contains(trangThai))
        {
            TempData["Error"] = "Trạng thái yêu cầu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var request = await _db.ChamSocKhachHangs.FindAsync(id);
        if (request == null) return NotFound();

        request.trangThai = trangThai;
        request.ngayXuLy = trangThai == "Moi" ? null : DateTime.Now;

        if (request.loaiYeuCau == "BaoHanh" &&
            request.userID.HasValue && request.donHangID.HasValue && request.sanPhamID.HasValue)
        {
            var warranty = await _db.BaoHanhs.FirstOrDefaultAsync(x =>
                x.userID == request.userID.Value &&
                x.donHangID == request.donHangID.Value &&
                x.sanPhamID == request.sanPhamID.Value);

            if (warranty != null)
            {
                warranty.trangThai = trangThai switch
                {
                    "Moi" => "TiepNhan",
                    "DangXuLy" => "DangXuLy",
                    "TuChoi" => "TuChoi",
                    "DaXuLy" or "Dong" => "HoanTat",
                    _ => warranty.trangThai
                };
                warranty.ngayHoanTat = warranty.trangThai == "HoanTat" ? DateTime.Now : null;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật trạng thái yêu cầu khách hàng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Reviews(string? trangThai)
    {
        var reviewQuery = _db.DanhGias.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(trangThai))
            reviewQuery = reviewQuery.Where(x => x.trangThai == trangThai);

        ViewBag.TrangThai = trangThai;
        var items = await (
            from review in reviewQuery
            join user in _db.NguoiDungs.AsNoTracking() on review.userID equals user.userID
            join product in _db.SanPhams.AsNoTracking() on review.sanPhamID equals product.sanPhamID
            join order in _db.DonHangs.AsNoTracking() on review.donHangID equals order.donHangID
            orderby review.ngayTao descending
            select new AdminReviewItemViewModel
            {
                DanhGiaID = review.danhGiaID,
                UserID = review.userID,
                TenKhachHang = user.hoTen,
                SoDienThoai = user.soDienThoai,
                SanPhamID = review.sanPhamID,
                TenSanPham = product.tenSanPham,
                DonHangID = review.donHangID,
                MaDonHang = order.maDonHang,
                SoSao = review.soSao,
                NoiDung = review.noiDung,
                TrangThai = review.trangThai,
                NgayTao = review.ngayTao
            }).ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReview(int id, string trangThai)
    {
        if (!new[] { "ChoDuyet", "DaDuyet", "TuChoi" }.Contains(trangThai))
            return BadRequest();

        var review = await _db.DanhGias.FindAsync(id);
        if (review == null) return NotFound();
        review.trangThai = trangThai;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật trạng thái đánh giá sản phẩm.";
        return RedirectToAction(nameof(Reviews));
    }

    // Giữ route cũ để các bookmark không lỗi, nhưng bảo hành được quản lý trực tiếp trong màn hình CSKH.
    public IActionResult Warranties() => RedirectToAction(nameof(Index), new { loaiYeuCau = "BaoHanh" });
}
