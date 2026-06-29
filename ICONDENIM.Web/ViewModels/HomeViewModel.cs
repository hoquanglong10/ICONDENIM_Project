using ICONDENIM.Web.Models;
namespace ICONDENIM.Web.ViewModels;
public class HomeViewModel { public List<Banner> Banners { get; set; } = new(); public List<DanhMuc> DanhMucs { get; set; } = new(); public List<SanPhamTrangChuView> HangHot { get; set; } = new(); public List<SanPhamTrangChuView> HangBanChay { get; set; } = new(); public List<SanPhamTrangChuView> TatCaSanPham { get; set; } = new(); }
