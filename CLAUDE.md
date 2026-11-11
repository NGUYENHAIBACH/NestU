# NestU — đồ án PRN232

Nền tảng quản lý nhà trọ cho sinh viên và người đi làm. Gồm **2 project chạy riêng**: một Web API và một Web App MVC chỉ lấy dữ liệu qua API đó.

- Thiết kế đầy đủ (nguồn sự thật): `docs/NestU-design.md` — **đọc mục liên quan trước khi code bất kỳ chức năng nào**.
- Đề bài: `docs/de-bai-PRN232.png`.
- **Trạng thái (31/10):** toàn bộ tầng Bắt buộc đã được viết (API + Web App) nhưng **chưa biên dịch lần nào**. Việc đầu tiên: chạy `setup.ps1` (hoặc `dotnet build` từng project), sửa lỗi biên dịch, rồi chạy kịch bản demo trong `docs/HUONG-DAN-CHAY.md` và sửa lỗi runtime. Hạn nộp 3/11.
- Khi sửa lỗi: sửa đúng chỗ sai, không viết lại cả file; giữ nguyên cấu trúc và quy tắc bên dưới.
- Nếu yêu cầu của người dùng mâu thuẫn với tài liệu thiết kế, hỏi lại trước khi làm.

## Công nghệ

- .NET 8, ASP.NET Core Web API, EF Core (SQL Server LocalDB), OData (`Microsoft.AspNetCore.OData`), ASP.NET Core Identity + JWT, Swagger
- Web App: ASP.NET Core MVC, Bootstrap 5, TagHelper, PartialView, typed HttpClient, cookie authentication
- Không dùng background job (BackgroundService, Hangfire…). Không dùng SignalR, bản đồ, VietQR, SuperAdmin trừ khi người dùng yêu cầu rõ.

## Cấu trúc thư mục

```
NestU/
├── CLAUDE.md
├── docs/
├── NestU.API/              solution + project riêng, chạy https://localhost:7001 (Swagger)
│   ├── Controllers/        AuthController, Public/, Tenant/, Landlord/, Admin/, OData/
│   ├── Services/           Interfaces/ + các Service nghiệp vụ
│   ├── Data/               NestUDbContext.cs, Configurations/ (Fluent API), Seed/
│   ├── Migrations/
│   ├── Entities/           class ánh xạ bảng + Enums/
│   ├── DTOs/               theo module: Rooms/, Houses/, Contracts/, Invoices/...
│   ├── Helpers/            JwtTokenGenerator, mapping, tính khoảng cách
│   └── Program.cs
└── NestU.WebApp/           solution + project riêng, chạy https://localhost:7002
    ├── Controllers/        Home, Account, Rooms (trang công khai)
    ├── Areas/              Tenant/, Landlord/, Admin/ (Controllers + Views)
    ├── Views/Shared/       _Layout, _RoomCard, _FloorRow, _AssetTable, _StatusBadge, _Pagination
    ├── Models/             model cùng tên với DTO của API + DataAnnotations riêng
    ├── ApiClients/         typed HttpClient theo module
    ├── Handlers/           JwtHandler (DelegatingHandler gắn Bearer token)
    ├── wwwroot/            css/site.css, js, images, logo.svg
    └── Program.cs
```

Mỗi project có file `.sln` riêng trong thư mục của nó. Web App **không tham chiếu** project API, không có DbContext, không có connection string.

## Quy tắc bắt buộc khi code

