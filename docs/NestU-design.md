# NestU — Tài liệu hệ thống (bản chốt)

Oct 26, 2026 · @Bách Nguyễn Hải

> **Ghi chú 31/10:** nhóm bắt đầu code tối 31/10, hạn nộp 3/11. Mục "Lộ trình và phân công" bên dưới là kế hoạch 7 ngày gốc; lịch thực tế đã nén còn 3 ngày, xem `docs/PROMPTS.md`.

## Thay đổi so với bản 29/09

Bản này giữ nguyên kiến trúc và hai quy định cốt lõi (PCCC, giá điện ≤ 4.000đ/kWh), nhưng thu hẹp phạm vi bắt buộc, vá các chỗ hở về dữ liệu và thêm quản lý tầng, tài sản cho Landlord.

| # | Thay đổi | Lý do |
| --- | --- | --- |
| 1 | Chia chức năng thành 3 tầng: Bắt buộc, Nên có, Mở rộng | Bản cũ \~60 endpoint, 25 entity; rủi ro không xong phần lõi |
| 2 | SuperAdmin, 2FA, AuditLog chuyển xuống tầng Mở rộng; mặc định seed 1 Admin | Đề không yêu cầu; tốn cả một nhóm route |
| 3 | Thêm `Room.Floor`, `BoardingHouse.TotalFloors`, tạo phòng hàng loạt theo tầng | Nhà trọ thực tế chia tầng, chủ trọ không muốn tạo từng phòng |
| 4 | Thêm `RoomAsset`, `ContractAsset` (biên bản bàn giao) | Quản lý tài sản phòng, làm căn cứ trừ cọc khi trả phòng |
| 5 | Thêm `LandlordProfile` (CCCD, SĐT, ApprovalStatus) | Bản cũ có duyệt chủ trọ nhưng không có nơi lưu trạng thái duyệt |
| 6 | Thêm `WaterPrice` + `WaterBillingType` trên nhà trọ | Hóa đơn có tiền nước nhưng thiếu đơn giá nước |
| 7 | Thêm luồng tiền cọc: ghi nhận khi duyệt thuê, quyết toán khi trả phòng (`DepositSettlement`) | Bản cũ có tiền cọc nhưng không có vòng đời |
| 8 | Giá trần điện ghi cứng 4.000, bỏ `SystemSetting` sửa được | Tránh mâu thuẫn giữa `[Range]`, CHECK và cấu hình |
| 9 | Filtered unique index cho hợp đồng Active theo Tenant và theo Room | Thể hiện ràng buộc ở tầng CSDL (mục 1.5) |
| 10 | Cookie hết hạn bằng JWT (60 phút, sliding qua refresh) | Tránh bị đá ra khi cookie còn sống nhưng JWT đã hết |
| 11 | `ViewCount` khóa theo `roomId + IP/userId` bằng `IMemoryCache` | Ghi rõ cách đếm "mỗi trình duyệt 30 phút" |
| 12 | OData trả `RoomListDto` phẳng (có sẵn tên, địa chỉ nhà trọ), bỏ `$expand` | `$expand` trên DTO tốn công cấu hình EDM |
| 13 | Đổi tên project `BHM.*` thành `NestU.*` | Khớp tên nền tảng trong báo cáo |
| 14 | Thêm việc nền tảng (tuần 1) do 1 người làm trước khi tách mảng | Tránh mỗi người tự viết một kiểu gọi API |

**Cập nhật 26/10 (đơn giản hóa):** PCCC chỉ còn duyệt một lần, không theo dõi ngày hết hạn, bỏ trạng thái Suspended và Expired. Hóa đơn Overdue và hợp đồng hết hạn tính khi đọc dữ liệu, nên toàn bộ dự án không cần background job.

**Cập nhật 26/10 (cấu trúc code):** API và Web App là hai project riêng, mỗi project một solution. API gộp còn 1 project, chia tầng bằng thư mục (`Controllers`, `Services`, `Data`, `Entities`, `DTOs`). Bỏ `NestU.Shared`: DTO nằm bên API, Web App có model riêng với validation riêng. Bảng đối chiếu đề ghi rõ từng mục nằm ở thư mục nào và demo ra sao.

## Tổng quan

NestU là nền tảng quản lý nhà trọ cho sinh viên và người đi làm (đề tài 2, dự án cuối môn PRN232), gồm một Web API và một Web App MVC chỉ lấy dữ liệu qua API đó. Hai điểm khác biệt: chỉ nhà trọ đạt chuẩn PCCC mới được hoạt động, và giá điện tối đa 4.000đ/kWh.

| Hạng mục | Lựa chọn |
| --- | --- |
| Tên nền tảng | NestU (nest + U: tổ ấm cho sinh viên đại học) |
| Backend | ASP.NET Core 8 Web API, EF Core, SQL Server, OData, JWT; SignalR (Nên có) |
| Frontend | ASP.NET Core 8 MVC, Bootstrap 5, TagHelper, PartialView |
| Xác thực | ASP.NET Core Identity + JWT ở API; cookie authentication mang claim role ở Web App |
| Logo | Vòng tổ có đỉnh mái nhà, chữ U ở giữa, chấm cam góc dưới phải (`logo.svg`) |
| Màu thương hiệu | Nền đen `#000`, trắng `#fff`, nhấn cam `#FF6A3D` |
| Font | Inter (giao diện), VT323 (pixel, đủ dấu tiếng Việt) |

## Phạm vi và thứ tự ưu tiên

Nhóm hoàn thành toàn bộ tầng Bắt buộc trước khi chạm vào tầng Nên có; tầng Mở rộng chỉ làm khi còn thời gian. Mọi bảng trong tài liệu đánh dấu (Nên có) hoặc (Mở rộng) cho những phần không thuộc tầng Bắt buộc.

| Tầng | Chức năng | Ghi chú |
| --- | --- | --- |
| Bắt buộc | Đăng ký, đăng nhập, JWT, 4 role (Admin, Landlord, Tenant, Guest) | Đủ mục 1.10, 2.7 |
| Bắt buộc | CRUD nhà trọ, phòng theo tầng, tạo phòng hàng loạt, tài sản phòng |  |
| Bắt buộc | Upload giấy PCCC, Admin duyệt một lần | Quy định 1 |
| Bắt buộc | Tìm phòng bằng OData, lượt xem, nhãn Còn phòng / Hết phòng | Mục 1.9 |
| Bắt buộc | Lịch xem, yêu cầu thuê, hợp đồng, tiền cọc, bàn giao tài sản |  |
| Bắt buộc | Dịch vụ, chỉ số điện nước, hóa đơn, báo và xác nhận thanh toán | Quy định 2 |
| Bắt buộc | Sửa chữa, đánh giá, khiếu nại; Admin duyệt chủ trọ, quản lý tài khoản, danh mục |  |
| Nên có | Thông báo realtime SignalR | Đã thiết kế đầy đủ, gây ấn tượng khi demo |
| Nên có | Bản đồ Leaflet, lọc theo khoảng cách tới trường |  |
| Nên có | Thống kê Landlord và Admin, biểu đồ Chart.js |  |
| Nên có | Mã VietQR cho hóa đơn | Chỉ sinh chuỗi, không cần cổng thanh toán |
| Mở rộng | SuperAdmin ẩn, AuditLog, 2FA | Khi không làm: seed sẵn 1 Admin |
| Mở rộng | Khai báo tạm trú, xuất hợp đồng PDF (QuestPDF) |  |
| Mở rộng | Phòng yêu thích, so sánh phòng, chat, VNPay sandbox |  |

## Tác nhân hệ thống

Hệ thống có 4 role công khai; SuperAdmin là role ẩn thuộc tầng Mở rộng. Mỗi role chỉ thao tác trên phạm vi dữ liệu của mình, và API kiểm tra cả role lẫn quyền sở hữu trên từng bản ghi.

| Role | Mô tả | Phạm vi dữ liệu |
| --- | --- | --- |
| Admin | Quản lý nền tảng, duyệt chủ trọ và giấy PCCC, xử lý khiếu nại, quản lý danh mục, xem báo cáo tổng | Toàn hệ thống (trừ tài khoản SuperAdmin nếu có) |
| Landlord (chủ trọ) | Quản lý nhà trọ, tầng, phòng, tài sản, hợp đồng, tiền cọc, điện nước, hóa đơn, sự cố | Chỉ nhà trọ do mình sở hữu, và chỉ thao tác được khi tài khoản đã được duyệt |
| Tenant (sinh viên hoặc khách thường) | Tìm phòng, đặt lịch xem, gửi yêu cầu thuê, xem hợp đồng, biên bản tài sản, hóa đơn, báo sự cố | Chỉ hợp đồng, hóa đơn, yêu cầu của chính mình |
| Guest (chưa đăng nhập) | Xem, tìm kiếm, lọc phòng công khai, đăng ký tài khoản | Chỉ nhà trọ đang Active |
| SuperAdmin (Mở rộng, ẩn) | Cấp, thu hồi role Admin, mở khóa Admin | Chỉ tài khoản Admin và AuditLog |

