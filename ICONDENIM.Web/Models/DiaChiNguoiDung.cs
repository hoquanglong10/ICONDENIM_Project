using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class DiaChiNguoiDung { [Key] public int diaChiID { get; set; } public int userID { get; set; } public string hoTenNhan { get; set; } = string.Empty; public string sdtNhan { get; set; } = string.Empty; public string diaChiChiTiet { get; set; } = string.Empty; public string? phuongXa { get; set; } public string? quanHuyen { get; set; } public string? tinhThanh { get; set; } public bool laMacDinh { get; set; } public bool trangThai { get; set; } = true; public NguoiDung? NguoiDung { get; set; } }
