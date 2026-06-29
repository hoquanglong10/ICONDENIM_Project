using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class ThanhToan { [Key] public int thanhToanID { get; set; } public int donHangID { get; set; } public string phuongThuc { get; set; } = string.Empty; public decimal soTien { get; set; } public string trangThai { get; set; } = "ChoThanhToan"; public string? maGiaoDich { get; set; } public string? noiDungThanhToan { get; set; } public DateTime? thoiGianThanhToan { get; set; } public DonHang? DonHang { get; set; } }