**Sinh viên và khách thường.** Khi đăng ký, Tenant chọn `IsStudent`. Sinh viên khai thêm MSSV và trường; khách thường chỉ cần họ tên, số điện thoại, CCCD. Mọi luồng thuê, hợp đồng, hóa đơn dùng chung cho cả hai. Nhà trọ đặt `AllowedTenantType` là `All` hoặc `StudentOnly`; phòng của nhà trọ `StudentOnly` hiện nhãn "Chỉ sinh viên" và API từ chối yêu cầu thuê từ người không phải sinh viên.

**Landlord chờ duyệt.** Landlord đăng ký xong ở trạng thái `Pending` trong `LandlordProfile`: đăng nhập được, xem được trang hồ sơ, nhưng mọi endpoint `/api/landlord/*` trả 403 cho tới khi Admin duyệt `Approved`.

**Staff (tuỳ chọn).** Nếu nhóm đông, thêm role Staff do chủ trọ giao ghi chỉ số điện nước và xử lý sự cố cho một nhà trọ cụ thể.

## Chức năng nghiệp vụ theo role

Mục không đánh dấu thuộc tầng Bắt buộc.

### Guest

- Xem danh sách nhà trọ Active và phòng, xem chi tiết phòng: ảnh, tầng, tiện ích, tài sản có sẵn, giá điện nước, lượt xem, nhãn Còn phòng / Hết phòng
- Tìm kiếm, lọc theo khu vực, giá, diện tích, tầng, tiện ích (OData)
- Lọc theo khoảng cách tới trường, xem nhà trọ trên bản đồ (Nên có)
- Đăng ký tài khoản Tenant (sinh viên hoặc khách thường) hoặc Landlord

### Tenant

- Quản lý hồ sơ: họ tên, CCCD, số điện thoại; MSSV và trường nếu là sinh viên
- Đặt lịch xem phòng, hủy khi còn Pending
- Gửi yêu cầu thuê kèm số người ở và ngày dọn vào; theo dõi Pending, Approved, Rejected, Cancelled
- Xem hợp đồng hiện tại, lịch sử thuê, số tiền cọc đã đóng
- Xem biên bản bàn giao tài sản khi nhận phòng và khi trả phòng
- Xem hóa đơn hằng tháng, báo đã thanh toán kèm ảnh chuyển khoản; quét mã VietQR (Nên có)
- Báo sự cố, có thể gắn với một tài sản cụ thể (máy lạnh hỏng, vòi nước rò); theo dõi tiến độ
- Gửi yêu cầu gia hạn hoặc trả phòng
- Đánh giá phòng đã từng thuê; khiếu nại chủ trọ hoặc tin đăng

### Landlord

- CRUD nhà trọ: địa chỉ, khu vực, vị trí bản đồ, số tầng (`TotalFloors`), đối tượng thuê, giá điện (≤ 4.000), giá nước, tài khoản nhận tiền
- Upload giấy chứng nhận PCCC để Admin duyệt
- **Quản lý phòng theo tầng:** xem phòng nhóm theo tầng ("Tầng 2: 5 phòng, còn 2"); tạo một phòng hoặc tạo hàng loạt cho một tầng (ví dụ tầng 3, phòng 301–306, cùng diện tích, giá, sức chứa, tiện ích, tài sản)
- CRUD phòng: số phòng, tầng, diện tích, giá, sức chứa, tiện ích, ảnh, trạng thái (Available, Maintenance); xem lượt xem từng phòng
- **Quản lý tài sản phòng:** thêm, sửa, xóa tài sản (giường, tủ, máy lạnh, bình nóng lạnh…) với số lượng, tình trạng, giá trị ước tính; sao chép danh sách tài sản từ phòng này sang các phòng khác
- Xác nhận hoặc từ chối lịch xem; duyệt hoặc từ chối yêu cầu thuê
- Quản lý hợp đồng: tạo khi duyệt thuê, gia hạn, chấm dứt; ghi nhận tiền cọc
- Lập biên bản bàn giao khi nhận phòng (tự sinh từ tài sản phòng), kiểm tra khi trả phòng, quyết toán cọc (khấu trừ hư hỏng, tiền còn nợ)
- Quản lý dịch vụ (wifi, rác, gửi xe) và đơn giá
- Ghi chỉ số điện nước hằng tháng; lập hóa đơn, xác nhận hoặc từ chối thanh toán
- Xử lý sự cố; khi tài sản hỏng, cập nhật tình trạng tài sản
- Thống kê doanh thu theo tháng, tỷ lệ lấp đầy theo tầng, hóa đơn chưa thanh toán (Nên có)

### Admin

- Quản lý tài khoản: khóa, mở, gán role Tenant hoặc Landlord
- Duyệt hồ sơ chủ trọ và giấy chứng nhận PCCC
- Xử lý khiếu nại: ẩn tin, khóa tài khoản hoặc bác bỏ
- Quản lý danh mục chung: khu vực, tiện ích, loại tài sản, trường đại học (kèm tọa độ)
- Dashboard thống kê toàn hệ thống (Nên có)

### SuperAdmin (Mở rộng)

- Cấp hoặc thu hồi role Admin; hệ thống luôn giữ ít nhất 1 Admin hoạt động
- Mở khóa, đặt lại mật khẩu Admin; xem AuditLog
- Tạo bằng seed, mật khẩu lấy từ User Secrets hoặc biến môi trường; không hiện trong danh sách người dùng; đăng nhập ở route riêng; 2FA nếu kịp

## Quy định nghiệp vụ

Bốn quy định dưới đây được kiểm tra ở nhiều lớp; hai quy định đầu là điểm khác biệt của đề tài và được chặn ở cả CSDL, API và giao diện.

### 1. Nhà trọ phải đạt chuẩn PCCC mới được hoạt động

1. Landlord tạo nhà trọ, trạng thái `Pending`, chưa hiển thị công khai.
2. Landlord upload giấy PCCC: số giấy và file ảnh hoặc PDF.
3. Admin duyệt: hợp lệ thì giấy `Approved`, nhà trọ `Active`; không hợp lệ thì giấy `Rejected` kèm lý do, Landlord upload lại.
4. Giấy chỉ duyệt một lần, hệ thống không theo dõi ngày hết hạn. Chỉ nhà trọ `Active` mới hiện công khai và nhận yêu cầu thuê.

| Lớp | Cách kiểm tra |
| --- | --- |
| CSDL | Mỗi nhà trọ tối đa một giấy Approved: filtered unique `(BoardingHouseId) WHERE Status = 'Approved'` |
| API | Tạo hoặc duyệt RentalRequest: nhà trọ phải `Active`, nếu không trả 400 |
| OData | Lọc `Status == Active` ở phía server trong `IQueryable` trả về |
| Phân quyền | Endpoint duyệt giấy chỉ cho Admin |
| Giao diện | Badge "Đạt PCCC" trên thẻ phòng; trang quản lý nhà trọ hiện trạng thái duyệt và lý do từ chối |

### 2. Giá điện không quá 4.000đ/kWh

Giá trần ghi cứng là 4.000 ở cả ba lớp, không cấu hình được. Tiền điện = (chỉ số mới − chỉ số cũ) × đơn giá; đơn giá tại thời điểm lập lưu vào `InvoiceDetail.UnitPrice`, nên đổi giá sau này không làm thay đổi hóa đơn cũ.

| Lớp | Cách kiểm tra |
| --- | --- |
| CSDL | `CHECK ([ElectricityPrice] > 0 AND [ElectricityPrice] <= 4000)` |
| DTO (API) | `[Range(1, 4000, ErrorMessage = "Giá điện tối đa 4.000đ/kWh")]` |
| Web App | Model của Web App cũng gắn Range(1, 4000), lỗi hiện bằng `asp-validation-for`, ghi chú "Tối đa 4.000đ/kWh" cạnh ô nhập |

### 3. Tiền cọc

Tiền cọc không có cổng thanh toán riêng; hệ thống chỉ ghi nhận và quyết toán.

1. Tenant gửi yêu cầu thuê; số tiền cọc lấy từ `Room.DepositAmount` (mặc định bằng 1 tháng tiền phòng).
2. Tenant đóng cọc trực tiếp hoặc chuyển khoản cho chủ trọ ngoài hệ thống.
3. Landlord duyệt yêu cầu thuê, xác nhận đã nhận cọc: `Contract.DepositAmount` và `DepositPaidAt` được ghi.
4. Khi hợp đồng kết thúc, Landlord lập `DepositSettlement`: khấu trừ hư hỏng tài sản (từ biên bản trả phòng) và hóa đơn còn nợ; số còn lại hoàn cho Tenant. Tổng khấu trừ không vượt tiền cọc.

