using System.ComponentModel.DataAnnotations;
namespace ICONDENIM.Web.Models;
public class VaiTro { [Key] public int vaiTroID { get; set; } [Required, StringLength(50)] public string tenVaiTro { get; set; } = string.Empty; public string? moTa { get; set; } public bool trangThai { get; set; } = true; public ICollection<NguoiDung>? NguoiDungs { get; set; } }
