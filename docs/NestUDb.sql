-- =====================================================================
-- NestU — Script tạo CSDL SQL Server (tương đương model EF Core)
-- Dùng khi muốn tạo CSDL thủ công hoặc đưa vào báo cáo.
-- Bình thường không cần chạy: NestU.API tự tạo CSDL qua EF Core migration khi khởi động.
-- Script chính xác nhất do EF sinh: trong NestU.API chạy  dotnet ef migrations script -o docs/NestUDb.generated.sql
-- Quy ước: enum lưu dạng nvarchar(30); tiền/diện tích decimal(18,2); Id int identity.
-- =====================================================================

IF DB_ID(N'NestUDb') IS NULL CREATE DATABASE NestUDb;
GO
USE NestUDb;
GO
-- Cần cho filtered index khi chạy bằng sqlcmd (SSMS đã bật sẵn)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ===================== ASP.NET Core Identity =====================
CREATE TABLE AspNetRoles (
    Id               nvarchar(450) NOT NULL PRIMARY KEY,
    Name             nvarchar(256) NULL,
    NormalizedName   nvarchar(256) NULL,
    ConcurrencyStamp nvarchar(max) NULL
);
CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;

CREATE TABLE AspNetUsers (
    Id                   nvarchar(450) NOT NULL PRIMARY KEY,
    FullName             nvarchar(100) NOT NULL,          -- thêm cho NestU
    IsLocked             bit           NOT NULL DEFAULT 0, -- thêm cho NestU
    CreatedAt            datetime2     NOT NULL,           -- thêm cho NestU
    UserName             nvarchar(256) NULL,
    NormalizedUserName   nvarchar(256) NULL,
    Email                nvarchar(256) NULL,
    NormalizedEmail      nvarchar(256) NULL,
    EmailConfirmed       bit NOT NULL,
    PasswordHash         nvarchar(max) NULL,
    SecurityStamp        nvarchar(max) NULL,
    ConcurrencyStamp     nvarchar(max) NULL,
    PhoneNumber          nvarchar(max) NULL,
    PhoneNumberConfirmed bit NOT NULL,
    TwoFactorEnabled     bit NOT NULL,
    LockoutEnd           datetimeoffset NULL,
    LockoutEnabled       bit NOT NULL,
    AccessFailedCount    int NOT NULL
);
CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON AspNetUsers(NormalizedEmail);

CREATE TABLE AspNetRoleClaims (
    Id int IDENTITY PRIMARY KEY, RoleId nvarchar(450) NOT NULL REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL);
CREATE TABLE AspNetUserClaims (
    Id int IDENTITY PRIMARY KEY, UserId nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL);
CREATE TABLE AspNetUserLogins (
    LoginProvider nvarchar(450) NOT NULL, ProviderKey nvarchar(450) NOT NULL, ProviderDisplayName nvarchar(max) NULL,
    UserId nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE, PRIMARY KEY (LoginProvider, ProviderKey));
CREATE TABLE AspNetUserRoles (
    UserId nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    RoleId nvarchar(450) NOT NULL REFERENCES AspNetRoles(Id) ON DELETE CASCADE, PRIMARY KEY (UserId, RoleId));
CREATE TABLE AspNetUserTokens (
    UserId nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    LoginProvider nvarchar(450) NOT NULL, Name nvarchar(450) NOT NULL, Value nvarchar(max) NULL, PRIMARY KEY (UserId, LoginProvider, Name));
GO

-- ===================== Danh mục =====================
CREATE TABLE Areas (
    Id   int IDENTITY PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    CONSTRAINT IX_Areas_Name UNIQUE (Name)
);

CREATE TABLE Universities (
    Id        int IDENTITY PRIMARY KEY,
    Name      nvarchar(200) NOT NULL,
    ShortName nvarchar(30)  NULL,
    Latitude  float NOT NULL,
    Longitude float NOT NULL,
    CONSTRAINT IX_Universities_Name UNIQUE (Name)
);

CREATE TABLE Amenities (
    Id   int IDENTITY PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Icon nvarchar(50)  NULL,
    CONSTRAINT IX_Amenities_Name UNIQUE (Name)
);

