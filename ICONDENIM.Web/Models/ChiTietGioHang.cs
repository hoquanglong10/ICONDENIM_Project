using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class ChiTietGioHang { [Key] public int chiTietGioHangID { get; set; } public int gioHangID { get; set; } public int bienTheID { get; set; } public int soLuong { get; set; } public decimal donGiaTamTinh { get; set; } public GioHang? GioHang { get; set; } public BienTheSanPham? BienTheSanPham { get; set; } }
