using ICONDENIM.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<VaiTro> VaiTros => Set<VaiTro>();
    public DbSet<LoaiKhachHang> LoaiKhachHangs => Set<LoaiKhachHang>();
    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<DiaChiNguoiDung> DiaChiNguoiDungs => Set<DiaChiNguoiDung>();
    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<BienTheSanPham> BienTheSanPhams => Set<BienTheSanPham>();
    public DbSet<HinhAnhSanPham> HinhAnhSanPhams => Set<HinhAnhSanPham>();
    public DbSet<GioHang> GioHangs => Set<GioHang>();
    public DbSet<ChiTietGioHang> ChiTietGioHangs => Set<ChiTietGioHang>();
    public DbSet<KhuyenMai> KhuyenMais => Set<KhuyenMai>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();
    public DbSet<LichSuTonKho> LichSuTonKhos => Set<LichSuTonKho>();
    public DbSet<DanhGia> DanhGias => Set<DanhGia>();
    public DbSet<ChamSocKhachHang> ChamSocKhachHangs => Set<ChamSocKhachHang>();
    public DbSet<BaoHanh> BaoHanhs => Set<BaoHanh>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<SanPhamTrangChuView> SanPhamTrangChuViews => Set<SanPhamTrangChuView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<VaiTro>().ToTable("VaiTro");
        modelBuilder.Entity<LoaiKhachHang>().ToTable("LoaiKhachHang");
        modelBuilder.Entity<NguoiDung>().ToTable("NguoiDung");
        modelBuilder.Entity<DiaChiNguoiDung>().ToTable("DiaChiNguoiDung");
        modelBuilder.Entity<DanhMuc>().ToTable("DanhMuc");
        modelBuilder.Entity<SanPham>().ToTable("SanPham");
        modelBuilder.Entity<BienTheSanPham>().ToTable("BienTheSanPham");
        modelBuilder.Entity<HinhAnhSanPham>().ToTable("HinhAnhSanPham");
        modelBuilder.Entity<GioHang>().ToTable("GioHang");
        modelBuilder.Entity<ChiTietGioHang>().ToTable("ChiTietGioHang");
        modelBuilder.Entity<KhuyenMai>().ToTable("KhuyenMai");
        modelBuilder.Entity<DonHang>().ToTable("DonHang");
        modelBuilder.Entity<ChiTietDonHang>().ToTable("ChiTietDonHang");
        modelBuilder.Entity<ThanhToan>().ToTable("ThanhToan");
        modelBuilder.Entity<LichSuTonKho>().ToTable("LichSuTonKho");
        modelBuilder.Entity<DanhGia>().ToTable("DanhGia");
        modelBuilder.Entity<ChamSocKhachHang>().ToTable("ChamSocKhachHang");
        modelBuilder.Entity<BaoHanh>().ToTable("BaoHanh");
        modelBuilder.Entity<Banner>().ToTable("Banner");
        modelBuilder.Entity<SanPhamTrangChuView>().HasNoKey().ToView("vw_SanPhamTrangChu");

        modelBuilder.Entity<NguoiDung>()
            .HasOne(x => x.VaiTro)
            .WithMany(x => x.NguoiDungs)
            .HasForeignKey(x => x.vaiTroID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NguoiDung>()
            .HasOne(x => x.LoaiKhachHang)
            .WithMany()
            .HasForeignKey(x => x.loaiKhachHangID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SanPham>()
            .HasOne(x => x.DanhMuc)
            .WithMany(x => x.SanPhams)
            .HasForeignKey(x => x.danhMucID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SanPham>()
            .HasMany(x => x.BienThes)
            .WithOne(x => x.SanPham)
            .HasForeignKey(x => x.sanPhamID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SanPham>()
            .HasMany(x => x.HinhAnhs)
            .WithOne(x => x.SanPham)
            .HasForeignKey(x => x.sanPhamID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HinhAnhSanPham>()
            .HasOne(x => x.BienTheSanPham)
            .WithMany()
            .HasForeignKey(x => x.bienTheID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DonHang>()
            .HasOne(x => x.NguoiDung)
            .WithMany()
            .HasForeignKey(x => x.userID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DonHang>()
            .HasOne(x => x.NguoiGiao)
            .WithMany()
            .HasForeignKey(x => x.nguoiGiaoID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChiTietDonHang>()
            .HasOne(x => x.DonHang)
            .WithMany(x => x.ChiTietDonHangs)
            .HasForeignKey(x => x.donHangID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChiTietDonHang>()
            .HasOne(x => x.BienTheSanPham)
            .WithMany()
            .HasForeignKey(x => x.bienTheID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
