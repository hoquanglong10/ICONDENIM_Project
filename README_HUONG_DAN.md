# ICONDENIM - Bài thực hành tuần 4

Bộ source này triển khai các yêu cầu:

1. Chèn dữ liệu cho các bảng CSDL.
2. Kết nối SQL Server và lấy dữ liệu hiển thị lên website.
3. Trang chủ: hiển thị banner, hàng hot, hàng bán chạy, sản phẩm hợp lệ; hàng hot hết tồn kho hiển thị "Cháy hàng".
4. Trang chi tiết sản phẩm: hiển thị thông tin sản phẩm, biến thể size/màu, tồn kho, đánh giá.
5. Admin: quản lý hàng hóa, người dùng, đơn hàng, khuyến mãi.
6. Đặt hàng/hủy đơn dùng stored procedure để trừ/cộng tồn kho.

## Bước chạy

### 1. Tạo CSDL
Mở SQL Server Management Studio, chạy file:

```text
SQL/ICONDENIM_Project_CSDL.sql
```

### 2. Sửa chuỗi kết nối
Mở file:

```text
ICONDENIM.Web/appsettings.json
```

Nếu dùng SQL Server Express, sửa thành:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=ICONDENIM_Project;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 3. Chạy web
Mở terminal trong thư mục `ICONDENIM.Web` rồi chạy:

```bash
dotnet restore
dotnet run
```

### 4. Các URL demo

- Trang chủ: `/`
- Tìm kiếm: `/Products/Search?q=denim`
- Chi tiết sản phẩm: `/Products/Details/1`
- Admin hàng hóa: `/AdminProducts`
- Admin người dùng: `/AdminUsers`
- Admin đơn hàng: `/AdminOrders`
- Admin khuyến mãi: `/AdminPromotions`

## Tài khoản demo

- Admin: `admin@icondenim.vn` / `123456` (mật khẩu hash demo trong DB)
- Khách hàng: `an.nguyen@example.com` / `123456`

## Điểm cần demo để lấy điểm

1. Chạy script CSDL và chỉ ra dữ liệu mẫu đã được chèn.
2. Trang chủ hiển thị sản phẩm từ SQL Server.
3. Trang chi tiết hiển thị biến thể size/màu/tồn kho.
4. Admin thêm/sửa/ẩn hiện sản phẩm.
5. Admin thêm/sửa/khóa mở người dùng.
6. Admin xem/lọc/cập nhật/hủy đơn hàng.
7. Khi hủy đơn, tồn kho được cộng lại trong `BienTheSanPham` và có log ở `LichSuTonKho`.


## Cách mở bằng Visual Studio

1. Mở file `ICONDENIM_Project_Tuan4.sln` bằng Visual Studio 2022.
2. Chạy file SQL `SQL/ICONDENIM_Project_CSDL.sql` trong SQL Server Management Studio để tạo CSDL và dữ liệu mẫu.
3. Mở `ICONDENIM.Web/appsettings.json` và chỉnh `DefaultConnection` cho đúng SQL Server của máy.
4. Bấm `Ctrl + F5` hoặc nút Run để chạy website.

## Cách chạy không cần Visual Studio

Mở Terminal tại thư mục chứa file `.sln`, chạy:

```bash
dotnet restore
dotnet run --project ICONDENIM.Web/ICONDENIM.Web.csproj
```

Hoặc bấm đôi file `RUN_WEB.bat`.