### 4. Bàn giao tài sản

1. Khi hợp đồng được tạo, hệ thống sao chép toàn bộ `RoomAsset` của phòng thành `ContractAsset` (giai đoạn `CheckIn`) với số lượng, tình trạng tại thời điểm nhận phòng.
2. Tenant xem biên bản và xác nhận; từ lúc đó biên bản nhận phòng bị khóa.
3. Khi trả phòng, Landlord ghi tình trạng từng tài sản (giai đoạn `CheckOut`) và tiền khấu trừ nếu hư hỏng, thất lạc.
4. Tình trạng `CheckOut` được cập nhật ngược lại vào `RoomAsset` để phòng sẵn sàng cho người thuê sau.

## Thiết kế CSDL

CSDL gồm 28 bảng nghiệp vụ ở tầng Bắt buộc cộng các bảng Identity; phần in nghiêng là entity mới hoặc trường mới so với bản 29/09. Tầng được lưu thành cột `Room.Floor` chứ không thành bảng riêng, vì tầng không có thuộc tính nghiệp vụ nào ngoài số thứ tự.

| Entity | Vai trò | Trường / quan hệ chính |
| --- | --- | --- |
| User, Role | Tài khoản, phân quyền (Identity) | Role lưu trong DB: Admin, Landlord, Tenant (+ SuperAdmin nếu làm Mở rộng) |
| TenantProfile | Hồ sơ người thuê | UserId, FullName, CCCD, Phone, IsStudent, StudentCode, UniversityId (hai trường cuối chỉ khi IsStudent) |
| *LandlordProfile* | *Hồ sơ chủ trọ* | *UserId, FullName, CCCD, Phone, ApprovalStatus (Pending, Approved, Rejected), ReviewNote* |
| BoardingHouse | Nhà trọ | LandlordId, Name, Address, AreaId, Latitude, Longitude, *TotalFloors*, Status, ElectricityPrice, *WaterPrice*, *WaterBillingType (PerM3, PerPerson)*, AllowedTenantType, BankCode, BankAccount, AccountName |
| FireSafetyCertificate | Giấy PCCC | BoardingHouseId, CertificateNo, FileUrl, Status (Pending, Approved, Rejected), ReviewNote |
| Room | Phòng | BoardingHouseId, RoomNumber, *Floor*, Area, Price, *DepositAmount*, Capacity, Status, ViewCount, RowVersion |
| RoomImage | Ảnh phòng | RoomId, Url, SortOrder |
| Amenity, RoomAmenity | Tiện ích (n–n với Room) | wifi, máy lạnh, WC riêng, chỗ để xe |
| *AssetType* | *Danh mục loại tài sản (Admin quản lý)* | *Name (giường, tủ, máy lạnh, bình nóng lạnh, quạt, bàn học…)* |
| *RoomAsset* | *Tài sản trong phòng* | *RoomId, AssetTypeId, Name, Quantity, Condition (Good, Damaged, Broken), EstimatedValue, Note* |
| *ContractAsset* | *Biên bản bàn giao* | *ContractId, RoomAssetId, Stage (CheckIn, CheckOut), Quantity, Condition, DeductionAmount, Note, ConfirmedByTenantAt* |
| Area, University | Khu vực, trường đại học | Name; University có Latitude, Longitude |
| ViewingAppointment | Lịch xem phòng | RoomId, TenantId, ScheduledAt, Status |
| RentalRequest | Yêu cầu thuê | RoomId, TenantId, NumberOfOccupants, MoveInDate, Status, RejectReason |
| Contract, ContractMember | Hợp đồng, người ở cùng | RoomId, TenantId, StartDate, EndDate, MonthlyRent, DepositAmount, *DepositPaidAt*, Status; ContractMember: FullName, CCCD |
| *DepositSettlement* | *Quyết toán cọc* | *ContractId (unique), DepositAmount, DamageDeduction, UnpaidDeduction, RefundAmount, SettledAt, Note* |
| ContractChangeRequest | Yêu cầu gia hạn, trả phòng | ContractId, Type (Renew, Terminate), NewEndDate, Status |
| Service, ContractService | Dịch vụ và dịch vụ đăng ký theo hợp đồng | Service: BoardingHouseId, Name, Price, Unit (PerMonth, PerPerson) |
| UtilityReading | Chỉ số điện nước theo tháng | ContractId, Month, Year, ElectricIndex, WaterIndex |
| Invoice, InvoiceDetail, Payment | Hóa đơn, dòng hóa đơn, thanh toán | Invoice: ContractId, Month, Year, Total, DueDate, Status; InvoiceDetail: ItemName, Quantity, UnitPrice, Amount; Payment: Amount, ProofImageUrl, ConfirmedAt |
| MaintenanceRequest | Yêu cầu sửa chữa | ContractId, *RoomAssetId (tuỳ chọn)*, Title, Description, ImageUrl, Status |
| Review, Report | Đánh giá, khiếu nại | Review: ContractId (unique), Rating 1–5, Comment; Report: ReporterId, TargetType, TargetId, Reason, Status |
| Notification | Thông báo (Nên có) | UserId, Type, Title, Message, LinkUrl, IsRead, CreatedAt |
| AuditLog | Nhật ký (Mở rộng) | ActorId, Action, TargetUserId, CreatedAt |
| ResidenceDeclaration, FavoriteRoom | Tạm trú, yêu thích (Mở rộng) | như bản 29/09 |

`SystemSetting` bị bỏ vì giá trần điện ghi cứng. Ghi chỉ số nước là bắt buộc khi `WaterBillingType = PerM3`; khi `PerPerson`, tiền nước = `WaterPrice × số người trong hợp đồng`.

**Trạng thái tính khi đọc, không lưu:** hóa đơn `Unpaid` có `DueDate < hôm nay` được DTO trả là `Overdue`; hợp đồng `Active` có `EndDate < hôm nay` được hiển thị "Đã hết hạn" và chờ Landlord gia hạn hoặc chấm dứt. Nhờ vậy không cần job chạy định kỳ.

## Ràng buộc dữ liệu

Mỗi ràng buộc ghi rõ được kiểm tra ở lớp nào; ràng buộc nào chặn được ở CSDL thì chặn ở CSDL, phần còn lại kiểm tra trong Service (mục 1.5, 1.7, 2.8).

| Ràng buộc | CSDL | API (DTO / Service) | Web App |
| --- | --- | --- | --- |
| Giá điện 1–4.000; giá phòng, giá nước, tiền cọc > 0 | CHECK | `[Range]` | `asp-validation-for` |
| Số phòng không trùng trong một nhà trọ | Unique `(BoardingHouseId, RoomNumber)` | Trả 409 | Lỗi cạnh ô |
| Tầng hợp lệ: `1 ≤ Floor ≤ TotalFloors` | CHECK `Floor >= 1` | Service so với `TotalFloors` | Dropdown chỉ hiện 1..TotalFloors |
| Không giảm `TotalFloors` dưới tầng cao nhất đang có phòng |  | Service, trả 400 |  |
| Tạo hàng loạt tối đa 30 phòng một lần, `fromNumber ≤ toNumber` |  | `[Range]` + Service | Xem trước danh sách phòng sẽ tạo |
| Số lượng tài sản ≥ 1; tình trạng thuộc tập cho phép | CHECK `Quantity >= 1` | Enum + `[Range]` | Dropdown |
| Mỗi hợp đồng một dòng tài sản mỗi giai đoạn | Unique `(ContractId, RoomAssetId, Stage)` |  |  |
| Biên bản nhận phòng đã xác nhận thì không sửa |  | Service, trả 409 | Ẩn nút sửa |
| Tổng khấu trừ cọc ≤ tiền cọc | CHECK `DamageDeduction + UnpaidDeduction <= DepositAmount` | Service |  |
| Số người không vượt sức chứa phòng |  | Service | `max` trên ô nhập |
| Chỉ phòng Available trong nhà trọ Active nhận yêu cầu thuê |  | Service, trả 400 | Ẩn nút thuê |
| Ngày kết thúc sau ngày bắt đầu, tối thiểu 1 tháng | CHECK `EndDate > StartDate` | Custom validation attribute |  |
| Mỗi Tenant một hợp đồng Active | Filtered unique `(TenantId) WHERE Status = 'Active'` | Service |  |
| Mỗi phòng một hợp đồng Active | Filtered unique `(RoomId) WHERE Status = 'Active'` | RowVersion, trả 409 |  |
| Chỉ số điện nước tháng này ≥ tháng trước |  | Service, trả 400 |  |
| Mỗi hợp đồng một hóa đơn mỗi tháng | Unique `(ContractId, Month, Year)` | Trả 409 |  |
| Số điện thoại 10 số, CCCD 12 số | CHECK độ dài | `[RegularExpression]` | Cùng DataAnnotations trên model Web App |
| MSSV, trường bắt buộc khi IsStudent | CHECK `IsStudent = 0 OR StudentCode IS NOT NULL` | `IValidatableObject` | Ẩn hiện ô theo checkbox |
| Nhà trọ StudentOnly chỉ nhận sinh viên |  | Service, trả 400 | Nhãn "Chỉ sinh viên" |
| Chỉ người từng thuê mới được đánh giá, mỗi hợp đồng một lần | Unique `(ContractId)` trên Review | Service, trả 403 |  |
| Landlord chỉ sửa dữ liệu nhà trọ của mình |  | Service kiểm tra `LandlordId`, trả 403 |  |
| Landlord chưa duyệt không dùng được chức năng chủ trọ |  | Authorization policy `ApprovedLandlord`, trả 403 | Trang "Đang chờ duyệt" |

