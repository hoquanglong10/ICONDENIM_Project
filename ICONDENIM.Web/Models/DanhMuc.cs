using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class DanhMuc { [Key] public int danhMucID { get; set; } public int? danhMucChaID { get; set; } [Required] public string tenDanhMuc { get; set; } = string.Empty; public string slug { get; set; } = string.Empty; public string? moTa { get; set; } public int thuTuHienThi { get; set; } public bool trangThai { get; set; } = true; public DanhMuc? DanhMucCha { get; set; } public ICollection<SanPham>? SanPhams { get; set; } }
