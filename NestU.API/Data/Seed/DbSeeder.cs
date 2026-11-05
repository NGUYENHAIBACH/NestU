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

        // Đã có tài khoản → không seed đè. Không dựa vào danh mục vì docs/NestUDb.sql chèn sẵn danh mục nhưng không có tài khoản.
        if (await db.Users.AnyAsync()) return;

        // ----- Danh mục: dùng lại mục đã có theo tên, chỉ thêm mục còn thiếu -----
        var areas = await EnsureByNameAsync(db.Areas,
            new[] { "Quận 1", "Quận 3", "Quận 7", "Quận 10", "Bình Thạnh", "Thủ Đức", "Gò Vấp", "Tân Bình" }.Select(n => new Area { Name = n }),
            a => a.Name);

        var amenities = await EnsureByNameAsync(db.Amenities, new (string Name, string Icon)[]
        {
            ("Wifi", "bi-wifi"), ("Máy lạnh", "bi-snow"), ("WC riêng", "bi-droplet"), ("Chỗ để xe", "bi-bicycle"),
            ("Gác lửng", "bi-layers"), ("Cửa sổ", "bi-brightness-high"), ("Bếp riêng", "bi-fire"), ("Giờ giấc tự do", "bi-unlock")
        }.Select(a => new Amenity { Name = a.Name, Icon = a.Icon }), a => a.Name);

        var assetTypes = await EnsureByNameAsync(db.AssetTypes,
            new[] { "Giường", "Nệm", "Tủ quần áo", "Bàn học", "Ghế", "Máy lạnh", "Quạt", "Bình nóng lạnh", "Kệ bếp", "Đèn" }.Select(n => new AssetType { Name = n }),
            a => a.Name);

        var universities = await EnsureByNameAsync(db.Universities, new List<University>
        {
            new() { Name = "Đại học FPT TP.HCM", ShortName = "FPTU", Latitude = 10.8411, Longitude = 106.8098 },
            new() { Name = "Đại học Bách Khoa TP.HCM", ShortName = "HCMUT", Latitude = 10.7725, Longitude = 106.6580 },
            new() { Name = "Đại học Khoa học Tự nhiên", ShortName = "HCMUS", Latitude = 10.7628, Longitude = 106.6824 },
            new() { Name = "Đại học Kinh tế TP.HCM", ShortName = "UEH", Latitude = 10.7830, Longitude = 106.6946 },
            new() { Name = "Đại học Sư phạm Kỹ thuật", ShortName = "HCMUTE", Latitude = 10.8506, Longitude = 106.7719 }
        }, u => u.Name);
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

        // ----- Nhà trọ Luxury 3 tầng của Landlord 1 -----
        var luxuryHouse = new BoardingHouse
        {
            LandlordId = landlord1.Id,
            Name = "Nhà Trọ Luxury",
            Address = "45 Nguyễn Huệ",
            AreaId = areas.First(a => a.Name == "Bình Thạnh").Id,
            Latitude = 10.7769, Longitude = 106.7009,
            TotalFloors = 3,
            Status = HouseStatus.Active,
            ElectricityPrice = 3500,
            WaterPrice = 15000, WaterBillingType = WaterBillingType.PerM3,
            AllowedTenantType = AllowedTenantType.All,
            Description = "Vip sang trọng",
            BankCode = "VCB", BankAccount = "0123456789", AccountName = "NGUYEN VAN CHU"
        };
        db.BoardingHouses.Add(luxuryHouse);
        await db.SaveChangesAsync();

        db.FireSafetyCertificates.Add(new FireSafetyCertificate
        {
            BoardingHouseId = luxuryHouse.Id, CertificateNo = "PCC-002-001",
            FileUrl = "/uploads/seed/pccc-luxury.pdf", Status = CertificateStatus.Approved,
            ReviewedAt = DateTime.UtcNow
        });

        var luxuryRoomsData = new[]
        {
            new { RoomNumber = "101", Floor = 1, Area = 22m, Price = 2100000m, Desc = "Phòng tầng 1 studio rộng rãi, đầy đủ tiện nghi, có gác lửng hiện đại", Imgs = new[] { "https://ankhoadesign.com.vn/wp-content/uploads/2024/09/mau-phong-tro-co-gac-lung-dep-hien-dai.jpg", "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "102", Floor = 1, Area = 22m, Price = 2100000m, Desc = "Phòng tầng 1 thoáng mát, có cửa sổ lớn đón gió tự nhiên", Imgs = new[] { "https://images.unsplash.com/photo-1598928506311-c55ded91a20c?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "103", Floor = 1, Area = 25m, Price = 2300000m, Desc = "Phòng tầng 1 diện tích 25m², không gian yên tĩnh, an ninh tốt", Imgs = new[] { "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "201", Floor = 2, Area = 24m, Price = 2400000m, Desc = "Phòng tầng 2 ban công riêng rộng rãi, view thoáng, nhiều ánh sáng", Imgs = new[] { "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "202", Floor = 2, Area = 22m, Price = 2300000m, Desc = "Phòng tầng 2 thiết kế hiện đại, đầy đủ máy lạnh và WC riêng khép kín", Imgs = new[] { "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1540518614846-7eded433c457?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "203", Floor = 2, Area = 26m, Price = 2500000m, Desc = "Phòng tầng 2 studio cao cấp 26m², giờ giấc tự do không chung chủ", Imgs = new[] { "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1554995207-c18c203602cb?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "301", Floor = 3, Area = 25m, Price = 2600000m, Desc = "Phòng tầng 3 view toàn cảnh thoáng mát, ban công riêng đón nắng", Imgs = new[] { "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "302", Floor = 3, Area = 24m, Price = 2500000m, Desc = "Phòng tầng 3 nội thất mới 100%, máy lạnh inverter tiết kiệm điện", Imgs = new[] { "https://images.unsplash.com/photo-1502005229762-ee1b2b8ab32f?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1000&q=80" } },
            new { RoomNumber = "303", Floor = 3, Area = 28m, Price = 2800000m, Desc = "Phòng tầng 3 căn góc 28m², 2 cửa sổ thoáng đãng, nội thất cao cấp", Imgs = new[] { "https://images.unsplash.com/photo-1560185127-6ed189bf02f4?auto=format&fit=crop&w=1000&q=80", "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?auto=format&fit=crop&w=1000&q=80" } }
        };

        foreach (var d in luxuryRoomsData)
        {
            var r = new Room
            {
                BoardingHouseId = luxuryHouse.Id,
                RoomNumber = d.RoomNumber,
                Floor = d.Floor,
                Area = d.Area,
                Price = d.Price,
                DepositAmount = d.Price,
                Capacity = 2,
                Status = RoomStatus.Available,
                Description = d.Desc,
                ViewCount = rnd.Next(5, 50)
            };
            for (var i = 0; i < d.Imgs.Length; i++)
                r.Images.Add(new RoomImage { Url = d.Imgs[i], SortOrder = i });

            r.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[0].Id }); // Wifi
            r.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[1].Id }); // Máy lạnh
            r.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[2].Id }); // WC riêng
            r.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[3].Id }); // Chỗ để xe
            r.RoomAmenities.Add(new RoomAmenity { AmenityId = amenities[7].Id }); // Giờ giấc tự do

            r.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[0].Id, Name = "Giường nệm 1m6 × 2m", Quantity = 1, EstimatedValue = 2200000, Note = "Gỗ công nghiệp mới 100%" });
            r.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[2].Id, Name = "Tủ quần áo 2 cánh", Quantity = 1, EstimatedValue = 1600000, Note = "Có gương soi" });
            r.Assets.Add(new RoomAsset { AssetTypeId = assetTypes[6].Id, Name = "Quạt treo tường Senko", Quantity = 1, EstimatedValue = 350000, Note = "Có điều khiển từ xa" });

            db.Rooms.Add(r);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Trả về danh sách theo đúng thứ tự <paramref name="items"/>: mục trùng tên lấy bản đã có trong CSDL, mục thiếu được thêm mới.</summary>
    private static async Task<List<T>> EnsureByNameAsync<T>(DbSet<T> set, IEnumerable<T> items, Func<T, string> name) where T : class
    {
        var existing = await set.ToListAsync();
        var result = new List<T>();
        foreach (var item in items)
        {
            var found = existing.FirstOrDefault(e => name(e) == name(item));
            if (found is null) set.Add(item);
            result.Add(found ?? item);
        }
        return result;
    }
}