## Kiến trúc và xác thực

Hệ thống gồm hai project chạy riêng, mỗi project một solution: **NestU.API** chỉ chạy API (không có giao diện, duy nhất chạm tới CSDL) và **NestU.WebApp** là giao diện web người dùng mở trên trình duyệt. Web App lấy dữ liệu bằng cách gọi HTTP tới API, không tham chiếu code của API.

**NestU.API** — 1 project, chia tầng bằng thư mục, chạy ở `https://localhost:7001` (có Swagger):

```
NestU.API/
├── Controllers/          nhận request, chia theo role (mục 1.3, 1.8)
│   ├── AuthController.cs
│   ├── Public/  Tenant/  Landlord/  Admin/
│   └── OData/RoomsODataController.cs      (mục 1.9)
├── Services/             xử lý nghiệp vụ, kiểm tra quyền sở hữu (mục 1.5)
├── Data/                 làm việc với CSDL (mục 1.4)
│   ├── NestUDbContext.cs
│   ├── Configurations/   Fluent API: CHECK, unique index (mục 1.5)
│   └── Seed/             tài khoản và dữ liệu mẫu
├── Migrations/           EF Core tự sinh
├── Entities/             class ánh xạ bảng + Enums/
├── DTOs/                 dữ liệu vào/ra theo module, có DataAnnotations (mục 1.6, 1.7)
├── Helpers/              tạo JWT, tính khoảng cách, map Entity ↔ DTO (mục 1.10)
├── appsettings.json      connection string, cấu hình JWT
└── Program.cs            đăng ký DbContext, Identity, JWT, OData, Swagger
```

**NestU.WebApp** — 1 project MVC, chạy ở `https://localhost:7002`:

```
NestU.WebApp/
├── Controllers/          Home, Account, Rooms (trang công khai)
├── Areas/                Tenant/  Landlord/  Admin/  (Controllers + Views theo role)
├── Views/Shared/         _Layout, _RoomCard, _FloorRow, _AssetTable... (mục 2.5)
├── Models/               model tương ứng DTO của API + DataAnnotations riêng (mục 2.8)
├── ApiClients/           RoomApiClient, ContractApiClient... gọi HTTP tới API (mục 2.6)
├── Handlers/             JwtHandler gắn token vào mỗi request
├── wwwroot/              CSS, JS, ảnh, logo
├── appsettings.json      ApiSettings:BaseUrl = https://localhost:7001/
└── Program.cs            cookie auth, đăng ký typed HttpClient
```

**Ba quy tắc cả nhóm giữ:**

1. Controller chỉ gọi Service; chỉ Service mới dùng `DbContext`. Controller không bao giờ trả Entity, chỉ trả DTO.
2. Mỗi DTO bên API có một model cùng tên trong `Models/` bên Web App; ai sửa DTO thì sửa luôn model.
3. Cổng cố định trong `launchSettings.json`. Khi chạy: mở NestU.API trước, rồi NestU.WebApp, chỉ dùng Web App trên trình duyệt.

**Xác thực.** API dùng ASP.NET Core Identity lưu user và role, cấp JWT 60 phút chứa userId và role. Web App gọi `POST /api/auth/login`, tạo cookie authentication với claim role (để `[Authorize(Roles)]` và TagHelper ẩn hiện menu hoạt động), và giữ JWT trong ticket của cookie, không để trong localStorage. Cookie đặt `ExpireTimeSpan` bằng thời hạn JWT; nếu làm refresh token thì `DelegatingHandler` gọi `POST /api/auth/refresh` khi gặp 401 rồi gửi lại request.

**Gọi API.** Mỗi module có một typed client (`HouseApiClient`, `ContractApiClient`…) dùng chung `DelegatingHandler` gắn `Authorization: Bearer`. API trả 401 thì Web App đăng xuất và về trang Đăng nhập; 403 thì về trang "Không có quyền"; lỗi validation `ProblemDetails` được đổ vào `ModelState` để hiện bằng `asp-validation-for`.

## Vòng đời trạng thái

Trạng thái chỉ đổi qua API, không sửa trực tiếp từ client; mỗi lần đổi gửi thông báo tới người liên quan (khi làm SignalR).

| Đối tượng | Trạng thái | Ai chuyển và khi nào |
| --- | --- | --- |
| LandlordProfile | Pending, Approved, Rejected | Admin duyệt hoặc từ chối hồ sơ chủ trọ |
| BoardingHouse | Pending, Active, Rejected | Admin duyệt giấy PCCC → Active; từ chối → Rejected, Landlord upload giấy mới → Pending |
| FireSafetyCertificate | Pending, Approved, Rejected | Admin duyệt hoặc từ chối |
| Room | Available, Occupied, Maintenance | Duyệt thuê → Occupied; hợp đồng kết thúc → Available; Landlord đặt Maintenance khi phòng không có hợp đồng Active |
| RoomAsset.Condition | Good, Damaged, Broken | Landlord cập nhật; tự cập nhật từ biên bản trả phòng hoặc khi sự cố gắn tài sản được Resolved |
| ViewingAppointment | Pending, Confirmed, Rejected, Cancelled | Landlord xác nhận hoặc từ chối; Tenant hủy khi còn Pending |
| RentalRequest | Pending, Approved, Rejected, Cancelled | Landlord duyệt (sinh hợp đồng) hoặc từ chối; Tenant hủy khi còn Pending |
| Contract | Active, Terminated (hết hạn tính khi đọc) | Landlord chấm dứt hoặc gia hạn (đổi EndDate); quá EndDate thì hiển thị "Đã hết hạn" |
| ContractAsset (CheckIn) | Chưa xác nhận, Đã xác nhận | Tenant xác nhận biên bản nhận phòng; sau đó bị khóa |
| ContractChangeRequest | Pending, Approved, Rejected | Tenant gửi xin gia hạn hoặc trả phòng; Landlord xử lý |
| Invoice | Unpaid, PendingConfirm, Paid (Overdue tính khi đọc) | Tenant báo đã trả → PendingConfirm; Landlord xác nhận → Paid, từ chối → Unpaid; Unpaid quá DueDate hiển thị Overdue |
| MaintenanceRequest | Open, InProgress, Resolved | Tenant gửi; Landlord cập nhật tiến độ |
| Report | Open, Resolved, Dismissed | Admin xử lý |

## Luồng xử lý chi tiết

### 1. Đăng nhập và gọi API từ Web App

1. Người dùng nhập form `Account/Login`; Web App gọi `POST /api/auth/login` bằng typed HttpClient.
2. API kiểm tra Identity, trả `TokenDto`: JWT chứa userId, role, hết hạn 60 phút.
3. Web App tạo cookie đăng nhập có claim role, giữ JWT trong ticket; cookie hết hạn cùng JWT.
4. Mỗi request sau đó, `DelegatingHandler` gắn `Authorization: Bearer`.
5. API trả 401 → đăng xuất, về trang Đăng nhập; 403 → trang "Không có quyền".

### 2. Tạo phòng hàng loạt theo tầng (`POST /api/landlord/houses/{id}/rooms/bulk`)

Body: `floor`, `fromNumber`, `toNumber`, `area`, `price`, `depositAmount`, `capacity`, `amenityIds`, `assets[]` (loại tài sản, số lượng).

1. Kiểm tra quyền sở hữu nhà trọ, trả 403 nếu không phải của Landlord này.
2. Kiểm tra `1 ≤ floor ≤ TotalFloors`, `fromNumber ≤ toNumber`, tối đa 30 phòng.
3. Sinh số phòng theo quy ước `floor × 100 + n` (ví dụ tầng 3, n từ 1 đến 6 → 301–306); nếu số nào đã tồn tại, trả 409 kèm danh sách số trùng, không tạo phòng nào.
4. Trong một transaction: tạo các Room (Available), RoomAmenity, RoomAsset theo mẫu.
5. Trả 201 với danh sách phòng đã tạo. Web App có bước xem trước danh sách số phòng trước khi gửi.

