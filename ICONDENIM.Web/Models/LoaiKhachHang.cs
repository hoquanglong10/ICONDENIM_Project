using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class LoaiKhachHang { [Key] public int loaiKhachHangID { get; set; } [Required, StringLength(50)] public string tenLoai { get; set; } = string.Empty; public int diemTu { get; set; } public int? diemDen { get; set; } public decimal tiLeUuDai { get; set; } public string? moTa { get; set; } public bool trangThai { get; set; } = true; }
