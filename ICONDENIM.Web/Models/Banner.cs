using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class Banner { [Key] public int bannerID { get; set; } public string tieuDe { get; set; } = string.Empty; public string hinhAnh { get; set; } = string.Empty; public string? linkDieuHuong { get; set; } public string viTri { get; set; } = "TrangChu"; public int thuTuHienThi { get; set; } public bool trangThai { get; set; } = true; }
