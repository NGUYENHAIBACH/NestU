# Lời nhắc cho Claude Code

## Sau khi code xong (từ 31/10) — dán theo thứ tự

Toàn bộ code đã có sẵn trong repo. Việc còn lại là biên dịch, chạy và sửa lỗi.

### A. Biên dịch
```
Đọc CLAUDE.md và docs/HUONG-DAN-CHAY.md. Chạy .\setup.ps1 (hoặc dotnet build trong NestU.API rồi NestU.WebApp).
Sửa mọi lỗi biên dịch, mỗi lỗi sửa đúng chỗ, không viết lại file. Build lại tới khi 0 lỗi. Liệt kê những gì đã sửa.
```

### B. Chạy API
```
Chạy NestU.API, kiểm tra Swagger mở được, seed chạy không lỗi (xem log). Đăng nhập bằng 5 tài khoản demo qua POST /api/auth/login,
gọi GET /api/auth/me, GET /odata/Rooms?$count=true&$top=3, GET /api/rooms/1, GET /api/landlord/houses với token chutro1 (200) và chutro2 (403).
Có lỗi thì sửa và chạy lại.
```

### C. Chạy Web App
```
Chạy NestU.WebApp song song với API. Đi qua kịch bản demo 7 bước trong docs/HUONG-DAN-CHAY.md bằng cách đọc code từng trang
và chạy thử trên trình duyệt. Sửa mọi lỗi runtime, lỗi binding form, lỗi hiển thị. Báo lại từng bước đã qua.
```

### D. Hoàn thiện
```
Chụp Database Diagram từ SQL Server, bổ sung docs/NestU-design.md cho khớp code thực tế (endpoint, entity đã đổi), kiểm tra responsive trên màn hình hẹp.
```

---

# Lịch gốc (tham khảo)

## Lịch nén 3 ngày (bắt đầu 31/10, nộp 3/11)

| Khi nào | Làm gì | Lời nhắc dùng |
| --- | --- | --- |
| Tối T7 31/10 | Nền tảng: 2 project, Entity, CSDL, seed, JWT, khung Web App | Bước 0, rồi 1.1 → 1.4 |
| CN 1/11 | Toàn bộ API tầng Bắt buộc (mỗi người một mảng) | Ngày 2–3 + OData |
| T2 2/11 | Toàn bộ Web App + ghép nối, chạy kịch bản demo | Ngày 4–5, Ngày 6 |
| T3 3/11 | Sửa lỗi, seed dữ liệu demo, chụp ERD và giao diện, báo cáo | Ngày 6 (chiều), Ngày 7 |

**Nếu không kịp, cắt theo thứ tự này (giữ nguyên PCCC, giá điện, hợp đồng, hóa đơn, OData, JWT):**

1. Sao chép tài sản giữa các phòng
2. Gia hạn hợp đồng qua yêu cầu (Landlord sửa EndDate trực tiếp là đủ)
3. Quyết toán cọc chi tiết (chỉ ghi số tiền hoàn + ghi chú)
4. Tạo phòng hàng loạt (giữ tạo từng phòng có chọn tầng)
5. Đánh giá và khiếu nại
6. Mọi mục tầng Nên có

### Bước 0 — dán đầu tiên

```
Đọc CLAUDE.md, docs/NestU-design.md và docs/PROMPTS.md. Hôm nay 31/10, hạn nộp 3/11, làm theo "Lịch nén 3 ngày" trong docs/PROMPTS.md.
Kiểm tra máy đã có .NET 8 SDK, dotnet-ef và SQL Server LocalDB chưa (chạy dotnet --list-sdks, dotnet ef --version, sqllocaldb info) và báo kết quả.
Tóm tắt ngắn những gì sẽ làm tối nay. Chưa tạo project.
```

---

# Lời nhắc theo kế hoạch gốc

Mở terminal ở thư mục `NestU/`, gõ `claude`, rồi dán lần lượt từng lời nhắc. Chạy thử xong mỗi bước mới dán bước tiếp. Mỗi người chỉ dán phần thuộc mảng của mình (từ ngày 2).

---

## Ngày 1 — Nền tảng (Mảng 1)

### 1.1 Dựng hai project

```
Đọc CLAUDE.md và docs/NestU-design.md (mục Kiến trúc và xác thực, Thiết kế CSDL).
Tạo hai project .NET 8 theo đúng cấu trúc thư mục trong CLAUDE.md:
- NestU.API (webapi, có NestU.API.sln riêng), cài EF Core SqlServer, EF Core Tools, Identity EntityFrameworkCore, JwtBearer, OData, Swashbuckle.
- NestU.WebApp (mvc, có NestU.WebApp.sln riêng), Bootstrap 5.
Cố định cổng 7001 và 7002 trong launchSettings.json. Thư mục gốc đã có .gitignore, không tạo lại.
Chưa viết nghiệp vụ. Build cả hai, báo kết quả.
```

### 1.2 Entity, DbContext, ràng buộc CSDL