CREATE TABLE AssetTypes (
    Id   int IDENTITY PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    CONSTRAINT IX_AssetTypes_Name UNIQUE (Name)
);
GO

-- ===================== Hồ sơ =====================
CREATE TABLE TenantProfiles (
    Id           int IDENTITY PRIMARY KEY,
    UserId       nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FullName     nvarchar(100) NOT NULL,
    CitizenId    nvarchar(12)  NOT NULL,
    Phone        nvarchar(10)  NOT NULL,
    IsStudent    bit NOT NULL,
    StudentCode  nvarchar(20) NULL,
    UniversityId int NULL REFERENCES Universities(Id),
    CONSTRAINT IX_TenantProfiles_UserId UNIQUE (UserId),
    CONSTRAINT CK_TenantProfile_Phone     CHECK (LEN([Phone]) = 10),
    CONSTRAINT CK_TenantProfile_CitizenId CHECK (LEN([CitizenId]) = 12),
    CONSTRAINT CK_TenantProfile_Student   CHECK ([IsStudent] = 0 OR ([StudentCode] IS NOT NULL AND [UniversityId] IS NOT NULL))
);
CREATE UNIQUE NONCLUSTERED INDEX IX_TenantProfiles_StudentCode ON TenantProfiles(StudentCode) WHERE StudentCode IS NOT NULL;

CREATE TABLE LandlordProfiles (
    Id             int IDENTITY PRIMARY KEY,
    UserId         nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FullName       nvarchar(100) NOT NULL,
    CitizenId      nvarchar(12)  NOT NULL,
    Phone          nvarchar(10)  NOT NULL,
    ApprovalStatus nvarchar(30)  NOT NULL,   -- Pending | Approved | Rejected
    ReviewNote     nvarchar(500) NULL,
    CreatedAt      datetime2 NOT NULL,
    ReviewedAt     datetime2 NULL,
    CONSTRAINT IX_LandlordProfiles_UserId UNIQUE (UserId),
    CONSTRAINT CK_LandlordProfile_Phone     CHECK (LEN([Phone]) = 10),
    CONSTRAINT CK_LandlordProfile_CitizenId CHECK (LEN([CitizenId]) = 12)
);
GO

-- ===================== Nhà trọ, giấy PCCC, dịch vụ =====================
CREATE TABLE BoardingHouses (
    Id                int IDENTITY PRIMARY KEY,
    LandlordId        nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id),
    Name              nvarchar(200) NOT NULL,
    Address           nvarchar(300) NOT NULL,
    AreaId            int NOT NULL REFERENCES Areas(Id),
    Latitude          float NULL,
    Longitude         float NULL,
    TotalFloors       int NOT NULL,
    Status            nvarchar(30) NOT NULL,          -- Pending | Active | Rejected
    IsHidden          bit NOT NULL DEFAULT 0,         -- Admin ẩn tin
    ElectricityPrice  decimal(18,2) NOT NULL,         -- đ/kWh
    WaterPrice        decimal(18,2) NOT NULL,
    WaterBillingType  nvarchar(30) NOT NULL,          -- PerM3 | PerPerson
    AllowedTenantType nvarchar(30) NOT NULL,          -- All | StudentOnly
    Description       nvarchar(2000) NULL,
    BankCode          nvarchar(20)  NULL,
    BankAccount       nvarchar(30)  NULL,
    AccountName       nvarchar(100) NULL,
    CreatedAt         datetime2 NOT NULL,
    -- Quy định 2: giá điện tối đa 4.000đ/kWh
    CONSTRAINT CK_BoardingHouse_ElectricityPrice CHECK ([ElectricityPrice] > 0 AND [ElectricityPrice] <= 4000),
    CONSTRAINT CK_BoardingHouse_WaterPrice       CHECK ([WaterPrice] > 0),
    CONSTRAINT CK_BoardingHouse_TotalFloors      CHECK ([TotalFloors] >= 1 AND [TotalFloors] <= 50)
);
CREATE INDEX IX_BoardingHouses_Status ON BoardingHouses(Status);