### 3. Duyệt yêu cầu thuê (`PUT /api/landlord/rental-requests/{id}/approve`)

1. Kiểm tra quyền sở hữu: phòng thuộc nhà trọ của Landlord này, nếu không trả 403.
2. Kiểm tra nghiệp vụ: nhà trọ Active (đã duyệt PCCC); phòng Available; số người ≤ sức chứa; Tenant chưa có hợp đồng Active; nhà trọ StudentOnly thì Tenant phải là sinh viên.
3. Trong một transaction: tạo Contract (ghi `DepositAmount`, `DepositPaidAt`) và ContractMember; sao chép RoomAsset thành ContractAsset (CheckIn); Room → Occupied; RentalRequest → Approved; các RentalRequest Pending khác của phòng → Rejected.
4. Thông báo cho Tenant được duyệt và các Tenant bị từ chối.
5. Hai thao tác duyệt cùng lúc: RowVersion của Room gây xung đột, API trả 409.

### 4. Lập hóa đơn hằng tháng (`POST /api/landlord/invoices/generate`)

Body: `contractId`, `month`, `year`.

1. Kiểm tra quyền sở hữu hợp đồng.
2. Có UtilityReading tháng đó, chỉ số mới ≥ chỉ số cũ; trùng `(ContractId, Month, Year)` thì 409.
3. Tính: tiền phòng + (điện mới − điện cũ) × giá điện + tiền nước (theo m³ hoặc theo người) + các dịch vụ đăng ký; mỗi khoản một InvoiceDetail với UnitPrice tại thời điểm lập.
4. Tạo Invoice (Unpaid, DueDate = ngày 10 tháng sau) và thông báo Tenant.

### 5. Thanh toán hóa đơn

1. Tenant mở hóa đơn; (Nên có) `GET /api/tenant/invoices/{id}/qr` trả chuỗi VietQR với tài khoản nhận của nhà trọ, đúng số tiền, nội dung `NESTU-{invoiceId}`.
2. Tenant chuyển khoản, gửi `POST /api/tenant/invoices/{id}/payments` kèm ảnh → Invoice PendingConfirm.
3. Landlord đối chiếu: `confirm-payment` → tạo Payment, Invoice Paid; `reject-payment` kèm lý do → Invoice Unpaid.

### 6. Trả phòng và quyết toán cọc

1. Tenant gửi yêu cầu trả phòng (ContractChangeRequest, Type = Terminate), Landlord chấp nhận; hoặc Landlord chủ động chấm dứt.
2. Landlord kiểm tra phòng: `PUT /api/landlord/contracts/{id}/checkout-assets` ghi tình trạng CheckOut và tiền khấu trừ từng tài sản.
3. Landlord lập hóa đơn tháng cuối nếu còn.
4. `POST /api/landlord/contracts/{id}/deposit-settlement`: hệ thống tính `DamageDeduction` từ biên bản, `UnpaidDeduction` từ hóa đơn chưa Paid, `RefundAmount = DepositAmount − hai khoản trên` (không âm).
5. Trong một transaction: Contract → Terminated, Room → Available, cập nhật `RoomAsset.Condition` theo CheckOut; thông báo Tenant.

## Endpoint API theo role

Route nhóm theo role, mỗi nhóm gắn một `[Authorize(Roles = ...)]` (mục 1.8, 1.10); Service kiểm tra tiếp quyền sở hữu trên từng bản ghi. Dữ liệu JSON camelCase; phân trang `?page=&pageSize=` hoặc `$top/$skip`; lỗi trả `ProblemDetails`.

| Mã | Khi nào |
| --- | --- |
| 200, 201 (kèm Location), 204 | Thành công |
| 400 | Sai validation hoặc vi phạm nghiệp vụ (giá điện > 4.000, tầng vượt TotalFloors, chỉ số giảm, phòng không Available) |
| 401 | Chưa đăng nhập hoặc token hết hạn |
| 403 | Sai role, không sở hữu dữ liệu, hoặc Landlord chưa được duyệt |
| 404 | Không tìm thấy |
| 409 | Xung đột: số phòng trùng, phòng đã bị thuê, hóa đơn tháng đã tồn tại, biên bản đã xác nhận |

**OData** (`/odata/Rooms`, mục 1.9): `$filter` theo khu vực, giá, diện tích, tầng, tiện ích; `$orderby` theo giá hoặc ngày đăng; `$top/$skip`; `$select`. Kết quả là `RoomListDto` phẳng, có sẵn `houseName`, `address`, `floor`, `availabilityLabel`, `viewCount`, nên không cần `$expand`.

### Công khai — `/api/auth`, `/api/public`, `/odata`

| Method | Route | Chức năng | DTO |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Đăng ký Tenant (sinh viên / khách thường) hoặc Landlord (chờ duyệt) | `RegisterDto` → 201 |
| POST | `/api/auth/login` | Đăng nhập | `LoginDto` → `TokenDto` |
| GET | `/api/auth/me` | Người dùng hiện tại | → `UserDto` |
| GET | `/odata/Rooms` | Tìm, lọc, sắp xếp, phân trang phòng; chỉ nhà trọ Active | → `RoomListDto` |
| GET | `/api/rooms/{id}` | Chi tiết phòng, tầng, ảnh, tiện ích, tài sản; tăng ViewCount (khóa `roomId + IP/userId` 30 phút trong `IMemoryCache`) | → `RoomDetailDto` |
| GET | `/api/houses/{id}` | Chi tiết nhà trọ Active, phòng nhóm theo tầng, badge đã duyệt PCCC | → `HouseDetailDto` |
| GET | `/api/public/stats` | Số liệu trang Home | → `HomeStatsDto` |
| GET | `/api/public/lookups` | Khu vực, tiện ích, trường (kèm tọa độ) | → `LookupsDto` |
| GET | `/api/rooms/nearby?universityId=&radiusKm=` | (Nên có) Phòng trong bán kính, gần nhất trước, Haversine làm tròn 0,1 km | → `RoomListDto` có `distanceKm` |
| GET | `/api/houses/map` | (Nên có) Marker bản đồ: tên, tọa độ, giá thấp nhất, số phòng còn | → `MapMarkerDto[]` |

### Tenant — `/api/tenant` (`Roles = "Tenant"`, chỉ dữ liệu của mình)

| Method | Route | Chức năng | DTO |
| --- | --- | --- | --- |
| GET, PUT | `/profile` | Xem, cập nhật hồ sơ | `TenantProfileDto` |
| GET, POST | `/appointments` | Lịch xem của mình; đặt lịch | `CreateAppointmentDto` → 201 |
| PUT | `/appointments/{id}/cancel` | Hủy khi Pending | 204 |
| GET, POST | `/rental-requests` | Theo dõi; gửi yêu cầu thuê (400 nếu phòng không Available hoặc nhà trọ chưa đủ PCCC) | `CreateRentalRequestDto` → 201 |
| PUT | `/rental-requests/{id}/cancel` | Hủy khi Pending | 204 |
| GET | `/contracts`, `/contracts/{id}` | Hợp đồng hiện tại, lịch sử, tiền cọc, quyết toán cọc | → `ContractDto` |
| GET | `/contracts/{id}/assets?stage=` | Biên bản bàn giao CheckIn / CheckOut | → `ContractAssetDto[]` |
| PUT | `/contracts/{id}/assets/confirm` | Xác nhận biên bản nhận phòng | 204 |
| POST | `/contracts/{id}/renewal-requests`, `/termination-requests` | Xin gia hạn, trả phòng | `CreateContractChangeDto` → 201 |
| GET | `/invoices`, `/invoices/{id}` | Hóa đơn và chi tiết | → `InvoiceDto` |
| POST | `/invoices/{id}/payments` | Báo đã thanh toán kèm ảnh (multipart) | `ReportPaymentDto` → 204 |
| GET | `/invoices/{id}/qr` | (Nên có) Chuỗi VietQR | → `QrDto` |
| GET, POST | `/maintenance-requests` | Theo dõi; báo sự cố (có thể gắn `roomAssetId`) | `CreateMaintenanceDto` → 201 |
| POST | `/reviews` | Đánh giá phòng đã thuê; 403 nếu chưa từng thuê | `CreateReviewDto` → 201 |
| POST | `/reports` | Khiếu nại chủ trọ, tin đăng, giá điện vượt trần | `CreateReportDto` → 201 |

### Landlord — `/api/landlord` (`Roles = "Landlord"` + policy `ApprovedLandlord` + kiểm tra sở hữu)