```
Theo bảng Thiết kế CSDL và bảng Ràng buộc dữ liệu trong docs/NestU-design.md, tạo toàn bộ Entity và Enum tầng Bắt buộc trong NestU.API/Entities
(bỏ Notification, AuditLog, ResidenceDeclaration, FavoriteRoom).
User kế thừa IdentityUser. Viết NestUDbContext kế thừa IdentityDbContext và mỗi entity một file IEntityTypeConfiguration trong Data/Configurations,
gồm đủ CHECK constraint, unique index, filtered unique index, RowVersion, kiểu decimal, OnDelete hợp lý (tránh multiple cascade paths).
Connection string LocalDB tên NestUDb. Tạo migration InitialCreate và update database. Liệt kê các CHECK và index đã tạo.
```

### 1.3 Seed + Identity + JWT

```
Thêm Data/Seed/DbSeeder.cs chạy khi khởi động (gọi Database.Migrate() trước, chỉ ở môi trường Development): 3 role, tài khoản và dữ liệu mẫu như mục "Dữ liệu seed" trong CLAUDE.md (mật khẩu lấy từ appsettings.Development.json).
Cấu hình Identity + JWT (Helpers/JwtTokenGenerator), policy ApprovedLandlord, ProblemDetails, Swagger có nút Authorize.
Làm AuthController: register (Tenant có IsStudent, hoặc Landlord ở trạng thái Pending), login trả TokenDto, me.
Test trên Swagger: đăng nhập từng tài khoản seed, gọi /api/auth/me.
```

### 1.4 Khung Web App

```
Trong NestU.WebApp: cấu hình cookie authentication theo mục Xác thực của CLAUDE.md, JwtHandler, base ApiClient xử lý 401/403/400 (đổ lỗi ProblemDetails vào ModelState),
AuthApiClient, AccountController (Login, Register có checkbox sinh viên ẩn hiện MSSV/trường, Logout, AccessDenied).
_Layout với navbar ẩn hiện menu theo role, Areas Tenant/Landlord/Admin (mỗi Area một trang Index trống có [Authorize(Roles)]).
wwwroot/css/site.css ghi đè biến Bootstrap: primary #FF6A3D, font Inter, bo góc 12px. Chạy cả hai và kiểm tra đăng nhập 3 role.
```

### 1.5 (Mảng 2–5, buổi chiều) DTO và HTML tĩnh

```
Đọc CLAUDE.md. Mình phụ trách Mảng <số> trong bảng phân công của docs/NestU-design.md.
Viết các DTO (có DataAnnotations) và interface Service cho mảng này theo bảng Endpoint API. Chưa viết implementation.
Sau đó dựng các View HTML tĩnh bằng Bootstrap cho các trang của mảng này (dữ liệu giả, chưa gọi API), dùng site.css chung.
```

---

## Ngày 2–3 — API theo mảng

```
Đọc CLAUDE.md và các mục liên quan đến Mảng <số> trong docs/NestU-design.md (chức năng, ràng buộc, luồng xử lý, endpoint).
Làm toàn bộ API của mảng này theo kế hoạch Ngày <2 hoặc 3>: Service + Controller, đúng route, đúng [Authorize], kiểm tra sở hữu, đúng mã lỗi.
Thao tác nhiều bảng chạy trong transaction. Xong thì liệt kê endpoint đã làm và cách test từng cái trên Swagger, kể cả các case lỗi cố ý.
```

Lời nhắc riêng cho OData (Mảng 2, ngày 3):

```
Làm /odata/Rooms theo mục 1.9: EDM model với RoomListDto phẳng (houseName, address, floor, availabilityLabel, viewCount),
Select sang DTO trước, chỉ nhà trọ Active, cho phép $filter, $orderby, $top, $skip, $select, $count, giới hạn MaxTop = 50.
Cho mình 5 URL ví dụ để test.
```

---

## Ngày 4–5 — Web App theo mảng

```
Đọc CLAUDE.md. Làm Web App cho Mảng <số>: tạo Models/ tương ứng DTO (cùng tên, có DataAnnotations), ApiClient của mảng,
nối các View HTML tĩnh vào API. Dùng TagHelper (asp-for, asp-validation-for, asp-area, asp-action) và PartialView dùng chung.
Responsive trên điện thoại. Xong thì liệt kê các trang và luồng thao tác để mình kiểm tra.
```

---

## Ngày 6 — Ghép nối

```
Chạy kịch bản demo trong mục Ngày 6 của docs/NestU-design.md bằng cách đọc code từng bước:
kiểm tra mỗi bước có endpoint và trang tương ứng, trạng thái chuyển đúng theo bảng Vòng đời trạng thái.
Liệt kê chỗ thiếu hoặc sai, rồi sửa từng chỗ. Bổ sung dữ liệu seed cho demo đẹp.
```

## Ngày 7 — Báo cáo

```
Dựa trên code hiện tại, cập nhật docs/NestU-design.md cho khớp thực tế (endpoint, entity đã đổi),
và viết docs/HUONG-DAN-CHAY.md: yêu cầu cài đặt, cách update database, tài khoản demo, thứ tự chạy API rồi WebApp.
```
