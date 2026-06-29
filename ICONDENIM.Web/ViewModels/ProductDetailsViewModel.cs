using ICONDENIM.Web.Models;
namespace ICONDENIM.Web.ViewModels;
public class ProductDetailsViewModel { public SanPham SanPham { get; set; } = new(); public List<BienTheSanPham> BienThes { get; set; } = new(); public List<HinhAnhSanPham> HinhAnhs { get; set; } = new(); public List<DanhGia> DanhGias { get; set; } = new(); public int TongTonKho => BienThes.Where(x => x.trangThai).Sum(x => x.soLuongTon); public bool LaChayHang => TongTonKho == 0; }
