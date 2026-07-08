using ICONDENIM.Web.Models;

namespace ICONDENIM.Web.ViewModels;

public class RevenueReportViewModel
{
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string? TrangThaiDonHang { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public List<DonHang> DonHangs { get; set; } = new();
    public int TongSoDon => DonHangs.Count;
    public decimal TongDoanhThu => DonHangs.Where(x => x.trangThaiDonHang == "GiaoThanhCong" && x.daThuTien).Sum(x => x.thanhTien);
    public decimal TongCongNo => DonHangs.Where(x => x.trangThaiDonHang == "GiaoThanhCong" && !x.daThuTien).Sum(x => x.thanhTien);
    public int DonCOD => DonHangs.Count(x => x.phuongThucThanhToan == "COD");
    public int DonOnline => DonHangs.Count(x => x.phuongThucThanhToan != "COD");
}
