using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class GioHang { [Key] public int gioHangID { get; set; } public int userID { get; set; } public DateTime ngayTao { get; set; } = DateTime.Now; public DateTime? ngayCapNhat { get; set; } public NguoiDung? NguoiDung { get; set; } public ICollection<ChiTietGioHang>? ChiTietGioHangs { get; set; } }