| Method | Route | Chức năng | DTO |
| --- | --- | --- | --- |
| GET, PUT | `/profile` | Hồ sơ chủ trọ, trạng thái duyệt (không cần policy duyệt) | `LandlordProfileDto` |
| GET, POST | `/houses` | Danh sách; tạo nhà trọ (Pending) với `totalFloors`, vị trí, giá điện, giá nước | `CreateHouseDto` → 201 |
| GET, PUT, DELETE | `/houses/{id}` | Xem, sửa, xóa (xóa khi không còn hợp đồng Active) | `UpdateHouseDto` |
| GET, POST | `/houses/{id}/certificates` | Giấy PCCC; upload (multipart) | `CreateCertificateDto` → 201 |
| GET | `/houses/{id}/floors` | Tóm tắt theo tầng: số phòng, số còn trống, số đang sửa | → `FloorSummaryDto[]` |
| GET, POST | `/rooms?houseId=&floor=` | Danh sách phòng (lọc theo tầng); tạo một phòng | `CreateRoomDto` → 201 |
| POST | `/houses/{id}/rooms/bulk` | Tạo hàng loạt phòng cho một tầng (luồng 2) | `BulkCreateRoomsDto` → 201 |
| PUT, DELETE | `/rooms/{id}` | Sửa (kể cả tầng), xóa phòng không có hợp đồng | `UpdateRoomDto` |
| PUT | `/rooms/{id}/status` | Đặt Available hoặc Maintenance | `SetRoomStatusDto` → 204 |
| GET, POST | `/rooms/{id}/assets` | Tài sản của phòng; thêm tài sản | `RoomAssetDto` → 201 |
| PUT, DELETE | `/rooms/{id}/assets/{assetId}` | Sửa số lượng, tình trạng; xóa | `RoomAssetDto` → 204 |
| POST | `/rooms/{id}/assets/copy` | Sao chép tài sản sang các phòng khác (`targetRoomIds`) | `CopyAssetsDto` → 204 |
| GET, POST | `/services` ; PUT, DELETE `/services/{id}` | Dịch vụ và đơn giá | `ServiceDto` |
| GET | `/appointments` ; PUT `/appointments/{id}/confirm`, `/reject` | Lịch xem phòng | `RejectDto` → 204 |
| GET | `/rental-requests` | Yêu cầu thuê gửi tới mình | → `RentalRequestDto[]` |
| PUT | `/rental-requests/{id}/approve` | Duyệt, sinh hợp đồng và biên bản (luồng 3); 409 nếu phòng đã bị thuê | `ApproveRentalDto` → `ContractDto` |
| PUT | `/rental-requests/{id}/reject` | Từ chối kèm lý do | `RejectDto` → 204 |
| GET | `/contracts` | Hợp đồng thuộc nhà trọ của mình | → `ContractDto[]` |
| PUT | `/contracts/{id}/extend` | Gia hạn | `ExtendContractDto` → 204 |
| GET, PUT | `/contract-requests`, `/contract-requests/{id}/decision` | Xử lý yêu cầu gia hạn, trả phòng | `DecisionDto` |
| PUT | `/contracts/{id}/checkout-assets` | Ghi tình trạng tài sản khi trả phòng | `CheckoutAssetsDto` → 204 |
| POST | `/contracts/{id}/deposit-settlement` | Quyết toán cọc, chấm dứt hợp đồng (luồng 6) | → `DepositSettlementDto` |
| GET, POST | `/readings` | Xem; ghi chỉ số (400 nếu nhỏ hơn tháng trước) | `UtilityReadingDto` → 201 |
| GET | `/invoices?status=` | Danh sách hóa đơn | → `InvoiceDto[]` |
| POST | `/invoices/generate` | Lập hóa đơn tháng (luồng 4); 409 nếu trùng | `GenerateInvoiceDto` → 201 |
| PUT | `/invoices/{id}/confirm-payment`, `/reject-payment` | Xác nhận hoặc từ chối thanh toán | `RejectDto` → 204 |
| GET | `/maintenance-requests` ; PUT `/{id}/status` | Sự cố; cập nhật tiến độ | `UpdateMaintenanceStatusDto` → 204 |
| GET | `/stats/revenue?year=`, `/stats/occupancy`, `/stats/unpaid-invoices` | (Nên có) Doanh thu, lấp đầy theo tầng, nợ | → `StatsDto` |

### Admin — `/api/admin` (`Roles = "Admin"`)

| Method | Route | Chức năng | DTO |
| --- | --- | --- | --- |
| GET | `/users?role=&status=` | Danh sách tài khoản (không hiện SuperAdmin) | → `UserListDto` |
| PUT | `/users/{id}/lock`, `/unlock` | Khóa, mở tài khoản | 204 |
| PUT | `/users/{id}/role` | Gán Tenant hoặc Landlord; không gán được Admin | `SetRoleDto` → 204 |
| GET | `/landlord-approvals?status=Pending` | Hồ sơ chủ trọ chờ duyệt | → `LandlordApprovalDto[]` |
| PUT | `/landlord-approvals/{id}/decision` | Duyệt hoặc từ chối chủ trọ | `DecisionDto` → 204 |
| GET | `/certificates?status=Pending`, `/certificates/{id}` | Giấy PCCC chờ duyệt | → `CertificateDto` |
| PUT | `/certificates/{id}/review` | Duyệt (nhà trọ → Active) hoặc từ chối | `ReviewCertificateDto` → 204 |
| GET ; PUT | `/reports` ; `/reports/{id}/resolve` | Khiếu nại; ẩn tin, khóa tài khoản hoặc bác bỏ | `ResolveReportDto` → 204 |
| CRUD | `/catalog/areas`, `/amenities`, `/asset-types`, `/universities` | Danh mục chung | `LookupItemDto` |
| GET | `/stats` | (Nên có) Dashboard toàn hệ thống | → `AdminStatsDto` |

### Dùng chung và Mở rộng

| Method | Route | Quyền | Chức năng |
| --- | --- | --- | --- |
| GET, PUT | `/api/notifications`, `/unread-count`, `/{id}/read`, `/read-all` | Đã đăng nhập (Nên có) | Danh sách, badge, đánh dấu đã đọc |
| WS | `/hubs/notifications` | Đã đăng nhập (Nên có) | SignalR, sự kiện `ReceiveNotification` |
| POST, GET | `/api/super/admins/{userId}/grant`, `/revoke`, `/unlock` ; `/api/super/audit-logs` | SuperAdmin (Mở rộng) | Cấp, thu hồi Admin (409 nếu Admin cuối cùng), nhật ký |

## Thông báo realtime (Nên có)

Service xử lý xong gọi `INotificationService.SendAsync(userId, type, title, message, linkUrl)`: lưu `Notification` (IsRead = false) rồi đẩy qua `IHubContext<NotificationHub>.Clients.User(userId)`. Thông báo luôn được lưu, nên mất kết nối không mất thông báo; nếu chưa làm SignalR, chỉ cần phần lưu và trang danh sách.

**Ba điểm dễ lỗi:**

- JWT với WebSocket: token gửi qua query `access_token`; `JwtBearerEvents.OnMessageReceived` chỉ đọc khi đường dẫn bắt đầu bằng `/hubs`.
- Token cho JS: Web App có `GET /Account/HubToken` (chỉ khi đã đăng nhập) trả JWT cho thư viện `@microsoft/signalr`.
- CORS: API dùng `WithOrigins(<địa chỉ Web App>)` và `AllowCredentials()`; hub gắn `[Authorize]`, userId lấy từ claim NameIdentifier.

| Sự kiện | Người nhận | Type |
| --- | --- | --- |
| Chủ trọ mới đăng ký chờ duyệt | Admin | LandlordPending |
| Hồ sơ chủ trọ được duyệt hoặc từ chối | Landlord | LandlordReviewed |
| Giấy PCCC được duyệt hoặc từ chối | Landlord | CertificateReviewed |
| Lịch xem mới; lịch xem được xác nhận hoặc từ chối | Landlord; Tenant | AppointmentCreated, AppointmentDecided |
| Yêu cầu thuê mới; được duyệt hoặc từ chối | Landlord; Tenant | RentalRequestCreated, RentalRequestDecided |
| Biên bản nhận phòng chờ xác nhận | Tenant | AssetHandoverPending |
| Hóa đơn mới; thanh toán được xác nhận hoặc từ chối | Tenant | InvoiceCreated, PaymentDecided |
| Người thuê báo đã thanh toán | Landlord | PaymentReported |
| Sự cố mới; tiến độ sửa chữa | Landlord; Tenant | MaintenanceUpdated |
| Hợp đồng sắp hết hạn (cảnh báo trên trang Hợp đồng, không đẩy realtime) | Landlord, Tenant | ContractExpiring |
| Quyết toán cọc hoàn tất | Tenant | DepositSettled |
| Khiếu nại có kết quả | Tenant | ReportResolved |

