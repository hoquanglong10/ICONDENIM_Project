using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class DanhGia { [Key] public int danhGiaID { get; set; } public int userID { get; set; } public int sanPhamID { get; set; } public int donHangID { get; set; } public int soSao { get; set; } public string? noiDung { get; set; } public string trangThai { get; set; } = "ChoDuyet"; public DateTime ngayTao { get; set; } = DateTime.Now; }