1. Controller chỉ gọi Service; chỉ Service dùng `NestUDbContext`. Controller nhận và trả **DTO**, không bao giờ trả Entity.
2. Mỗi DTO bên API có model cùng tên trong `NestU.WebApp/Models/`; sửa DTO thì sửa luôn model.
3. Route nhóm theo role: `api/auth`, `api/public`, `odata`, `api/tenant`, `api/landlord`, `api/admin`. Mỗi controller gắn `[Authorize(Roles = "...")]`; Service **luôn** kiểm tra quyền sở hữu (Landlord chỉ sửa nhà trọ của mình → 403).
4. Ràng buộc dữ liệu chặn ở nhiều lớp theo bảng "Ràng buộc dữ liệu" trong tài liệu: CHECK / unique / filtered unique index trong `Data/Configurations/`, DataAnnotations trên DTO, kiểm tra nghiệp vụ trong Service, DataAnnotations + `asp-validation-for` ở Web App.
5. Mã lỗi: 400 validation/vi phạm nghiệp vụ, 401 chưa đăng nhập, 403 sai role/không sở hữu/Landlord chưa duyệt, 404, 409 xung đột (trùng số phòng, phòng đã bị thuê, hóa đơn trùng tháng, biên bản đã xác nhận). Lỗi trả `ProblemDetails`.
6. Thao tác nhiều bảng (duyệt thuê, tạo phòng hàng loạt, quyết toán cọc) chạy trong một transaction.
7. JSON camelCase. Thông báo lỗi và giao diện bằng **tiếng Việt**; tên class, biến, route bằng tiếng Anh.
8. Cổng cố định trong `Properties/launchSettings.json`: API 7001, WebApp 7002. Web App đọc `ApiSettings:BaseUrl`.

## Quy định nghiệp vụ cốt lõi

- **Role:** Admin, Landlord, Tenant (Guest = chưa đăng nhập). Landlord đăng ký ở trạng thái `Pending` trong `LandlordProfile`; chưa `Approved` thì mọi `/api/landlord/*` (trừ `/profile`) trả 403 qua policy `ApprovedLandlord`.
- **Tenant:** `IsStudent` = true thì bắt buộc MSSV và trường. Nhà trọ `StudentOnly` chỉ nhận sinh viên.
- **Số điện thoại:** mỗi số chỉ gắn với một tài khoản, xét `TenantProfile`, `LandlordProfile` và `AspNetUsers.PhoneNumber` (`Helpers/PhoneNumbers`, 409 nếu trùng; không thêm unique index). SĐT người ở cùng chỉ cấm trùng nhau trong một hợp đồng.
- **Trường đại học:** sinh viên chọn gợi ý hoặc nhập tay `UniversityName`; `Helpers/Universities.ResolveAsync` khớp tên/tên viết tắt (không phân biệt hoa thường), chưa có thì tạo `University` mới (tọa độ 0) cùng lần lưu hồ sơ. `UniversityId` gửi kèm thì ưu tiên.
- **Đổi mật khẩu:** Tenant/Landlord đổi trong trang hồ sơ, cần mật khẩu hiện tại + OTP gửi về email (`api/auth/change-password/otp`, `api/auth/change-password`); môi trường dev in OTP ra log API.
- **CCCD:** mỗi số CCCD chỉ gắn với một tài khoản, xét cả `TenantProfile` lẫn `LandlordProfile` (đăng ký, sửa hồ sơ → 409 nếu trùng). Kiểm tra ở Service (`Helpers/CitizenIds`), không thêm unique index để CSDL không đổi. CCCD người ở cùng (`ContractMember`) không xét.
- **Khu vực:** nhà trọ lưu `AreaId` như cũ nhưng chủ trọ không phải chọn tay. Form gửi `DistrictName` (quận/huyện từ bộ chọn địa chỉ); `HouseService` so khớp chính xác theo tên đã chuẩn hóa (`Helpers/AreaNames`: bỏ "Quận/Huyện/Thị xã/Thành phố", quận số giữ "Quận N"), chưa có thì tạo `Area` mới cùng lần lưu. `AreaId` gửi kèm thì ưu tiên. Admin thấy số nhà trọ mỗi khu vực và gộp hai khu vực trùng nghĩa (`POST /api/admin/catalog/areas/{id}/merge`). Bộ lọc Tìm phòng dùng `LookupsDto.SearchAreas` (chỉ khu vực có nhà trọ Active, không ẩn).
- **PCCC:** nhà trọ tạo ra ở `Pending`; Landlord upload giấy (số giấy 3–50 ký tự có chữ số, không trùng nhà trọ khác; file JPG/PNG/PDF ≤ 5 MB, `FileStorage` kiểm tra magic bytes); Admin duyệt một lần → nhà trọ `Active`. Không có ngày hết hạn, không có `Suspended`. Admin ẩn tin bằng cờ `BoardingHouse.IsHidden` (`PUT /api/admin/houses/{id}/hidden` hoặc khi xử lý khiếu nại). Chỉ nhà trọ `Active` và không `IsHidden` hiện công khai (OData lọc ở server) và nhận lịch xem, yêu cầu thuê.
- **Giá điện:** 1–4.000đ/kWh, ghi cứng ở CHECK, `[Range(1, 4000)]` trên DTO và model Web App. Đơn giá lưu vào `InvoiceDetail.UnitPrice` lúc lập hóa đơn.
- **Tầng:** `Room.Floor`, `BoardingHouse.TotalFloors`, `1 ≤ Floor ≤ TotalFloors`. Tạo hàng loạt: số phòng = `floor × 100 + n`, tối đa 30 phòng/lần, trùng số → 409 và không tạo phòng nào.
- **Tài sản:** `RoomAsset`; khi duyệt thuê sao chép thành `ContractAsset` (Stage = CheckIn); Tenant xác nhận thì khóa; trả phòng ghi CheckOut + khấu trừ, cập nhật ngược `RoomAsset.Condition`.
- **Tiền cọc:** ghi `Contract.DepositAmount`, `DepositPaidAt` khi duyệt thuê; quyết toán bằng `DepositSettlement`. Service cắt khấu trừ về tối đa bằng cọc (trừ hư hỏng trước, rồi hóa đơn từ cũ đến mới; CHECK ở CSDL chỉ là lớp chặn cuối): hóa đơn trừ đủ → `Paid`, trừ một phần → ghi `Payment` phần đã trừ và giữ `Unpaid`, `InvoiceDto.RemainingAmount` là số còn phải trả. Biên bản trả phòng ghi số lượng 0 (thất lạc) → `RoomAsset.Condition = Broken`, không xóa `RoomAsset`.
- **Tính khi đọc, không lưu:** hóa đơn `Unpaid` quá `DueDate` → DTO trả `Overdue`; hợp đồng `Active` quá `EndDate` → hiển thị "Đã hết hạn".
- **Hợp đồng:** filtered unique `(TenantId) WHERE Status='Active'` và `(RoomId) WHERE Status='Active'`; `Room.RowVersion` chống duyệt trùng → 409.
- **Hóa đơn:** unique `(ContractId, Month, Year)`; chỉ số điện nước tháng này ≥ tháng trước và ≤ kỳ sau nếu đã ghi (`GET /api/landlord/readings/bounds` cho form kiểm tra trước); nước tính theo m³ (`PerM3`) hoặc theo người (`PerPerson`).
- **ViewCount:** tăng khi `GET /api/rooms/{id}`, khóa `roomId + (userId hoặc visitorId)` 30 phút bằng `IMemoryCache`. Web App tạo cookie `nestu_visitor` (GUID) và gửi header `X-Visitor-Id`; không dùng IP cho khách qua Web App vì Web App gọi API từ server (IP chỉ là dự phòng khi gọi thẳng API không có header).