## Giao diện Web App

Web App dùng MVC với Area theo role (`Areas/Tenant`, `Areas/Landlord`, `Areas/Admin`); menu hiện theo claim role. Mọi form dùng TagHelper (`asp-for`, `asp-validation-for`) trên model trong `Models/` của NestU.WebApp.

| Role | Trang chính |
| --- | --- |
| Guest | Home, Tìm phòng (lưới / bản đồ), Chi tiết phòng, Chi tiết nhà trọ (phòng nhóm theo tầng), Đăng nhập, Đăng ký |
| Tenant | Hồ sơ, Lịch xem, Yêu cầu thuê, Hợp đồng (tab Biên bản tài sản, tab Tiền cọc), Hóa đơn, Sự cố, Đánh giá, Khiếu nại |
| Landlord | Hồ sơ / chờ duyệt, Nhà trọ, Giấy PCCC, **Sơ đồ tầng** (mỗi tầng một hàng, mỗi phòng một ô màu theo trạng thái), Tạo phòng hàng loạt (có bước xem trước), Phòng (tab Tài sản), Lịch xem, Yêu cầu thuê, Hợp đồng (Bàn giao, Trả phòng, Quyết toán cọc), Dịch vụ, Chỉ số điện nước, Hóa đơn, Sự cố, Thống kê |
| Admin | Tài khoản, Duyệt chủ trọ, Duyệt PCCC, Khiếu nại, Danh mục (khu vực, tiện ích, loại tài sản, trường), Dashboard |

PartialView dùng lại: `_RoomCard`, `_FloorRow`, `_AssetTable`, `_InvoiceDetailTable`, `_StatusBadge`, `_Pagination`, `_NotificationBell`.

### Trang Home

Màn hình đầu là hero toàn trang trên nền canvas; cuộn xuống là thanh tìm kiếm và 6 phòng nổi bật. Prompt chi tiết trong `nestu-home-prompt.md`. Cấu trúc: `_LandingLayout`, `Views/Home/Index.cshtml`, các PartialView `_LandingHeader`, `_MobileMenu`, `_TrustRow`, `_StatsBar`, `_SearchBar`, `_RoomCard`.

| Thành phần | Nội dung |
| --- | --- |
| Nền | Canvas tự vẽ: mưa ký tự, trời hồng tím, đồi cát, hạt nhiễu (không dùng video ngoài) |
| Header | Logo trong vòng tròn trắng; nav: Trang chủ, Tìm phòng, Nhà trọ, Liên hệ; nút Đăng nhập |
| Hàng tin cậy | Icon nhà, bình chữa cháy, tia sét + "100% nhà trọ đạt chuẩn PCCC" |
| Tiêu đề | "Tìm Trọ An Toàn / Sống Trọn Tuổi Trẻ", kiểu dot-matrix bằng CSS |
| Mô tả | Kết nối sinh viên và người đi làm với nhà trọ đã kiểm duyệt PCCC, giá điện minh bạch không quá 4.000đ/kWh, hợp đồng và hóa đơn quản lý trực tuyến |
| Nút chính | "Tìm phòng ngay" → trang Tìm phòng |
| Số liệu | Phòng đang trống, Nhà trọ đạt PCCC, Giá điện tối đa / kWh, Người đang thuê (từ `/api/public/stats`, đếm lên khi hiện) |
| Dưới màn hình đầu | Thanh tìm kiếm (khu vực, trường, giá, tiện ích) + 6 `_RoomCard` có badge "Đạt PCCC" |
| Mobile | Menu burger, tấm menu trắng, số liệu 2×2 |

## Lộ trình và phân công

Hoàn thành trong 7 ngày, từ thứ Tư 28/10 đến thứ Ba 3/11. Chỉ làm tầng **Bắt buộc**; tầng Nên có (SignalR, bản đồ, thống kê, VietQR) chỉ đụng tới nếu ngày 6 xong sớm; tầng Mở rộng bỏ hẳn. Mỗi người làm trọn API + trang Web App của mảng mình theo bảng phân công bên dưới.

**Nhịp làm việc mỗi ngày:** họp 15 phút buổi sáng (hôm qua xong gì, hôm nay làm gì, đang kẹt gì); mỗi mảng một nhánh Git, cuối ngày merge vào `main` và chạy thử được thì mới nghỉ. Ai kẹt quá 1 giờ thì hỏi nhóm ngay.

| Ngày | Mục tiêu | Xong khi |
| --- | --- | --- |
| 1 · T4 28/10 | Nền tảng chung | Cả nhóm clone về, chạy được API (Swagger) và Web App, đăng nhập được bằng tài khoản seed |
| 2 · T5 29/10 | API: CRUD từng mảng | Endpoint CRUD của mỗi mảng chạy đúng trên Swagger |
| 3 · T6 30/10 | API: luồng nghiệp vụ + khóa API | Mọi endpoint tầng Bắt buộc chạy đúng; từ đây hạn chế đổi DTO |
| 4 · T7 31/10 | Web App: trang chính từng mảng | Mỗi mảng có trang danh sách + form tạo/sửa gọi được API |
| 5 · CN 1/11 | Web App: hoàn thiện | Đủ trang theo mục Giao diện Web App, có validation, responsive |
| 6 · T2 2/11 | Ghép nối, sửa lỗi, dữ liệu demo | Chạy trọn kịch bản demo không lỗi |
| 7 · T3 3/11 | Báo cáo + tập demo | Nộp được mã nguồn, báo cáo, ảnh ERD và ảnh giao diện |

### Ngày 1 · T4 28/10 — Nền tảng chung

- **Sáng, cả nhóm:** mỗi người viết Entity cho các bảng thuộc mảng mình (theo mục Thiết kế CSDL), gửi cho Mảng 1 trước 11h.
- **Mảng 1:** tạo hai project NestU.API và NestU.WebApp theo cấu trúc mục Kiến trúc; gộp Entity, viết `NestUDbContext` và `Configurations/` (CHECK, unique index), migration đầu tiên, seed 1 Admin, 2 Landlord, 3 Tenant, khu vực, tiện ích, trường; Identity + JWT + 3 role, Swagger có nút Authorize.
- **Mảng 1 (chiều):** Web App có `_Layout`, cookie auth, `JwtHandler`, base ApiClient, trang Đăng nhập, Đăng ký, "Không có quyền".
- **Mảng 2–5 (chiều):** viết DTO và interface Service cho mảng mình; dựng HTML tĩnh bằng Bootstrap cho các trang của mảng mình (chưa gọi API). Mảng 2 làm trang Home.

### Ngày 2 · T5 29/10 — API: CRUD từng mảng

- **Mảng 1:** quản lý tài khoản (khóa, mở, gán role), duyệt hồ sơ chủ trọ, policy `ApprovedLandlord`, danh mục (khu vực, tiện ích, loại tài sản, trường).
- **Mảng 2:** CRUD nhà trọ (có `TotalFloors`, giá điện, giá nước), upload giấy PCCC, CRUD phòng có tầng, tài sản phòng, `GET /houses/{id}/floors`.
- **Mảng 3:** lịch xem (đặt, hủy, xác nhận, từ chối), gửi và hủy yêu cầu thuê.
- **Mảng 4:** dịch vụ, ghi chỉ số điện nước (kiểm tra không giảm), danh sách hóa đơn.
- **Mảng 5:** sửa chữa (gắn tài sản), đánh giá, khiếu nại phía Tenant.

### Ngày 3 · T6 30/10 — API: luồng nghiệp vụ

- **Mảng 1:** Admin duyệt giấy PCCC (nhà trọ → Active), xử lý khiếu nại.
- **Mảng 2:** tạo phòng hàng loạt, sao chép tài sản, OData `/odata/Rooms`, API công khai (chi tiết phòng có ViewCount, chi tiết nhà trọ, lookups, stats).
- **Mảng 3:** duyệt yêu cầu thuê trong transaction (sinh hợp đồng, biên bản CheckIn, RowVersion 409), gia hạn, trả phòng, biên bản CheckOut, quyết toán cọc.
- **Mảng 4:** lập hóa đơn tháng (409 khi trùng), báo thanh toán kèm ảnh, xác nhận / từ chối, Overdue tính khi đọc.
- **Mảng 5:** Landlord cập nhật tiến độ sửa chữa, chặn đánh giá khi chưa từng thuê; viết sẵn kịch bản demo (xem ngày 6).
- **Cuối ngày:** mỗi người tự test mảng mình trên Swagger theo cột "Khi demo chỉ vào" của bảng đối chiếu đề. Từ đây đổi DTO phải báo cả nhóm.