CREATE TABLE FireSafetyCertificates (
    Id              int IDENTITY PRIMARY KEY,
    BoardingHouseId int NOT NULL REFERENCES BoardingHouses(Id) ON DELETE CASCADE,
    CertificateNo   nvarchar(50)  NOT NULL,
    FileUrl         nvarchar(500) NOT NULL,
    Status          nvarchar(30)  NOT NULL,           -- Pending | Approved | Rejected
    ReviewNote      nvarchar(500) NULL,
    UploadedAt      datetime2 NOT NULL,
    ReviewedAt      datetime2 NULL
);
-- Quy định 1: mỗi nhà trọ tối đa một giấy PCCC Approved
CREATE UNIQUE INDEX UX_Certificate_OneApprovedPerHouse ON FireSafetyCertificates(BoardingHouseId) WHERE [Status] = 'Approved';

CREATE TABLE Services (
    Id              int IDENTITY PRIMARY KEY,
    BoardingHouseId int NOT NULL REFERENCES BoardingHouses(Id) ON DELETE CASCADE,
    Name            nvarchar(100) NOT NULL,
    Price           decimal(18,2) NOT NULL,
    Unit            nvarchar(30)  NOT NULL,           -- PerMonth | PerPerson
    IsActive        bit NOT NULL,
    CONSTRAINT CK_Service_Price CHECK ([Price] >= 0)
);
GO

-- ===================== Phòng, tài sản =====================
CREATE TABLE Rooms (
    Id              int IDENTITY PRIMARY KEY,
    BoardingHouseId int NOT NULL REFERENCES BoardingHouses(Id),
    RoomNumber      nvarchar(10) NOT NULL,
    Floor           int NOT NULL,
    Area            decimal(18,2) NOT NULL,
    Price           decimal(18,2) NOT NULL,
    DepositAmount   decimal(18,2) NOT NULL,
    Capacity        int NOT NULL,
    Status          nvarchar(30) NOT NULL,            -- Available | Occupied | Maintenance
    Description     nvarchar(2000) NULL,
    ViewCount       int NOT NULL,
    RowVersion      rowversion NOT NULL,              -- chống duyệt thuê trùng (409)
    CreatedAt       datetime2 NOT NULL,
    CONSTRAINT IX_Rooms_BoardingHouseId_RoomNumber UNIQUE (BoardingHouseId, RoomNumber),
    CONSTRAINT CK_Room_Floor         CHECK ([Floor] >= 1),
    CONSTRAINT CK_Room_Price         CHECK ([Price] > 0),
    CONSTRAINT CK_Room_DepositAmount CHECK ([DepositAmount] >= 0),
    CONSTRAINT CK_Room_Area          CHECK ([Area] > 0),
    CONSTRAINT CK_Room_Capacity      CHECK ([Capacity] >= 1 AND [Capacity] <= 10)
);
CREATE INDEX IX_Rooms_BoardingHouseId_Floor ON Rooms(BoardingHouseId, Floor);

CREATE TABLE RoomImages (
    Id        int IDENTITY PRIMARY KEY,
    RoomId    int NOT NULL REFERENCES Rooms(Id) ON DELETE CASCADE,
    Url       nvarchar(500) NOT NULL,
    SortOrder int NOT NULL
);

CREATE TABLE RoomAmenities (
    RoomId    int NOT NULL REFERENCES Rooms(Id) ON DELETE CASCADE,
    AmenityId int NOT NULL REFERENCES Amenities(Id),
    PRIMARY KEY (RoomId, AmenityId)
);

CREATE TABLE RoomAssets (
    Id             int IDENTITY PRIMARY KEY,
    RoomId         int NOT NULL REFERENCES Rooms(Id) ON DELETE CASCADE,
    AssetTypeId    int NOT NULL REFERENCES AssetTypes(Id),
    Name           nvarchar(100) NOT NULL,
    Quantity       int NOT NULL,
    Condition      nvarchar(30) NOT NULL,             -- Good | Damaged | Broken
    EstimatedValue decimal(18,2) NOT NULL,
    Note           nvarchar(500) NULL,
    CONSTRAINT CK_RoomAsset_Quantity       CHECK ([Quantity] >= 1),
    CONSTRAINT CK_RoomAsset_EstimatedValue CHECK ([EstimatedValue] >= 0)
);
GO

