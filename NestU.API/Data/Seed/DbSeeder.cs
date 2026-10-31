using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;

namespace NestU.API.Data.Seed;

/// <summary>
/// Chạy khi khởi động ở môi trường Development: migrate CSDL rồi seed dữ liệu mẫu nếu chưa có.
/// Mật khẩu chung đọc từ appsettings.Development.json: Seed:Password.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<NestUDbContext>();
        // Có migration → Migrate; chưa có (chạy lần đầu quên tạo migration) → tạo thẳng schema để vẫn chạy được
        if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();

        if (!config.GetValue<bool>("Seed:Enabled")) return;

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var password = config["Seed:Password"] ?? "NestU@123";

        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (await db.Areas.AnyAsync()) return; // đã seed rồi

        // ----- Danh mục -----
        var areas = new[] { "Quận 1", "Quận 3", "Quận 7", "Quận 10", "Bình Thạnh", "Thủ Đức", "Gò Vấp", "Tân Bình" }
            .Select(n => new Area { Name = n }).ToList();
        db.Areas.AddRange(areas);

        var amenities = new (string Name, string Icon)[]
        {
            ("Wifi", "bi-wifi"), ("Máy lạnh", "bi-snow"), ("WC riêng", "bi-droplet"), ("Chỗ để xe", "bi-bicycle"),
            ("Gác lửng", "bi-layers"), ("Cửa sổ", "bi-brightness-high"), ("Bếp riêng", "bi-fire"), ("Giờ giấc tự do", "bi-unlock")
        }.Select(a => new Amenity { Name = a.Name, Icon = a.Icon }).ToList();
        db.Amenities.AddRange(amenities);

        var assetTypes = new[] { "Giường", "Nệm", "Tủ quần áo", "Bàn học", "Ghế", "Máy lạnh", "Quạt", "Bình nóng lạnh", "Kệ bếp", "Đèn" }
            .Select(n => new AssetType { Name = n }).ToList();
        db.AssetTypes.AddRange(assetTypes);

        var universities = new List<University>
        {
            new() { Name = "Đại học FPT TP.HCM", ShortName = "FPTU", Latitude = 10.8411, Longitude = 106.8098 },
            new() { Name = "Đại học Bách Khoa TP.HCM", ShortName = "HCMUT", Latitude = 10.7725, Longitude = 106.6580 },
            new() { Name = "Đại học Khoa học Tự nhiên", ShortName = "HCMUS", Latitude = 10.7628, Longitude = 106.6824 },
            new() { Name = "Đại học Kinh tế TP.HCM", ShortName = "UEH", Latitude = 10.7830, Longitude = 106.6946 },
            new() { Name = "Đại học Sư phạm Kỹ thuật", ShortName = "HCMUTE", Latitude = 10.8506, Longitude = 106.7719 }
        };
        db.Universities.AddRange(universities);
        await db.SaveChangesAsync();

        // ----- Tài khoản -----
        async Task<ApplicationUser> CreateUser(string email, string fullName, string role, string phone)
        {
            var u = new ApplicationUser { UserName = email, Email = email, FullName = fullName, PhoneNumber = phone, EmailConfirmed = true };
            var r = await userManager.CreateAsync(u, password);
            if (!r.Succeeded) throw new Exception("Seed user failed: " + string.Join("; ", r.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(u, role);
            return u;
        }

        var admin = await CreateUser("admin@nestu.vn", "Quản trị NestU", Roles.Admin, "0900000001");

        var landlord1 = await CreateUser("chutro1@nestu.vn", "Nguyễn Văn Chủ", Roles.Landlord, "0911111111");
        db.LandlordProfiles.Add(new LandlordProfile
        {
            UserId = landlord1.Id, FullName = landlord1.FullName, CitizenId = "079123456789", Phone = "0911111111",
            ApprovalStatus = ApprovalStatus.Approved, ReviewedAt = DateTime.UtcNow
        });

        var landlord2 = await CreateUser("chutro2@nestu.vn", "Trần Thị Mới", Roles.Landlord, "0922222222");
        db.LandlordProfiles.Add(new LandlordProfile
        {
            UserId = landlord2.Id, FullName = landlord2.FullName, CitizenId = "079987654321", Phone = "0922222222",
            ApprovalStatus = ApprovalStatus.Pending
        });

        var tenant1 = await CreateUser("sinhvien1@nestu.vn", "Lê Minh Sinh", Roles.Tenant, "0933333333");
        db.TenantProfiles.Add(new TenantProfile
        {
            UserId = tenant1.Id, FullName = tenant1.FullName, CitizenId = "079111222333", Phone = "0933333333",
            IsStudent = true, StudentCode = "SE170001", UniversityId = universities[0].Id
        });

        var tenant2 = await CreateUser("sinhvien2@nestu.vn", "Phạm Thu Hà", Roles.Tenant, "0944444444");
        db.TenantProfiles.Add(new TenantProfile
        {
            UserId = tenant2.Id, FullName = tenant2.FullName, CitizenId = "079444555666", Phone = "0944444444",
            IsStudent = true, StudentCode = "SE170002", UniversityId = universities[1].Id
        });

        var tenant3 = await CreateUser("khach1@nestu.vn", "Võ Hoàng Nam", Roles.Tenant, "0955555555");
        db.TenantProfiles.Add(new TenantProfile
        {
            UserId = tenant3.Id, FullName = tenant3.FullName, CitizenId = "079777888999", Phone = "0955555555",
            IsStudent = false
        });
        await db.SaveChangesAsync();

        // ----- Nhà trọ Active 3 tầng của Landlord 1 -----
        var house = new BoardingHouse
        {
            LandlordId = landlord1.Id,
            Name = "Nhà trọ An Bình",
            Address = "123 Lê Văn Việt, Phường Hiệp Phú, Thủ Đức",
            AreaId = areas.First(a => a.Name == "Thủ Đức").Id,
            Latitude = 10.8460, Longitude = 106.7790,
            TotalFloors = 3,
            Status = HouseStatus.Active,
            ElectricityPrice = 3500,
            WaterPrice = 15000, WaterBillingType = WaterBillingType.PerM3,
            AllowedTenantType = AllowedTenantType.All,
            Description = "Nhà trọ sạch sẽ, an ninh, gần Đại học FPT và khu công nghệ cao. Đạt chuẩn PCCC.",
            BankCode = "VCB", BankAccount = "0123456789", AccountName = "NGUYEN VAN CHU"
        };
        db.BoardingHouses.Add(house);
        await db.SaveChangesAsync();

        db.FireSafetyCertificates.Add(new FireSafetyCertificate
        {
            BoardingHouseId = house.Id, CertificateNo = "PCCC-2026-0001",
            FileUrl = "/uploads/seed/pccc-anbinh.pdf", Status = CertificateStatus.Approved,
            ReviewedAt = DateTime.UtcNow
        });

        db.Services.AddRange(
            new Service { BoardingHouseId = house.Id, Name = "Wifi", Price = 100000, Unit = ServiceUnit.PerMonth },
            new Service { BoardingHouseId = house.Id, Name = "Rác", Price = 30000, Unit = ServiceUnit.PerMonth },
            new Service { BoardingHouseId = house.Id, Name = "Gửi xe", Price = 100000, Unit = ServiceUnit.PerPerson });

        // 3 tầng × 4 phòng
        var rnd = new Random(42);
        for (var floor = 1; floor <= 3; floor++)
        {
            for (var n = 1; n <= 4; n++)
            {
                var price = 2500000 + floor * 200000 + (n % 2) * 300000;
                var room = new Room
                {
                    BoardingHouseId = house.Id,
                    RoomNumber = (floor * 100 + n).ToString(),
                    Floor = floor,
                    Area = 18 + (n % 2) * 4,
                    Price = price,
                    DepositAmount = price,
                    Capacity = 2 + (n % 2),
                    Status = RoomStatus.Available,
                    Description = $"Phòng {floor * 100 + n}, tầng {floor}, thoáng mát.",
                    ViewCount = rnd.Next(5, 80)
                };
                room.Images.Add(new RoomImage { Url = $"https://picsum.photos/seed/nestu{floor}{n}/800/600", SortOrder = 0 });
                room.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[0].Id });
                room.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[2].Id });
                if (n % 2 == 1) room.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[1].Id });

                room.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[0].Id, Name = "Giường 1m4", Quantity = 1, EstimatedValue = 1500000 });
                room.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[2].Id, Name = "Tủ quần áo", Quantity = 1, EstimatedValue = 1200000 });
                room.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[6].Id, Name = "Quạt treo tường", Quantity = 1, EstimatedValue = 400000 });
                if (n % 2 == 1)
                    room.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[5].Id, Name = "Máy lạnh 1HP", Quantity = 1, EstimatedValue = 6000000 });

                db.Rooms.Add(room);
            }
        }

        await db.SaveChangesAsync();
    }
}