## Xác thực

- API: Identity lưu user/role, `POST /api/auth/login` trả `TokenDto` (JWT 60 phút chứa userId, role, fullName).
- Web App: sau khi login, `HttpContext.SignInAsync` với cookie chứa claim `NameIdentifier`, `Name`, `Role`, và claim `access_token`; cookie hết hạn cùng JWT. `JwtHandler` lấy claim `access_token` gắn header Bearer. API trả 401 → đăng xuất về Login; 403 → trang "Không có quyền". Lỗi validation từ API đổ vào `ModelState`.

## Dữ liệu seed (dùng chung cả nhóm, mật khẩu đọc từ appsettings.Development.json)

- 1 Admin, 2 Landlord (1 đã duyệt, 1 Pending), 3 Tenant (2 sinh viên, 1 khách thường)
- Khu vực, tiện ích, loại tài sản, vài trường đại học có tọa độ
- 1 nhà trọ Active 3 tầng có phòng, tài sản, dịch vụ để demo nhanh

## Cách làm việc

- Làm theo `docs/PROMPTS.md` (mục "Sau khi code xong"), từng bước nhỏ. Sau mỗi bước: build không lỗi (`dotnet build`), chạy thử, rồi mới sang bước tiếp.
- Tạo migration bằng `dotnet ef migrations add <Ten>` trong `NestU.API`; không sửa migration đã áp dụng.
- Không thêm thư viện ngoài danh sách công nghệ nếu chưa hỏi.
- Khi có quyết định thiết kế mới, cập nhật file này và ghi chú vào `docs/NestU-design.md`.