-- ===================== Lịch xem, yêu cầu thuê, hợp đồng =====================
CREATE TABLE ViewingAppointments (
    Id           int IDENTITY PRIMARY KEY,
    RoomId       int NOT NULL REFERENCES Rooms(Id),
    TenantId     nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id),
    ScheduledAt  datetime2 NOT NULL,
    Status       nvarchar(30) NOT NULL,               -- Pending | Confirmed | Rejected | Cancelled
    Note         nvarchar(500) NULL,
    RejectReason nvarchar(500) NULL,
    CreatedAt    datetime2 NOT NULL
);
CREATE INDEX IX_ViewingAppointments_TenantId_Status ON ViewingAppointments(TenantId, Status);
CREATE UNIQUE INDEX UX_Appointment_OnePendingPerRoomAndTenant ON ViewingAppointments(TenantId, RoomId) WHERE [Status] = 'Pending';

CREATE TABLE RentalRequests (
    Id                int IDENTITY PRIMARY KEY,
    RoomId            int NOT NULL REFERENCES Rooms(Id),
    TenantId          nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id),
    NumberOfOccupants int NOT NULL,
    MoveInDate        datetime2 NOT NULL,
    DurationMonths    int NOT NULL,
    Status            nvarchar(30) NOT NULL,          -- Pending | Approved | Rejected | Cancelled
    Note              nvarchar(500) NULL,
    RejectReason      nvarchar(500) NULL,
    CreatedAt         datetime2 NOT NULL,
    CONSTRAINT CK_RentalRequest_Occupants CHECK ([NumberOfOccupants] >= 1),
    CONSTRAINT CK_RentalRequest_Duration  CHECK ([DurationMonths] >= 1)
);
CREATE INDEX IX_RentalRequests_RoomId_Status ON RentalRequests(RoomId, Status);
CREATE UNIQUE INDEX UX_RentalRequest_OnePendingPerRoomAndTenant ON RentalRequests(TenantId, RoomId) WHERE [Status] = 'Pending';

CREATE TABLE Contracts (
    Id              int IDENTITY PRIMARY KEY,
    RoomId          int NOT NULL REFERENCES Rooms(Id),
    TenantId        nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id),
    RentalRequestId int NULL REFERENCES RentalRequests(Id),
    StartDate       datetime2 NOT NULL,
    EndDate         datetime2 NOT NULL,
    MonthlyRent     decimal(18,2) NOT NULL,
    DepositAmount   decimal(18,2) NOT NULL,
    DepositPaidAt   datetime2 NULL,
    InitialElectricIndex int NOT NULL DEFAULT 0,      -- chỉ số điện lúc nhận phòng
    InitialWaterIndex    int NOT NULL DEFAULT 0,      -- chỉ số nước lúc nhận phòng
    Status          nvarchar(30) NOT NULL,            -- Active | Terminated (hết hạn tính khi đọc)
    TerminatedAt    datetime2 NULL,
    CreatedAt       datetime2 NOT NULL,
    CONSTRAINT CK_Contract_Dates       CHECK ([EndDate] > [StartDate]),
    CONSTRAINT CK_Contract_MonthlyRent CHECK ([MonthlyRent] > 0),
    CONSTRAINT CK_Contract_Deposit     CHECK ([DepositAmount] >= 0),
    CONSTRAINT CK_Contract_InitialReadings CHECK ([InitialElectricIndex] >= 0 AND [InitialWaterIndex] >= 0)
);
-- Mỗi người thuê một hợp đồng Active; mỗi phòng một hợp đồng Active
CREATE UNIQUE INDEX UX_Contract_OneActivePerTenant ON Contracts(TenantId) WHERE [Status] = 'Active';
CREATE UNIQUE INDEX UX_Contract_OneActivePerRoom   ON Contracts(RoomId)   WHERE [Status] = 'Active';

CREATE TABLE ContractMembers (
    Id         int IDENTITY PRIMARY KEY,
    ContractId int NOT NULL REFERENCES Contracts(Id) ON DELETE CASCADE,
    FullName   nvarchar(100) NOT NULL,
    CitizenId  nvarchar(12)  NOT NULL,
    Phone      nvarchar(10)  NULL
);

CREATE TABLE ContractServices (
    ContractId int NOT NULL REFERENCES Contracts(Id) ON DELETE CASCADE,
    ServiceId  int NOT NULL REFERENCES Services(Id),
    PRIMARY KEY (ContractId, ServiceId)
);

