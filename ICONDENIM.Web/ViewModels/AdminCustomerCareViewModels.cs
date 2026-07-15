namespace ICONDENIM.Web.ViewModels;

public class AdminCustomerCareIndexViewModel
{
    public List<AdminCustomerCareItemViewModel> Items { get; set; } = new();
    public string? LoaiYeuCau { get; set; }
    public string? TrangThai { get; set; }
}

public class AdminCustomerCareItemViewModel
{
    public int YeuCauID { get; set; }
    public int? UserID { get; set; }
    public string TenKhachHang { get; set; } = "Không xác định";
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }

    public string LoaiYeuCau { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayGui { get; set; }
    public DateTime? NgayXuLy { get; set; }

    public int? DonHangID { get; set; }
    public string? MaDonHang { get; set; }
    public string? TrangThaiDonHang { get; set; }
    public DateTime? NgayDat { get; set; }

    public int? SanPhamID { get; set; }
    public string? TenSanPham { get; set; }
    public string? MaSanPham { get; set; }

    public bool ThieuThongTinLienKet =>
        (LoaiYeuCau == "DonHang" && !DonHangID.HasValue) ||
        ((LoaiYeuCau == "DoiTra" || LoaiYeuCau == "BaoHanh") &&
         (!DonHangID.HasValue || !SanPhamID.HasValue));
}

public class AdminReviewItemViewModel
{
    public int DanhGiaID { get; set; }
    public int UserID { get; set; }
    public string TenKhachHang { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public int SanPhamID { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public int DonHangID { get; set; }
    public string MaDonHang { get; set; } = string.Empty;
    public int SoSao { get; set; }
    public string? NoiDung { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
}