### Ngày 4 · T7 31/10 — Web App: trang chính

- Mỗi mảng: tạo `Models/` tương ứng DTO, ApiClient của mảng, nối HTML tĩnh hôm thứ Năm vào API.
- **Mảng 1:** trang Admin (tài khoản, duyệt chủ trọ, duyệt PCCC), menu ẩn hiện theo role.
- **Mảng 2:** trang Tìm phòng (lọc qua OData), Chi tiết phòng, Landlord: Nhà trọ, Sơ đồ tầng, Phòng.
- **Mảng 3:** Tenant: Lịch xem, Yêu cầu thuê, Hợp đồng; Landlord: duyệt yêu cầu.
- **Mảng 4:** Landlord: Dịch vụ, Chỉ số, Hóa đơn; Tenant: Hóa đơn, báo thanh toán.
- **Mảng 5:** Sự cố, Đánh giá, Khiếu nại cả hai phía.

### Ngày 5 · CN 1/11 — Web App: hoàn thiện

- Phần còn lại của từng mảng: tạo phòng hàng loạt có bước xem trước, tab Tài sản, Bàn giao, Trả phòng, Quyết toán cọc, Danh mục Admin, Khiếu nại Admin.
- Validation: DataAnnotations trong `Models/` + `asp-validation-for`; lỗi 400 từ API đổ vào `ModelState`.
- Tách PartialView dùng chung (`_RoomCard`, `_FloorRow`, `_AssetTable`, `_StatusBadge`, `_Pagination`); kiểm tra responsive trên điện thoại.

### Ngày 6 · T2 2/11 — Ghép nối và sửa lỗi

- **Sáng, cả nhóm chạy kịch bản demo đầu cuối:** Landlord đăng ký → Admin duyệt → tạo nhà trọ, upload PCCC → Admin duyệt → tạo tầng 1–3 bằng tạo hàng loạt, thêm tài sản → Tenant tìm phòng, đặt lịch, gửi yêu cầu thuê → Landlord duyệt, Tenant xác nhận biên bản → ghi chỉ số, lập hóa đơn → Tenant báo thanh toán → Landlord xác nhận → báo sự cố → trả phòng, quyết toán cọc → đánh giá.
- Kiểm tra các lỗi cố ý: giá điện 5.000, số phòng trùng, chỉ số giảm, token Tenant gọi API Landlord, hóa đơn trùng tháng.
- **Chiều:** sửa lỗi; seed dữ liệu demo đẹp (ảnh phòng thật, tên tiếng Việt, vài hóa đơn cũ). Còn thời gian mới làm 1 mục Nên có, ưu tiên thống kê Landlord.

### Ngày 7 · T3 3/11 — Báo cáo và demo

- Chụp Database Diagram từ SQL Server (tách 3–4 hình theo mảng) và ảnh từng trang theo role.
- Hoàn thiện tài liệu này thành báo cáo: điền tên vào bảng phân công, chèn ERD và ảnh giao diện.
- Tập demo 1–2 lần theo kịch bản ngày 6, mỗi người trình bày mảng của mình; đóng gói mã nguồn (bỏ thư mục `bin/`, `obj/`).

**Rủi ro và cách xử lý:** nếu cuối ngày 3 API của một mảng chưa xong, mảng đó cắt bớt chức năng phụ (ví dụ sao chép tài sản, quyết toán cọc chỉ còn hoàn đủ cọc) thay vì kéo lùi cả nhóm. Không bắt đầu tầng Nên có trước khi kịch bản demo ngày 6 chạy trọn.

| Mảng | Phụ trách | Phạm vi |
| --- | --- | --- |
| 1. Nền tảng + Auth + Admin |  | Việc nền tảng tuần 1; duyệt chủ trọ, duyệt PCCC, quản lý tài khoản, danh mục |
| 2. Nhà trọ + tầng + phòng + tài sản |  | CRUD nhà trọ, phòng, tạo hàng loạt, sơ đồ tầng, RoomAsset; OData; trang Home, Tìm phòng |
| 3. Thuê + hợp đồng + bàn giao |  | Lịch xem, yêu cầu thuê, hợp đồng, ContractAsset, gia hạn, trả phòng, quyết toán cọc |
| 4. Điện nước + hóa đơn |  | Dịch vụ, chỉ số, lập hóa đơn, thanh toán, VietQR |
| 5. Sửa chữa + đánh giá + thống kê |  | Maintenance (gắn tài sản), Review, Report, thống kê, SignalR |

## Đối chiếu yêu cầu đề PRN232

Mọi mục của đề được đáp ứng ngay trong tầng Bắt buộc. Cột cuối là thứ cần chỉ ra khi demo hoặc chụp ảnh cho báo cáo.

| Mục | Yêu cầu | Làm ở đâu | Khi demo chỉ vào |
| --- | --- | --- | --- |
| 1.1 | Tác nhân sử dụng API | Mục Tác nhân hệ thống: Admin, Landlord, Tenant, Guest | Bảng role trong báo cáo |
| 1.2 | Chức năng của endpoint | Mục Endpoint API theo role | Swagger liệt kê endpoint nhóm theo role |
| 1.3 | ASP.NET Web API | NestU.API, thư mục `Controllers/` | Chạy API, mở Swagger |
| 1.4 | SQL Server + EF Core | `Data/NestUDbContext.cs`, `Migrations/`, `Data/Seed/` | Chạy `Update-Database`, mở SQL Server thấy bảng và dữ liệu mẫu |
| 1.5 | Ràng buộc trong code và CSDL | `Data/Configurations/` (CHECK, unique index), `Services/` (kiểm tra nghiệp vụ) | Đặt giá điện 5.000 → 400; tạo trùng số phòng → 409; xem CHECK trong SQL Server |
| 1.6 | DTO cho CRUD | `DTOs/` theo module: `Create…Dto`, `Update…Dto`, `…ListDto` | Controller chỉ nhận và trả DTO |
| 1.7 | Validation | DataAnnotations trong `DTOs/`: `[Required]`, `[Range(1, 4000)]`, `[RegularExpression]` | Gửi CCCD sai định dạng → 400 kèm lỗi từng trường |
| 1.8 | Routing | `[Route("api/landlord/rooms")]`… trên controller theo role | Route nhóm theo role trong Swagger |
| 1.9 | OData | `Controllers/OData/RoomsODataController.cs`, EDM trong `Program.cs` | `/odata/Rooms?$filter=Price le 3000000&$orderby=Price&$top=10` |
| 1.10 | JWT + Role-Based | `Helpers/` tạo JWT; `[Authorize(Roles = "Landlord")]`; Service kiểm tra sở hữu | Gọi API Landlord bằng token Tenant → 403 |
| 2.1 | Tác nhân hệ thống | 4 role, menu khác nhau theo role | Đăng nhập lần lượt 4 tài khoản demo |
| 2.2 | Chức năng của hệ thống | Mục Chức năng nghiệp vụ, mục Giao diện Web App | Ảnh chụp từng trang |
| 2.3 | MVC hoặc Razor Pages | NestU.WebApp: `Controllers/`, `Areas/`, `Views/` | Cấu trúc project |
| 2.4 | HTML5, CSS3, Bootstrap 5, responsive | `Views/`, `wwwroot/css` | Thu nhỏ trình duyệt hoặc mở trên điện thoại |
| 2.5 | TagHelper, PartialView | `asp-for`, `asp-action`; `Views/Shared/_RoomCard.cshtml`, `_FloorRow`… | Code một View có dùng cả hai |
| 2.6 | Dùng Web API | `ApiClients/` gọi HTTP; Web App không có DbContext hay connection string | `appsettings.json` của Web App chỉ có địa chỉ API |
| 2.7 | Identity phân quyền | Identity ở API; Web App dùng cookie mang claim role, `[Authorize(Roles)]` trên Areas (giảng viên đã đồng ý 26/10) | Tenant mở trang Landlord → bị chặn |
| 2.8 | Ràng buộc trong code | DataAnnotations trong `Models/` + `asp-validation-for` | Nhập sai form → báo lỗi ngay, không gửi lên API |
| 3 | Báo cáo | Tài liệu này + ERD chụp từ Database Diagram của SQL Server + ảnh giao diện + bảng phân công |  |

## Câu hỏi cần xác nhận với giảng viên

- [x] Mục 2.7: Identity đặt ở API và Web App dùng cookie authentication mang claim role từ JWT có được chấp nhận, hay Web App phải cài ASP.NET Core Identity riêng? Giảng viên đã đồng ý dùng cookie authentication từ JWT (26/10).
- [x] Báo cáo có cần ERD vẽ bằng công cụ riêng hay chụp diagram từ SQL Server là đủ? Giảng viên xác nhận chụp Database Diagram từ SQL Server là đủ (26/10).
