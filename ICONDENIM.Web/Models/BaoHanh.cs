using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class BaoHanh { [Key] public int baoHanhID { get; set; } public int userID { get; set; } public int donHangID { get; set; } public int sanPhamID { get; set; } public string lyDo { get; set; } = string.Empty; public string trangThai { get; set; } = "TiepNhan"; public DateTime ngayTiepNhan { get; set; } = DateTime.Now; public DateTime? ngayHoanTat { get; set; } }
