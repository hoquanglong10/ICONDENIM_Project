
/* ================================================================
   ICONDENIM - CSDL chuẩn cho bài thực hành tuần 4 và project thật
   Nội dung: tạo CSDL, tạo bảng, ràng buộc, dữ liệu mẫu, view,
             stored procedure đặt hàng/hủy đơn/cập nhật trạng thái.
   ================================================================ */

IF DB_ID(N'ICONDENIM_Project') IS NULL
BEGIN
    CREATE DATABASE ICONDENIM_Project;
END
GO
USE ICONDENIM_Project;
GO

/* Xóa đối tượng cũ để chạy lại script nhiều lần */
IF OBJECT_ID(N'dbo.sp_CapNhatTrangThaiDonHang', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_CapNhatTrangThaiDonHang;
IF OBJECT_ID(N'dbo.sp_HuyDonHang', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_HuyDonHang;
IF OBJECT_ID(N'dbo.sp_DatHangNhanh', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_DatHangNhanh;
IF OBJECT_ID(N'dbo.vw_DoanhThuTheoDonHang', N'V') IS NOT NULL DROP VIEW dbo.vw_DoanhThuTheoDonHang;
IF OBJECT_ID(N'dbo.vw_SanPhamTrangChu', N'V') IS NOT NULL DROP VIEW dbo.vw_SanPhamTrangChu;
GO

DROP TABLE IF EXISTS dbo.BaoHanh;
DROP TABLE IF EXISTS dbo.ChamSocKhachHang;
DROP TABLE IF EXISTS dbo.DanhGia;
DROP TABLE IF EXISTS dbo.LichSuTonKho;
DROP TABLE IF EXISTS dbo.ThanhToan;
DROP TABLE IF EXISTS dbo.ChiTietDonHang;
DROP TABLE IF EXISTS dbo.DonHang;
DROP TABLE IF EXISTS dbo.KhuyenMai;
DROP TABLE IF EXISTS dbo.ChiTietGioHang;
DROP TABLE IF EXISTS dbo.GioHang;
DROP TABLE IF EXISTS dbo.HinhAnhSanPham;
DROP TABLE IF EXISTS dbo.BienTheSanPham;
DROP TABLE IF EXISTS dbo.SanPham;
DROP TABLE IF EXISTS dbo.DanhMuc;
DROP TABLE IF EXISTS dbo.DiaChiNguoiDung;
DROP TABLE IF EXISTS dbo.NguoiDung;
DROP TABLE IF EXISTS dbo.LoaiKhachHang;
DROP TABLE IF EXISTS dbo.VaiTro;
DROP TABLE IF EXISTS dbo.Banner;
GO

CREATE TABLE dbo.VaiTro(
    vaiTroID INT IDENTITY(1,1) PRIMARY KEY,
    tenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    moTa NVARCHAR(255) NULL,
    trangThai BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.LoaiKhachHang(
    loaiKhachHangID INT IDENTITY(1,1) PRIMARY KEY,
    tenLoai NVARCHAR(50) NOT NULL UNIQUE,
    diemTu INT NOT NULL DEFAULT 0,
    diemDen INT NULL,
    tiLeUuDai DECIMAL(5,2) NOT NULL DEFAULT 0,
    moTa NVARCHAR(255) NULL,
    trangThai BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.NguoiDung(
    userID INT IDENTITY(1,1) PRIMARY KEY,
    vaiTroID INT NOT NULL,
    loaiKhachHangID INT NULL,
    hoTen NVARCHAR(100) NOT NULL,
    soDienThoai VARCHAR(15) NOT NULL UNIQUE,
    email VARCHAR(100) NULL UNIQUE,
    matKhauHash NVARCHAR(255) NOT NULL,
    ngaySinh DATE NULL,
    diemTichLuy INT NOT NULL DEFAULT 0,
    trangThai NVARCHAR(30) NOT NULL DEFAULT N'HoatDong',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ngayCapNhat DATETIME2 NULL,
    CONSTRAINT FK_NguoiDung_VaiTro FOREIGN KEY(vaiTroID) REFERENCES dbo.VaiTro(vaiTroID),
    CONSTRAINT FK_NguoiDung_LoaiKhachHang FOREIGN KEY(loaiKhachHangID) REFERENCES dbo.LoaiKhachHang(loaiKhachHangID),
    CONSTRAINT CK_NguoiDung_TrangThai CHECK(trangThai IN (N'HoatDong', N'BiKhoa'))
);

CREATE TABLE dbo.DiaChiNguoiDung(
    diaChiID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT NOT NULL,
    hoTenNhan NVARCHAR(100) NOT NULL,
    sdtNhan VARCHAR(15) NOT NULL,
    diaChiChiTiet NVARCHAR(255) NOT NULL,
    phuongXa NVARCHAR(100) NULL,
    quanHuyen NVARCHAR(100) NULL,
    tinhThanh NVARCHAR(100) NULL,
    laMacDinh BIT NOT NULL DEFAULT 0,
    trangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DiaChi_NguoiDung FOREIGN KEY(userID) REFERENCES dbo.NguoiDung(userID)
);

CREATE TABLE dbo.DanhMuc(
    danhMucID INT IDENTITY(1,1) PRIMARY KEY,
    danhMucChaID INT NULL,
    tenDanhMuc NVARCHAR(100) NOT NULL,
    slug VARCHAR(150) NOT NULL UNIQUE,
    moTa NVARCHAR(255) NULL,
    thuTuHienThi INT NOT NULL DEFAULT 0,
    trangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DanhMuc_Cha FOREIGN KEY(danhMucChaID) REFERENCES dbo.DanhMuc(danhMucID)
);

CREATE TABLE dbo.SanPham(
    sanPhamID INT IDENTITY(1,1) PRIMARY KEY,
    danhMucID INT NOT NULL,
    maSanPham VARCHAR(30) NOT NULL UNIQUE,
    tenSanPham NVARCHAR(200) NOT NULL,
    slug VARCHAR(220) NOT NULL UNIQUE,
    moTaNgan NVARCHAR(500) NULL,
    moTaChiTiet NVARCHAR(MAX) NULL,
    giaGoc DECIMAL(18,2) NOT NULL,
    giaKhuyenMai DECIMAL(18,2) NULL,
    hinhAnhDaiDien NVARCHAR(255) NULL,
    laHangHot BIT NOT NULL DEFAULT 0,
    laHangBanChay BIT NOT NULL DEFAULT 0,
    choPhepHienThi BIT NOT NULL DEFAULT 1,
    trangThai NVARCHAR(30) NOT NULL DEFAULT N'DangBan',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ngayCapNhat DATETIME2 NULL,
    CONSTRAINT FK_SanPham_DanhMuc FOREIGN KEY(danhMucID) REFERENCES dbo.DanhMuc(danhMucID),
    CONSTRAINT CK_SanPham_Gia CHECK(giaGoc >= 0 AND (giaKhuyenMai IS NULL OR giaKhuyenMai >= 0)),
    CONSTRAINT CK_SanPham_TrangThai CHECK(trangThai IN (N'DangBan', N'NgungBan', N'An'))
);

CREATE TABLE dbo.BienTheSanPham(
    bienTheID INT IDENTITY(1,1) PRIMARY KEY,
    sanPhamID INT NOT NULL,
    sku VARCHAR(50) NOT NULL UNIQUE,
    size NVARCHAR(20) NOT NULL,
    mauSac NVARCHAR(50) NOT NULL,
    giaBan DECIMAL(18,2) NOT NULL,
    giaKhuyenMai DECIMAL(18,2) NULL,
    soLuongTon INT NOT NULL DEFAULT 0,
    hinhAnh NVARCHAR(255) NULL,
    trangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_BienThe_SanPham FOREIGN KEY(sanPhamID) REFERENCES dbo.SanPham(sanPhamID),
    CONSTRAINT CK_BienThe_Ton CHECK(soLuongTon >= 0),
    CONSTRAINT CK_BienThe_Gia CHECK(giaBan >= 0 AND (giaKhuyenMai IS NULL OR giaKhuyenMai >= 0))
);

CREATE TABLE dbo.HinhAnhSanPham(
    hinhAnhID INT IDENTITY(1,1) PRIMARY KEY,
    sanPhamID INT NOT NULL,
    bienTheID INT NULL,
    duongDanAnh NVARCHAR(255) NOT NULL,
    laAnhChinh BIT NOT NULL DEFAULT 0,
    thuTuHienThi INT NOT NULL DEFAULT 0,
    trangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_HinhAnh_SanPham FOREIGN KEY(sanPhamID) REFERENCES dbo.SanPham(sanPhamID),
    CONSTRAINT FK_HinhAnh_BienThe FOREIGN KEY(bienTheID) REFERENCES dbo.BienTheSanPham(bienTheID)
);

CREATE TABLE dbo.GioHang(
    gioHangID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT NOT NULL UNIQUE,
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ngayCapNhat DATETIME2 NULL,
    CONSTRAINT FK_GioHang_NguoiDung FOREIGN KEY(userID) REFERENCES dbo.NguoiDung(userID)
);

CREATE TABLE dbo.ChiTietGioHang(
    chiTietGioHangID INT IDENTITY(1,1) PRIMARY KEY,
    gioHangID INT NOT NULL,
    bienTheID INT NOT NULL,
    soLuong INT NOT NULL,
    donGiaTamTinh DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_CTGioHang_GioHang FOREIGN KEY(gioHangID) REFERENCES dbo.GioHang(gioHangID),
    CONSTRAINT FK_CTGioHang_BienThe FOREIGN KEY(bienTheID) REFERENCES dbo.BienTheSanPham(bienTheID),
    CONSTRAINT CK_CTGioHang_SoLuong CHECK(soLuong > 0),
    CONSTRAINT UQ_CTGioHang UNIQUE(gioHangID, bienTheID)
);

CREATE TABLE dbo.KhuyenMai(
    khuyenMaiID INT IDENTITY(1,1) PRIMARY KEY,
    maCode VARCHAR(30) NOT NULL UNIQUE,
    tenChuongTrinh NVARCHAR(150) NOT NULL,
    loaiGiam NVARCHAR(20) NOT NULL,
    giaTriGiam DECIMAL(18,2) NOT NULL,
    giaTriGiamToiDa DECIMAL(18,2) NULL,
    dieuKienToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0,
    soLuotSuDung INT NOT NULL DEFAULT 0,
    daSuDung INT NOT NULL DEFAULT 0,
    ngayBatDau DATETIME2 NOT NULL,
    ngayKetThuc DATETIME2 NOT NULL,
    apDungLoaiKhachHangID INT NULL,
    trangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_KhuyenMai_LoaiKhach FOREIGN KEY(apDungLoaiKhachHangID) REFERENCES dbo.LoaiKhachHang(loaiKhachHangID),
    CONSTRAINT CK_KhuyenMai_Loai CHECK(loaiGiam IN (N'PhanTram', N'TienMat')),
    CONSTRAINT CK_KhuyenMai_Luot CHECK(soLuotSuDung >= 0 AND daSuDung >= 0)
);

CREATE TABLE dbo.DonHang(
    donHangID INT IDENTITY(1,1) PRIMARY KEY,
    maDonHang VARCHAR(30) NOT NULL UNIQUE,
    userID INT NULL,
    khuyenMaiID INT NULL,
    hoTenNhan NVARCHAR(100) NOT NULL,
    sdtNhan VARCHAR(15) NOT NULL,
    diaChiNhan NVARCHAR(255) NOT NULL,
    tenKhachVangLai NVARCHAR(100) NULL,
    sdtKhachVangLai VARCHAR(15) NULL,
    ngayDat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    trangThaiDonHang NVARCHAR(30) NOT NULL DEFAULT N'ChoXacNhan',
    trangThaiThanhToan NVARCHAR(30) NOT NULL DEFAULT N'ChuaThanhToan',
    trangThaiGiaoHang NVARCHAR(30) NOT NULL DEFAULT N'ChuaGiao',
    phuongThucThanhToan NVARCHAR(30) NOT NULL DEFAULT N'COD',
    tongTienHang DECIMAL(18,2) NOT NULL DEFAULT 0,
    phiVanChuyen DECIMAL(18,2) NOT NULL DEFAULT 0,
    giamGia DECIMAL(18,2) NOT NULL DEFAULT 0,
    thanhTien AS (tongTienHang + phiVanChuyen - giamGia) PERSISTED,
    daThuTien BIT NOT NULL DEFAULT 0,
    nguoiGiaoID INT NULL,
    ghiChu NVARCHAR(500) NULL,
    ngayCapNhat DATETIME2 NULL,
    CONSTRAINT FK_DonHang_NguoiDung FOREIGN KEY(userID) REFERENCES dbo.NguoiDung(userID),
    CONSTRAINT FK_DonHang_KhuyenMai FOREIGN KEY(khuyenMaiID) REFERENCES dbo.KhuyenMai(khuyenMaiID),
    CONSTRAINT FK_DonHang_NguoiGiao FOREIGN KEY(nguoiGiaoID) REFERENCES dbo.NguoiDung(userID),
    CONSTRAINT CK_DonHang_TTDH CHECK(trangThaiDonHang IN (N'ChoXacNhan', N'DaDuyet', N'DangGiao', N'GiaoThanhCong', N'GiaoThatBai', N'DaHuy')),
    CONSTRAINT CK_DonHang_TTThanhToan CHECK(trangThaiThanhToan IN (N'ChuaThanhToan', N'DaThanhToan', N'ThanhToanThatBai', N'HoanTien')),
    CONSTRAINT CK_DonHang_TTGiao CHECK(trangThaiGiaoHang IN (N'ChuaGiao', N'DangGiao', N'GiaoThanhCong', N'GiaoThatBai')),
    CONSTRAINT CK_DonHang_PTThanhToan CHECK(phuongThucThanhToan IN (N'COD', N'VNPay', N'MoMo', N'Banking'))
);

CREATE TABLE dbo.ChiTietDonHang(
    chiTietDonHangID INT IDENTITY(1,1) PRIMARY KEY,
    donHangID INT NOT NULL,
    bienTheID INT NOT NULL,
    tenSanPhamSnapshot NVARCHAR(200) NOT NULL,
    skuSnapshot VARCHAR(50) NOT NULL,
    sizeSnapshot NVARCHAR(20) NOT NULL,
    mauSacSnapshot NVARCHAR(50) NOT NULL,
    soLuong INT NOT NULL,
    donGia DECIMAL(18,2) NOT NULL,
    thanhTien AS (soLuong * donGia) PERSISTED,
    commentPro NVARCHAR(500) NULL,
    CONSTRAINT FK_CTDonHang_DonHang FOREIGN KEY(donHangID) REFERENCES dbo.DonHang(donHangID),
    CONSTRAINT FK_CTDonHang_BienThe FOREIGN KEY(bienTheID) REFERENCES dbo.BienTheSanPham(bienTheID),
    CONSTRAINT CK_CTDonHang_SoLuong CHECK(soLuong > 0)
);

CREATE TABLE dbo.ThanhToan(
    thanhToanID INT IDENTITY(1,1) PRIMARY KEY,
    donHangID INT NOT NULL,
    phuongThuc NVARCHAR(30) NOT NULL,
    soTien DECIMAL(18,2) NOT NULL,
    trangThai NVARCHAR(30) NOT NULL DEFAULT N'ChoThanhToan',
    maGiaoDich VARCHAR(100) NULL,
    noiDungThanhToan NVARCHAR(255) NULL,
    thoiGianThanhToan DATETIME2 NULL,
    CONSTRAINT FK_ThanhToan_DonHang FOREIGN KEY(donHangID) REFERENCES dbo.DonHang(donHangID),
    CONSTRAINT CK_ThanhToan_Tien CHECK(soTien >= 0)
);

CREATE TABLE dbo.LichSuTonKho(
    lichSuTonKhoID INT IDENTITY(1,1) PRIMARY KEY,
    bienTheID INT NOT NULL,
    loaiGiaoDich NVARCHAR(30) NOT NULL,
    soLuongThayDoi INT NOT NULL,
    soLuongTruoc INT NOT NULL,
    soLuongSau INT NOT NULL,
    donHangID INT NULL,
    ghiChu NVARCHAR(255) NULL,
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_LichSuTonKho_BienThe FOREIGN KEY(bienTheID) REFERENCES dbo.BienTheSanPham(bienTheID),
    CONSTRAINT FK_LichSuTonKho_DonHang FOREIGN KEY(donHangID) REFERENCES dbo.DonHang(donHangID)
);

CREATE TABLE dbo.DanhGia(
    danhGiaID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT NOT NULL,
    sanPhamID INT NOT NULL,
    donHangID INT NOT NULL,
    soSao INT NOT NULL,
    noiDung NVARCHAR(1000) NULL,
    trangThai NVARCHAR(30) NOT NULL DEFAULT N'ChoDuyet',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_DanhGia_NguoiDung FOREIGN KEY(userID) REFERENCES dbo.NguoiDung(userID),
    CONSTRAINT FK_DanhGia_SanPham FOREIGN KEY(sanPhamID) REFERENCES dbo.SanPham(sanPhamID),
    CONSTRAINT FK_DanhGia_DonHang FOREIGN KEY(donHangID) REFERENCES dbo.DonHang(donHangID),
    CONSTRAINT CK_DanhGia_Sao CHECK(soSao BETWEEN 1 AND 5)
);

CREATE TABLE dbo.ChamSocKhachHang(
    yeuCauID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT NULL,
    sanPhamID INT NULL,
    donHangID INT NULL,
    loaiYeuCau NVARCHAR(50) NOT NULL,
    noiDung NVARCHAR(1000) NOT NULL,
    trangThai NVARCHAR(30) NOT NULL DEFAULT N'Moi',
    ngayGui DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ngayXuLy DATETIME2 NULL,
    CONSTRAINT FK_CSKH_NguoiDung FOREIGN KEY(userID) REFERENCES dbo.NguoiDung(userID),
    CONSTRAINT FK_CSKH_SanPham FOREIGN KEY(sanPhamID) REFERENCES dbo.SanPham(sanPhamID),
    CONSTRAINT FK_CSKH_DonHang FOREIGN KEY(donHangID) REFERENCES dbo.DonHang(donHangID)
);

CREATE TABLE dbo.BaoHanh(
    baoHanhID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT NOT NULL,
    donHangID INT NOT NULL,
    sanPhamID INT NOT NULL,
    lyDo NVARCHAR(1000) NOT NULL,
    trangThai NVARCHAR(30) NOT NULL DEFAULT N'TiepNhan',
    ngayTiepNhan DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ngayHoanTat DATETIME2 NULL,
    CONSTRAINT FK_BaoHanh_NguoiDung FOREIGN KEY(userID) REFERENCES dbo.NguoiDung(userID),
    CONSTRAINT FK_BaoHanh_DonHang FOREIGN KEY(donHangID) REFERENCES dbo.DonHang(donHangID),
    CONSTRAINT FK_BaoHanh_SanPham FOREIGN KEY(sanPhamID) REFERENCES dbo.SanPham(sanPhamID)
);

CREATE TABLE dbo.Banner(
    bannerID INT IDENTITY(1,1) PRIMARY KEY,
    tieuDe NVARCHAR(150) NOT NULL,
    hinhAnh NVARCHAR(255) NOT NULL,
    linkDieuHuong NVARCHAR(255) NULL,
    viTri NVARCHAR(50) NOT NULL,
    thuTuHienThi INT NOT NULL DEFAULT 0,
    trangThai BIT NOT NULL DEFAULT 1
);
GO

/* Dữ liệu mẫu */
INSERT INTO dbo.VaiTro(tenVaiTro, moTa) VALUES
(N'Admin', N'Quản trị hệ thống'),
(N'KhachHang', N'Khách hàng mua sắm'),
(N'NhanVienKho', N'Nhân viên quản lý kho'),
(N'NhanVienGiaoHang', N'Nhân viên giao hàng');

INSERT INTO dbo.LoaiKhachHang(tenLoai, diemTu, diemDen, tiLeUuDai, moTa) VALUES
(N'VangLai', 0, 499, 0, N'Khách hàng mới hoặc chưa đủ điểm'),
(N'ThanThiet', 500, 999, 3, N'Khách hàng thân thiết'),
(N'Bac', 1000, 4999, 5, N'Hạng bạc'),
(N'Vang', 5000, 9999, 7, N'Hạng vàng'),
(N'BachKim', 10000, NULL, 10, N'Hạng bạch kim');

INSERT INTO dbo.NguoiDung(vaiTroID, loaiKhachHangID, hoTen, soDienThoai, email, matKhauHash, ngaySinh, diemTichLuy) VALUES
(1, NULL, N'Quản trị ICONDENIM', '0900000001', 'admin@icondenim.vn', '123456_HASH_DEMO', '1995-01-01', 0),
(2, 3, N'Nguyễn Minh An', '0900000002', 'an.nguyen@example.com', '123456_HASH_DEMO', '2002-05-20', 1250),
(2, 2, N'Trần Hoàng Nam', '0900000003', 'nam.tran@example.com', '123456_HASH_DEMO', '2001-10-12', 620),
(4, NULL, N'Lê Văn Giao', '0900000004', 'giaohang@icondenim.vn', '123456_HASH_DEMO', '1998-03-09', 0),
(3, NULL, N'Phạm Thị Kho', '0900000005', 'kho@icondenim.vn', '123456_HASH_DEMO', '1997-07-15', 0);

INSERT INTO dbo.DiaChiNguoiDung(userID, hoTenNhan, sdtNhan, diaChiChiTiet, phuongXa, quanHuyen, tinhThanh, laMacDinh) VALUES
(2, N'Nguyễn Minh An', '0900000002', N'12 Nguyễn Trãi', N'Phường Bến Thành', N'Quận 1', N'TP.HCM', 1),
(3, N'Trần Hoàng Nam', '0900000003', N'88 Cộng Hòa', N'Phường 4', N'Tân Bình', N'TP.HCM', 1);

INSERT INTO dbo.DanhMuc(danhMucChaID, tenDanhMuc, slug, moTa, thuTuHienThi) VALUES
(NULL, N'Áo nam', 'ao-nam', N'Áo thun, áo sơ mi, hoodie', 1),
(NULL, N'Quần nam', 'quan-nam', N'Quần jeans, quần short, quần kaki', 2),
(NULL, N'Giày & Phụ kiện', 'giay-phu-kien', N'Giày, vớ, mũ, túi', 3),
(NULL, N'Hàng mới', 'hang-moi', N'Sản phẩm mới về', 4),
(NULL, N'Hàng bán chạy', 'hang-ban-chay', N'Sản phẩm được mua nhiều', 5);

INSERT INTO dbo.SanPham(danhMucID, maSanPham, tenSanPham, slug, moTaNgan, moTaChiTiet, giaGoc, giaKhuyenMai, hinhAnhDaiDien, laHangHot, laHangBanChay, choPhepHienThi, trangThai) VALUES
(1, 'ICD-AT-001', N'Áo thun Basic Logo ICONDENIM', 'ao-thun-basic-logo-icondenim', N'Áo thun cotton form regular dễ phối.', N'Chất liệu cotton mềm, thấm hút tốt, phù hợp mặc hằng ngày.', 299000, 249000, '/images/products/ao-thun-basic.jpg', 1, 1, 1, N'DangBan'),
(1, 'ICD-SM-002', N'Áo sơ mi Denim xanh nhạt', 'ao-so-mi-denim-xanh-nhat', N'Sơ mi denim xanh nhạt phong cách casual.', N'Form rộng vừa, chất vải denim mềm, phù hợp đi học/đi chơi.', 499000, 429000, '/images/products/so-mi-denim.jpg', 0, 1, 1, N'DangBan'),
(2, 'ICD-QJ-003', N'Quần jeans slim xanh đậm', 'quan-jeans-slim-xanh-dam', N'Jeans slim fit tôn dáng.', N'Chất denim co giãn nhẹ, đường may chắc chắn, dễ phối áo thun/sơ mi.', 699000, 599000, '/images/products/jeans-slim.jpg', 1, 1, 1, N'DangBan'),
(2, 'ICD-QS-004', N'Quần short kaki be', 'quan-short-kaki-be', N'Quần short kaki nhẹ, thoải mái.', N'Phù hợp mùa hè, chất kaki mềm, túi tiện dụng.', 399000, NULL, '/images/products/short-kaki.jpg', 0, 0, 1, N'DangBan'),
(1, 'ICD-AK-005', N'Áo khoác Denim oversize', 'ao-khoac-denim-oversize', N'Áo khoác denim hot trend.', N'Áo khoác denim dày dặn, form oversize, phù hợp phối nhiều phong cách.', 899000, 799000, '/images/products/ao-khoac-denim.jpg', 1, 0, 1, N'DangBan'),
(3, 'ICD-VN-006', N'Combo 3 vớ nam no-show', 'combo-3-vo-nam-no-show', N'Vớ nam thấp cổ, mềm và thoáng.', N'Combo 3 đôi vớ nam, co giãn tốt, phù hợp giày sneaker.', 99000, 79000, '/images/products/vo-no-show.jpg', 0, 1, 1, N'DangBan'),
(2, 'ICD-QJ-007', N'Quần jeans rách gối đen', 'quan-jeans-rach-goi-den', N'Jeans đen rách gối cá tính.', N'Chất liệu denim đen, thiết kế rách gối nổi bật.', 749000, 649000, '/images/products/jeans-den-rach.jpg', 0, 0, 1, N'DangBan'),
(1, 'ICD-HD-008', N'Hoodie ICONDENIM Signature', 'hoodie-icondenim-signature', N'Hoodie nỉ dày dặn phiên bản hot.', N'Hoodie form rộng, chất nỉ mềm, logo thêu ngực.', 599000, 499000, '/images/products/hoodie-signature.jpg', 1, 0, 1, N'DangBan');

INSERT INTO dbo.BienTheSanPham(sanPhamID, sku, size, mauSac, giaBan, giaKhuyenMai, soLuongTon, hinhAnh) VALUES
(1, 'ICD-AT-001-M-DEN', N'M', N'Đen', 299000, 249000, 20, '/images/products/ao-thun-basic.jpg'),
(1, 'ICD-AT-001-L-TRANG', N'L', N'Trắng', 299000, 249000, 15, '/images/products/ao-thun-basic.jpg'),
(2, 'ICD-SM-002-M-XANH', N'M', N'Xanh nhạt', 499000, 429000, 8, '/images/products/so-mi-denim.jpg'),
(2, 'ICD-SM-002-L-XANH', N'L', N'Xanh nhạt', 499000, 429000, 6, '/images/products/so-mi-denim.jpg'),
(3, 'ICD-QJ-003-30-XANH', N'30', N'Xanh đậm', 699000, 599000, 10, '/images/products/jeans-slim.jpg'),
(3, 'ICD-QJ-003-32-XANH', N'32', N'Xanh đậm', 699000, 599000, 9, '/images/products/jeans-slim.jpg'),
(4, 'ICD-QS-004-M-BE', N'M', N'Be', 399000, NULL, 11, '/images/products/short-kaki.jpg'),
(5, 'ICD-AK-005-FREE-XANH', N'Free', N'Xanh denim', 899000, 799000, 0, '/images/products/ao-khoac-denim.jpg'),
(6, 'ICD-VN-006-FREE-DEN', N'Free', N'Đen', 99000, 79000, 40, '/images/products/vo-no-show.jpg'),
(7, 'ICD-QJ-007-31-DEN', N'31', N'Đen', 749000, 649000, 7, '/images/products/jeans-den-rach.jpg'),
(8, 'ICD-HD-008-L-XAM', N'L', N'Xám', 599000, 499000, 0, '/images/products/hoodie-signature.jpg');

INSERT INTO dbo.HinhAnhSanPham(sanPhamID, bienTheID, duongDanAnh, laAnhChinh, thuTuHienThi) VALUES
(1, 1, '/images/products/ao-thun-basic.jpg', 1, 1),
(2, 3, '/images/products/so-mi-denim.jpg', 1, 1),
(3, 5, '/images/products/jeans-slim.jpg', 1, 1),
(4, 7, '/images/products/short-kaki.jpg', 1, 1),
(5, 8, '/images/products/ao-khoac-denim.jpg', 1, 1),
(6, 9, '/images/products/vo-no-show.jpg', 1, 1),
(7, 10, '/images/products/jeans-den-rach.jpg', 1, 1),
(8, 11, '/images/products/hoodie-signature.jpg', 1, 1);

INSERT INTO dbo.KhuyenMai(maCode, tenChuongTrinh, loaiGiam, giaTriGiam, giaTriGiamToiDa, dieuKienToiThieu, soLuotSuDung, daSuDung, ngayBatDau, ngayKetThuc, apDungLoaiKhachHangID) VALUES
('JUN20', N'Giảm 20% đơn từ 500K', N'PhanTram', 20, 120000, 500000, 200, 3, '2026-06-01', '2026-07-31', NULL),
('VIP50', N'Ưu đãi khách hàng thân thiết', N'TienMat', 50000, 50000, 300000, 100, 1, '2026-06-01', '2026-08-31', 2);

INSERT INTO dbo.Banner(tieuDe, hinhAnh, linkDieuHuong, viTri, thuTuHienThi) VALUES
(N'Summer is coming - Hè mát hơn', '/images/banner/banner-summer.jpg', '/Products/Search?q=summer', N'TrangChu', 1),
(N'Denim collection', '/images/banner/banner-denim.jpg', '/Products/Search?q=denim', N'TrangChu', 2);

INSERT INTO dbo.GioHang(userID) VALUES (2), (3);
INSERT INTO dbo.ChiTietGioHang(gioHangID, bienTheID, soLuong, donGiaTamTinh) VALUES
(1, 1, 1, 249000),
(1, 5, 1, 599000),
(2, 9, 2, 79000);

INSERT INTO dbo.DonHang(maDonHang, userID, khuyenMaiID, hoTenNhan, sdtNhan, diaChiNhan, ngayDat, trangThaiDonHang, trangThaiThanhToan, trangThaiGiaoHang, phuongThucThanhToan, tongTienHang, phiVanChuyen, giamGia, daThuTien, nguoiGiaoID, ghiChu)
VALUES
('DH202606001', 2, 1, N'Nguyễn Minh An', '0900000002', N'12 Nguyễn Trãi, Quận 1, TP.HCM', '2026-06-15', N'GiaoThanhCong', N'DaThanhToan', N'GiaoThanhCong', N'VNPay', 848000, 30000, 120000, 1, 4, N'Đơn đã giao và thu tiền'),
('DH202606002', 3, NULL, N'Trần Hoàng Nam', '0900000003', N'88 Cộng Hòa, Tân Bình, TP.HCM', '2026-06-20', N'DangGiao', N'ChuaThanhToan', N'DangGiao', N'COD', 79000, 30000, 0, 0, 4, N'Đang giao'),
('DH202606003', NULL, NULL, N'Lê Khách Lẻ', '0900000006', N'25 Lê Lợi, Quận 1, TP.HCM', '2026-06-21', N'ChoXacNhan', N'ChuaThanhToan', N'ChuaGiao', N'COD', 399000, 30000, 0, 0, NULL, N'Khách vãng lai');

INSERT INTO dbo.ChiTietDonHang(donHangID, bienTheID, tenSanPhamSnapshot, skuSnapshot, sizeSnapshot, mauSacSnapshot, soLuong, donGia, commentPro) VALUES
(1, 1, N'Áo thun Basic Logo ICONDENIM', 'ICD-AT-001-M-DEN', N'M', N'Đen', 1, 249000, N'Giao đúng màu đen'),
(1, 5, N'Quần jeans slim xanh đậm', 'ICD-QJ-003-30-XANH', N'30', N'Xanh đậm', 1, 599000, NULL),
(2, 9, N'Combo 3 vớ nam no-show', 'ICD-VN-006-FREE-DEN', N'Free', N'Đen', 1, 79000, NULL),
(3, 7, N'Quần short kaki be', 'ICD-QS-004-M-BE', N'M', N'Be', 1, 399000, NULL);

INSERT INTO dbo.ThanhToan(donHangID, phuongThuc, soTien, trangThai, maGiaoDich, noiDungThanhToan, thoiGianThanhToan) VALUES
(1, N'VNPay', 758000, N'ThanhCong', 'VNP202606001', N'Thanh toán đơn DH202606001', '2026-06-15 10:30:00'),
(2, N'COD', 109000, N'ChoThuTien', NULL, N'Thu tiền khi giao hàng', NULL),
(3, N'COD', 429000, N'ChoThuTien', NULL, N'Khách vãng lai thanh toán COD', NULL);

INSERT INTO dbo.LichSuTonKho(bienTheID, loaiGiaoDich, soLuongThayDoi, soLuongTruoc, soLuongSau, donHangID, ghiChu) VALUES
(1, N'DatHang', -1, 21, 20, 1, N'Đặt hàng thành công'),
(5, N'DatHang', -1, 11, 10, 1, N'Đặt hàng thành công'),
(9, N'DatHang', -1, 41, 40, 2, N'Đặt hàng thành công'),
(7, N'DatHang', -1, 12, 11, 3, N'Đặt hàng thành công');

INSERT INTO dbo.DanhGia(userID, sanPhamID, donHangID, soSao, noiDung, trangThai) VALUES
(2, 1, 1, 5, N'Áo đẹp, chất vải tốt.', N'DaDuyet'),
(2, 3, 1, 4, N'Quần vừa form, giao nhanh.', N'DaDuyet');

INSERT INTO dbo.ChamSocKhachHang(userID, sanPhamID, donHangID, loaiYeuCau, noiDung, trangThai) VALUES
(3, 6, 2, N'HoiDap', N'Đơn hàng khi nào giao tới?', N'DangXuLy'),
(2, 1, 1, N'DoiTra', N'Muốn đổi size áo từ M sang L nếu còn hàng.', N'Moi');

INSERT INTO dbo.BaoHanh(userID, donHangID, sanPhamID, lyDo, trangThai) VALUES
(2, 1, 3, N'Khóa kéo cần kiểm tra lại.', N'TiepNhan');
GO

/* View hiển thị trang chủ: chỉ bán sản phẩm hợp lệ; hết hàng chỉ hiện nếu là hàng hot để ghi chú Cháy hàng */
CREATE VIEW dbo.vw_SanPhamTrangChu AS
SELECT
    sp.sanPhamID,
    sp.maSanPham,
    sp.tenSanPham,
    sp.slug,
    dm.tenDanhMuc,
    sp.moTaNgan,
    COALESCE(MIN(COALESCE(bt.giaKhuyenMai, bt.giaBan)), COALESCE(sp.giaKhuyenMai, sp.giaGoc)) AS giaHienThi,
    sp.hinhAnhDaiDien,
    sp.laHangHot,
    sp.laHangBanChay,
    sp.choPhepHienThi,
    sp.trangThai,
    ISNULL(SUM(CASE WHEN bt.trangThai = 1 THEN bt.soLuongTon ELSE 0 END), 0) AS tongTonKho,
    CASE WHEN ISNULL(SUM(CASE WHEN bt.trangThai = 1 THEN bt.soLuongTon ELSE 0 END), 0) = 0 AND sp.laHangHot = 1 THEN N'Cháy hàng'
         WHEN ISNULL(SUM(CASE WHEN bt.trangThai = 1 THEN bt.soLuongTon ELSE 0 END), 0) > 0 THEN N'Còn hàng'
         ELSE N'Không hiển thị' END AS trangThaiHienThi
FROM dbo.SanPham sp
JOIN dbo.DanhMuc dm ON sp.danhMucID = dm.danhMucID
LEFT JOIN dbo.BienTheSanPham bt ON sp.sanPhamID = bt.sanPhamID
WHERE sp.choPhepHienThi = 1 AND sp.trangThai = N'DangBan' AND dm.trangThai = 1
GROUP BY sp.sanPhamID, sp.maSanPham, sp.tenSanPham, sp.slug, dm.tenDanhMuc, sp.moTaNgan, sp.hinhAnhDaiDien, sp.giaKhuyenMai, sp.giaGoc, sp.laHangHot, sp.laHangBanChay, sp.choPhepHienThi, sp.trangThai
HAVING ISNULL(SUM(CASE WHEN bt.trangThai = 1 THEN bt.soLuongTon ELSE 0 END), 0) > 0 OR sp.laHangHot = 1;
GO

CREATE VIEW dbo.vw_DoanhThuTheoDonHang AS
SELECT
    dh.donHangID,
    dh.maDonHang,
    dh.ngayDat,
    dh.trangThaiDonHang,
    dh.trangThaiThanhToan,
    dh.phuongThucThanhToan,
    dh.thanhTien,
    dh.daThuTien,
    CASE WHEN dh.trangThaiDonHang = N'GiaoThanhCong' AND (dh.daThuTien = 1 OR dh.trangThaiThanhToan = N'DaThanhToan') THEN dh.thanhTien ELSE 0 END AS doanhThuTinh
FROM dbo.DonHang dh;
GO

CREATE PROCEDURE dbo.sp_DatHangNhanh
    @userID INT = NULL,
    @bienTheID INT,
    @soLuong INT,
    @hoTenNhan NVARCHAR(100),
    @sdtNhan VARCHAR(15),
    @diaChiNhan NVARCHAR(255),
    @phuongThucThanhToan NVARCHAR(30) = N'COD'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @ton INT, @donGia DECIMAL(18,2), @tenSP NVARCHAR(200), @sku VARCHAR(50), @size NVARCHAR(20), @mauSac NVARCHAR(50);
        SELECT @ton = bt.soLuongTon,
               @donGia = COALESCE(bt.giaKhuyenMai, bt.giaBan),
               @tenSP = sp.tenSanPham,
               @sku = bt.sku,
               @size = bt.size,
               @mauSac = bt.mauSac
        FROM dbo.BienTheSanPham bt
        JOIN dbo.SanPham sp ON bt.sanPhamID = sp.sanPhamID
        WHERE bt.bienTheID = @bienTheID AND bt.trangThai = 1 AND sp.choPhepHienThi = 1 AND sp.trangThai = N'DangBan';

        IF @ton IS NULL THROW 50001, N'Biến thể sản phẩm không hợp lệ.', 1;
        IF @soLuong <= 0 THROW 50002, N'Số lượng đặt phải lớn hơn 0.', 1;
        IF @ton < @soLuong THROW 50003, N'Số lượng tồn kho không đủ.', 1;

        DECLARE @maDonHang VARCHAR(30) = CONCAT('DH', FORMAT(SYSDATETIME(), 'yyyyMMddHHmmss'));
        DECLARE @tong DECIMAL(18,2) = @donGia * @soLuong;
        INSERT INTO dbo.DonHang(maDonHang, userID, hoTenNhan, sdtNhan, diaChiNhan, tenKhachVangLai, sdtKhachVangLai, phuongThucThanhToan, tongTienHang, phiVanChuyen, giamGia)
        VALUES(@maDonHang, @userID, @hoTenNhan, @sdtNhan, @diaChiNhan,
               CASE WHEN @userID IS NULL THEN @hoTenNhan ELSE NULL END,
               CASE WHEN @userID IS NULL THEN @sdtNhan ELSE NULL END,
               @phuongThucThanhToan, @tong, 30000, 0);
        DECLARE @donHangID INT = SCOPE_IDENTITY();

        INSERT INTO dbo.ChiTietDonHang(donHangID, bienTheID, tenSanPhamSnapshot, skuSnapshot, sizeSnapshot, mauSacSnapshot, soLuong, donGia, commentPro)
        VALUES(@donHangID, @bienTheID, @tenSP, @sku, @size, @mauSac, @soLuong, @donGia, NULL);

        UPDATE dbo.BienTheSanPham SET soLuongTon = soLuongTon - @soLuong WHERE bienTheID = @bienTheID;
        INSERT INTO dbo.LichSuTonKho(bienTheID, loaiGiaoDich, soLuongThayDoi, soLuongTruoc, soLuongSau, donHangID, ghiChu)
        VALUES(@bienTheID, N'DatHang', -@soLuong, @ton, @ton - @soLuong, @donHangID, N'Trừ tồn kho khi đặt hàng thành công');

        COMMIT TRANSACTION;
        SELECT @donHangID AS donHangID, @maDonHang AS maDonHang;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE dbo.sp_HuyDonHang
    @donHangID INT,
    @lyDo NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM dbo.DonHang WHERE donHangID = @donHangID)
            THROW 50101, N'Đơn hàng không tồn tại.', 1;
        IF EXISTS (SELECT 1 FROM dbo.DonHang WHERE donHangID = @donHangID AND trangThaiDonHang IN (N'DaHuy', N'GiaoThanhCong'))
            THROW 50102, N'Đơn hàng đã hủy hoặc đã giao thành công, không thể hủy.', 1;

        DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT bienTheID, soLuong FROM dbo.ChiTietDonHang WHERE donHangID = @donHangID;
        DECLARE @bienTheID INT, @soLuong INT, @tonTruoc INT;
        OPEN cur;
        FETCH NEXT FROM cur INTO @bienTheID, @soLuong;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @tonTruoc = soLuongTon FROM dbo.BienTheSanPham WHERE bienTheID = @bienTheID;
            UPDATE dbo.BienTheSanPham SET soLuongTon = soLuongTon + @soLuong WHERE bienTheID = @bienTheID;
            INSERT INTO dbo.LichSuTonKho(bienTheID, loaiGiaoDich, soLuongThayDoi, soLuongTruoc, soLuongSau, donHangID, ghiChu)
            VALUES(@bienTheID, N'HuyDon', @soLuong, @tonTruoc, @tonTruoc + @soLuong, @donHangID, ISNULL(@lyDo, N'Cộng lại tồn kho khi hủy đơn'));
            FETCH NEXT FROM cur INTO @bienTheID, @soLuong;
        END
        CLOSE cur; DEALLOCATE cur;

        UPDATE dbo.DonHang
        SET trangThaiDonHang = N'DaHuy', trangThaiGiaoHang = N'ChuaGiao', ngayCapNhat = SYSDATETIME(), ghiChu = ISNULL(@lyDo, ghiChu)
        WHERE donHangID = @donHangID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE dbo.sp_CapNhatTrangThaiDonHang
    @donHangID INT,
    @trangThaiDonHang NVARCHAR(30),
    @trangThaiGiaoHang NVARCHAR(30) = NULL,
    @daThuTien BIT = NULL,
    @nguoiGiaoID INT = NULL,
    @ghiChu NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.DonHang
    SET trangThaiDonHang = @trangThaiDonHang,
        trangThaiGiaoHang = ISNULL(@trangThaiGiaoHang, trangThaiGiaoHang),
        daThuTien = ISNULL(@daThuTien, daThuTien),
        nguoiGiaoID = ISNULL(@nguoiGiaoID, nguoiGiaoID),
        ghiChu = ISNULL(@ghiChu, ghiChu),
        trangThaiThanhToan = CASE WHEN ISNULL(@daThuTien, daThuTien) = 1 THEN N'DaThanhToan' ELSE trangThaiThanhToan END,
        ngayCapNhat = SYSDATETIME()
    WHERE donHangID = @donHangID;
END;
GO

/* Một số truy vấn test nhanh
SELECT * FROM dbo.vw_SanPhamTrangChu;
EXEC dbo.sp_DatHangNhanh @userID = 2, @bienTheID = 1, @soLuong = 1, @hoTenNhan = N'Nguyễn Minh An', @sdtNhan = '0900000002', @diaChiNhan = N'12 Nguyễn Trãi, Quận 1';
EXEC dbo.sp_HuyDonHang @donHangID = 3, @lyDo = N'Khách yêu cầu hủy';
*/
