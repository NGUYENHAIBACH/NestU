# Hướng dẫn chạy NestU

## Yêu cầu
- .NET 8 SDK
- SQL Server LocalDB (đi kèm Visual Studio workload ASP.NET) hoặc SQL Server bất kỳ (sửa `ConnectionStrings:DefaultConnection` trong `NestU.API/appsettings.json`)
- `dotnet tool install --global dotnet-ef`

## Lần đầu
```powershell
.\setup.ps1
```
Script sẽ restore, build, tạo migration `InitialCreate` (nếu chưa có), tạo CSDL `NestUDb` và build Web App.
Nếu không dùng script: trong `NestU.API` chạy `dotnet ef migrations add InitialCreate` rồi `dotnet ef database update`.

> Nếu lỡ chạy API trước khi tạo migration, CSDL được tạo bằng `EnsureCreated` (không có bảng lịch sử migration). Khi đó hãy xóa CSDL (`dotnet ef database drop -f` trong `NestU.API`) rồi chạy lại `setup.ps1`.

## Chạy
1. `NestU.API`: `dotnet run` (hoặc F5 trong Visual Studio với `NestU.API.sln`) → https://localhost:7001/swagger. Khi khởi động ở Development, API tự migrate và seed dữ liệu mẫu.
2. `NestU.WebApp`: `dotnet run` (hoặc F5 với `NestU.WebApp.sln`) → https://localhost:7002.
3. Dùng Web App trên trình duyệt; Swagger chỉ để test API.

Lần đầu chạy https trên máy mới: `dotnet dev-certs https --trust`.

## Tài khoản demo (mật khẩu `NestU@123`, cấu hình ở `appsettings.Development.json` → `Seed:Password`)
| Email | Role | Ghi chú |
| --- | --- | --- |
| admin@nestu.vn | Admin | |
| chutro1@nestu.vn | Landlord | Đã duyệt, sở hữu "Nhà trọ An Bình" 3 tầng × 4 phòng, đã duyệt PCCC |
| chutro2@nestu.vn | Landlord | Đang chờ Admin duyệt hồ sơ |
| sinhvien1@nestu.vn, sinhvien2@nestu.vn | Tenant | Sinh viên |
| khach1@nestu.vn | Tenant | Khách thường |

## Kịch bản demo (theo tài liệu thiết kế, mục Ngày 6)
1. `chutro2` đăng nhập → thấy "chờ duyệt". `admin` → Duyệt chủ trọ → duyệt. `chutro2` tạo nhà trọ, upload giấy PCCC → `admin` → Duyệt PCCC → nhà trọ Active.
2. `chutro1` → Nhà trọ An Bình → tầng 1 → "Tạo hàng loạt" 101–106 (báo trùng 101–104 → 409) → đổi thành 105–106. Thêm tài sản, sao chép sang phòng khác.
3. `sinhvien1` → Tìm phòng (lọc OData) → chi tiết phòng (lượt xem +1) → Đặt lịch xem → Gửi yêu cầu thuê.
4. `chutro1` → Yêu cầu thuê → Duyệt & lập HĐ (chọn dịch vụ, người ở cùng) → phòng Occupied. `sinhvien1` → Hợp đồng → tab Biên bản → Xác nhận.
5. `chutro1` → Chỉ số điện nước → ghi tháng này → Lập hóa đơn. `sinhvien1` → Hóa đơn → Thanh toán (ảnh) → `chutro1` → Xác nhận.
6. `sinhvien1` → Báo sự cố (gắn tài sản) → `chutro1` cập nhật Đã xử lý.
7. `sinhvien1` → Xin trả phòng → `chutro1` đồng ý → Biên bản trả phòng (khấu trừ) → Quyết toán cọc → hợp đồng Terminated, phòng Available. `sinhvien1` → Đánh giá.

## Case lỗi cố ý để demo ràng buộc
- Tạo nhà trọ với giá điện 5.000 → Web App báo lỗi ngay (DataAnnotations); gọi Swagger với 5000 → 400; sửa thẳng CSDL → CHECK constraint chặn.
- Tạo phòng trùng số → 409. Ghi chỉ số nhỏ hơn tháng trước → 400. Lập hóa đơn trùng tháng → 409.
- Dùng token Tenant gọi `/api/landlord/houses` → 403. `chutro2` (chưa duyệt) gọi `/api/landlord/houses` → 403.
