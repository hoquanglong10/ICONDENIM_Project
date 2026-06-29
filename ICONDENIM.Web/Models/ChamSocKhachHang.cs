using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class ChamSocKhachHang { [Key] public int yeuCauID { get; set; } public int? userID { get; set; } public int? sanPhamID { get; set; } public int? donHangID { get; set; } public string loaiYeuCau { get; set; } = string.Empty; public string noiDung { get; set; } = string.Empty; public string trangThai { get; set; } = "Moi"; public DateTime ngayGui { get; set; } = DateTime.Now; public DateTime? ngayXuLy { get; set; } }