-- Biên bản bàn giao tài sản (CheckIn khi nhận phòng, CheckOut khi trả)
CREATE TABLE ContractAssets (
    Id                  int IDENTITY PRIMARY KEY,
    ContractId          int NOT NULL REFERENCES Contracts(Id) ON DELETE CASCADE,
    RoomAssetId         int NOT NULL REFERENCES RoomAssets(Id),
    Stage               nvarchar(30) NOT NULL,        -- CheckIn | CheckOut
    Name                nvarchar(100) NOT NULL,
    Quantity            int NOT NULL,
    Condition           nvarchar(30) NOT NULL,
    DeductionAmount     decimal(18,2) NOT NULL,
    Note                nvarchar(500) NULL,
    ConfirmedByTenantAt datetime2 NULL,
    CONSTRAINT IX_ContractAssets_ContractId_RoomAssetId_Stage UNIQUE (ContractId, RoomAssetId, Stage),
    CONSTRAINT CK_ContractAsset_Quantity  CHECK ([Quantity] >= 0),
    CONSTRAINT CK_ContractAsset_Deduction CHECK ([DeductionAmount] >= 0)
);

CREATE TABLE DepositSettlements (
    Id              int IDENTITY PRIMARY KEY,
    ContractId      int NOT NULL REFERENCES Contracts(Id) ON DELETE CASCADE,
    DepositAmount   decimal(18,2) NOT NULL,
    DamageDeduction decimal(18,2) NOT NULL,
    UnpaidDeduction decimal(18,2) NOT NULL,
    RefundAmount    decimal(18,2) NOT NULL,
    Note            nvarchar(500) NULL,
    SettledAt       datetime2 NOT NULL,
    CONSTRAINT IX_DepositSettlements_ContractId UNIQUE (ContractId),
    CONSTRAINT CK_DepositSettlement_Deductions CHECK ([DamageDeduction] >= 0 AND [UnpaidDeduction] >= 0 AND [DamageDeduction] + [UnpaidDeduction] <= [DepositAmount]),
    CONSTRAINT CK_DepositSettlement_Refund     CHECK ([RefundAmount] >= 0)
);

CREATE TABLE ContractChangeRequests (
    Id           int IDENTITY PRIMARY KEY,
    ContractId   int NOT NULL REFERENCES Contracts(Id),
    Type         nvarchar(30) NOT NULL,               -- Renew | Terminate
    NewEndDate   datetime2 NULL,
    Reason       nvarchar(500) NULL,
    Status       nvarchar(30) NOT NULL,               -- Pending | Approved | Rejected
    DecisionNote nvarchar(500) NULL,
    CreatedAt    datetime2 NOT NULL,
    DecidedAt    datetime2 NULL
);
CREATE UNIQUE INDEX UX_ContractChangeRequest_OnePendingPerContract ON ContractChangeRequests(ContractId) WHERE [Status] = 'Pending';
GO

-- ===================== Điện nước, hóa đơn, thanh toán =====================
CREATE TABLE UtilityReadings (
    Id            int IDENTITY PRIMARY KEY,
    ContractId    int NOT NULL REFERENCES Contracts(Id) ON DELETE CASCADE,
    Month         int NOT NULL,
    Year          int NOT NULL,
    ElectricIndex int NOT NULL,
    WaterIndex    int NOT NULL,
    RecordedAt    datetime2 NOT NULL,
    CONSTRAINT IX_UtilityReadings_ContractId_Year_Month UNIQUE (ContractId, Year, Month),
    CONSTRAINT CK_UtilityReading_Month CHECK ([Month] BETWEEN 1 AND 12),
    CONSTRAINT CK_UtilityReading_Index CHECK ([ElectricIndex] >= 0 AND [WaterIndex] >= 0)
);

