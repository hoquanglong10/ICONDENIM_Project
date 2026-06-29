using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class LichSuTonKho { [Key] public int lichSuTonKhoID { get; set; } public int bienTheID { get; set; } public string loaiGiaoDich { get; set; } = string.Empty; public int soLuongThayDoi { get; set; } public int soLuongTruoc { get; set; } public int soLuongSau { get; set; } public int? donHangID { get; set; } public string? ghiChu { get; set; } public DateTime ngayTao { get; set; } = DateTime.Now; public BienTheSanPham? BienTheSanPham { get; set; } }
