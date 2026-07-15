using System.ComponentModel.DataAnnotations;
using ICONDENIM.Web.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ICONDENIM.Web.ViewModels;

public class CartSessionItem
{
    public int bienTheID { get; set; }
    public int soLuong { get; set; }
}

public class CartItemViewModel
{
    public int bienTheID { get; set; }
    public int sanPhamID { get; set; }
    public string tenSanPham { get; set; } = string.Empty;
    public string sku { get; set; } = string.Empty;
    public string size { get; set; } = string.Empty;
    public string mauSac { get; set; } = string.Empty;
    public string? hinhAnh { get; set; }
    public int soLuong { get; set; }
    public int soLuongTon { get; set; }
    public decimal donGia { get; set; }
    public decimal thanhTien => soLuong * donGia;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal TongTienHang => Items.Sum(x => x.thanhTien);
    public decimal PhiVanChuyen => Items.Any() ? 30000 : 0;
    public decimal GiamGia { get; set; }
    public decimal ThanhTien => TongTienHang + PhiVanChuyen - GiamGia;
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [Display(Name = "Họ tên người nhận")]
    public string HoTenNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Display(Name = "Số điện thoại nhận hàng")]
    public string SdtNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string DiaChiNhan { get; set; } = string.Empty;

    [Display(Name = "Mã khuyến mãi")]
    public string? MaKhuyenMai { get; set; }

    [Display(Name = "Phương thức thanh toán")]
    public string PhuongThucThanhToan { get; set; } = "COD";

    [Display(Name = "Tài khoản thanh toán online")]
    public string? TaiKhoanThanhToanOnline { get; set; }

    [Display(Name = "Mã xác nhận thanh toán demo")]
    public string? MaOtpThanhToan { get; set; }

    public CartViewModel Cart { get; set; } = new();
}

public class OrderTrackingViewModel
{
    [Display(Name = "Số điện thoại hoặc mã đơn hàng")]
    public string? Keyword { get; set; }
    public List<DonHang> DonHangs { get; set; } = new();
}

public class ProfileViewModel
{
    public int userID { get; set; }
    [Required] public string hoTen { get; set; } = string.Empty;
    [Required] public string soDienThoai { get; set; } = string.Empty;
    [EmailAddress] public string? email { get; set; }
    public DateTime? ngaySinh { get; set; }
    public int diemTichLuy { get; set; }
    public string? diaChiGiaoHang { get; set; }
    public string? loaiKhachHang { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string MatKhauCu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    public string NhapLaiMatKhauMoi { get; set; } = string.Empty;
}

public class ReviewCreateViewModel
{
    public int donHangID { get; set; }
    public int sanPhamID { get; set; }
    [Range(1,5)] public int soSao { get; set; } = 5;
    [StringLength(500)] public string? noiDung { get; set; }
}

public class SupportRequestViewModel
{
    public int? sanPhamID { get; set; }
    public int? donHangID { get; set; }
    [Required] public string loaiYeuCau { get; set; } = "HoiDap";
    [Required(ErrorMessage = "Vui lòng nhập nội dung cần hỗ trợ")]
    [StringLength(1000)] public string noiDung { get; set; } = string.Empty;

    [ValidateNever] public List<SupportOrderOptionViewModel> OrderOptions { get; set; } = new();
    [ValidateNever] public List<SupportProductOptionViewModel> ProductOptions { get; set; } = new();
    [ValidateNever] public string CustomerDisplayName { get; set; } = string.Empty;
    [ValidateNever] public string CustomerContact { get; set; } = string.Empty;
}

public class SupportOrderOptionViewModel
{
    public int DonHangID { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SupportProductOptionViewModel
{
    public int SanPhamID { get; set; }
    public int? DonHangID { get; set; }
    public string Label { get; set; } = string.Empty;
}