CREATE TABLE Invoices (
    Id           int IDENTITY PRIMARY KEY,
    ContractId   int NOT NULL REFERENCES Contracts(Id) ON DELETE CASCADE,
    Month        int NOT NULL,
    Year         int NOT NULL,
    Total        decimal(18,2) NOT NULL,
    DueDate      datetime2 NOT NULL,
    Status       nvarchar(30) NOT NULL,               -- Unpaid | PendingConfirm | Paid (Overdue tính khi đọc)
    RejectReason nvarchar(500) NULL,
    CreatedAt    datetime2 NOT NULL,
    CONSTRAINT IX_Invoices_ContractId_Year_Month UNIQUE (ContractId, Year, Month),
    CONSTRAINT CK_Invoice_Month CHECK ([Month] BETWEEN 1 AND 12),
    CONSTRAINT CK_Invoice_Total CHECK ([Total] >= 0)
);
CREATE INDEX IX_Invoices_Status ON Invoices(Status);

CREATE TABLE InvoiceDetails (
    Id        int IDENTITY PRIMARY KEY,
    InvoiceId int NOT NULL REFERENCES Invoices(Id) ON DELETE CASCADE,
    ItemName  nvarchar(100) NOT NULL,
    Quantity  decimal(18,2) NOT NULL,
    Unit      nvarchar(20) NULL,
    UnitPrice decimal(18,2) NOT NULL,                 -- đơn giá tại thời điểm lập (điện ≤ 4.000)
    Amount    decimal(18,2) NOT NULL
);

CREATE TABLE Payments (
    Id            int IDENTITY PRIMARY KEY,
    InvoiceId     int NOT NULL REFERENCES Invoices(Id) ON DELETE CASCADE,
    Amount        decimal(18,2) NOT NULL,
    ProofImageUrl nvarchar(500) NULL,
    Note          nvarchar(500) NULL,
    ReportedAt    datetime2 NOT NULL,
    ConfirmedAt   datetime2 NULL,
    CONSTRAINT CK_Payment_Amount CHECK ([Amount] > 0)
);
GO

-- ===================== Sự cố, đánh giá, khiếu nại =====================
CREATE TABLE MaintenanceRequests (
    Id          int IDENTITY PRIMARY KEY,
    ContractId  int NOT NULL REFERENCES Contracts(Id),
    RoomAssetId int NULL REFERENCES RoomAssets(Id),
    Title       nvarchar(200)  NOT NULL,
    Description nvarchar(2000) NOT NULL,
    ImageUrl    nvarchar(500)  NULL,
    Status      nvarchar(30)   NOT NULL,              -- Open | InProgress | Resolved
    ResolveNote nvarchar(500)  NULL,
    CreatedAt   datetime2 NOT NULL,
    ResolvedAt  datetime2 NULL
);

CREATE TABLE Reviews (
    Id         int IDENTITY PRIMARY KEY,
    ContractId int NOT NULL REFERENCES Contracts(Id),
    Rating     int NOT NULL,
    Comment    nvarchar(1000) NULL,
    CreatedAt  datetime2 NOT NULL,
    CONSTRAINT IX_Reviews_ContractId UNIQUE (ContractId),          -- mỗi hợp đồng đánh giá một lần
    CONSTRAINT CK_Review_Rating CHECK ([Rating] BETWEEN 1 AND 5)
);

CREATE TABLE Reports (
    Id          int IDENTITY PRIMARY KEY,
    ReporterId  nvarchar(450) NOT NULL REFERENCES AspNetUsers(Id),
    TargetType  nvarchar(30) NOT NULL,                -- Landlord | Room | House
    TargetId    int NOT NULL,
    Reason      nvarchar(1000) NOT NULL,
    Status      nvarchar(30) NOT NULL,                -- Open | Resolved | Dismissed
    ResolveNote nvarchar(500) NULL,
    CreatedAt   datetime2 NOT NULL,
    ResolvedAt  datetime2 NULL
);
CREATE INDEX IX_Reports_Status ON Reports(Status);
CREATE UNIQUE INDEX UX_Report_OneOpenPerReporterAndTarget ON Reports(ReporterId, TargetType, TargetId) WHERE [Status] = 'Open';
GO

-- ===================== Dữ liệu mẫu =====================
-- Không INSERT ở đây: NestU.API tự seed toàn bộ dữ liệu mẫu (role, tài khoản, danh mục, nhà trọ, phòng,
-- hợp đồng, hóa đơn 12 tháng) ở lần chạy đầu tiên, vì mật khẩu phải được băm bằng ASP.NET Core Identity.
