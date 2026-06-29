using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class HinhAnhSanPham { [Key] public int hinhAnhID { get; set; } public int sanPhamID { get; set; } public int? bienTheID { get; set; } public string duongDanAnh { get; set; } = string.Empty; public bool laAnhChinh { get; set; } public int thuTuHienThi { get; set; } public bool trangThai { get; set; } = true; public SanPham? SanPham { get; set; } public BienTheSanPham? BienTheSanPham { get; set; } }
