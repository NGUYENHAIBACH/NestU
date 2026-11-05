using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;

namespace NestU.API.Data.Seed;

/// <summary>
/// Seed dữ liệu lịch sử ~12 tháng (hợp đồng, chỉ số điện nước, hóa đơn, thanh toán, sự cố, quyết toán cọc, đánh giá)
/// cho một số phòng của Landlord 1 để demo biểu đồ doanh thu, hóa đơn quá hạn, hợp đồng sắp hết hạn...
/// Chạy sau DbSeeder.SeedAsync; bỏ qua nếu đã có hợp đồng.
/// </summary>
public static class HistorySeeder
{
    private sealed record Plan(
        string HouseName, string RoomNumber, string TenantEmail,
        DateTime Start, DateTime End,
        string[] Members,                 // người ở cùng (ngoài người ký)
        string[] ServiceNames,
        DateTime? TerminatedAt,           // null = còn hiệu lực
        string LastInvoiceMode);          // "paid" | "unpaid" | "pending" | "overdue" (áp cho hóa đơn tháng gần nhất)

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<NestUDbContext>();
        if (!config.GetValue<bool>("Seed:Enabled")) return;
        if (await db.Contracts.AnyAsync()) return; // đã seed lịch sử

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        // Thiếu dữ liệu nền của DbSeeder (CSDL đã có tài khoản thật) → bỏ qua, không ghi dở dang
        var hasBaseData = await db.BoardingHouses.CountAsync(h => h.Name == "Nhà trọ An Bình" || h.Name == "Nhà Trọ Luxury") == 2;
        foreach (var email in new[] { "sinhvien1@nestu.vn", "sinhvien2@nestu.vn", "khach1@nestu.vn" })
            hasBaseData = hasBaseData && await userManager.FindByEmailAsync(email) != null;
        if (!hasBaseData) return;

        var password = config["Seed:Password"] ?? "NestU@123";
        var universities = await db.Universities.OrderBy(u => u.Id).ToListAsync();

        // ----- Thêm vài người thuê để có nhiều hợp đồng (mỗi Tenant chỉ 1 hợp đồng Active) -----
        var extraTenants = new[]
        {
            ("nguoithue1@nestu.vn", "Đỗ Quang Huy", "0961111111", true,  "SE170101", 0),
            ("nguoithue2@nestu.vn", "Bùi Thị Lan", "0962222222", true,  "SE170102", 1),
            ("nguoithue3@nestu.vn", "Hoàng Đức Anh", "0963333333", false, "", 0),
            ("nguoithue4@nestu.vn", "Ngô Thanh Trúc", "0964444444", true,  "SE170104", 0),
            ("nguoithue5@nestu.vn", "Trịnh Văn Long", "0965555555", false, "", 0),
            ("nguoithue6@nestu.vn", "Mai Phương Thảo", "0966666666", true,  "SE170106", 1),
        };
        var seq = 100;
        foreach (var (email, name, phone, isStudent, code, uniIdx) in extraTenants)
        {
            if (await userManager.FindByEmailAsync(email) != null) continue;
            var u = new ApplicationUser { UserName = email, Email = email, FullName = name, PhoneNumber = phone, EmailConfirmed = true, CreatedAt = new DateTime(2025, 10, 1) };
            var r = await userManager.CreateAsync(u, password);
            if (!r.Succeeded) throw new Exception("Seed tenant failed: " + string.Join("; ", r.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(u, Roles.Tenant);
            db.TenantProfiles.Add(new TenantProfile
            {
                UserId = u.Id, FullName = name, Phone = phone, CitizenId = $"0792000{seq++:D5}",
                IsStudent = isStudent,
                StudentCode = isStudent ? code : null,
                UniversityId = isStudent ? universities[Math.Min(uniIdx, universities.Count - 1)].Id : null
            });
        }
        await db.SaveChangesAsync();

        var houses = await db.BoardingHouses.Include(h => h.Services).ToListAsync();
        var luxury = houses.FirstOrDefault(h => h.Name == "Nhà Trọ Luxury");
        if (luxury != null && !luxury.Services.Any())
        {
            db.Services.AddRange(
                new Service { BoardingHouseId = luxury.Id, Name = "Wifi", Price = 120000, Unit = ServiceUnit.PerMonth },
                new Service { BoardingHouseId = luxury.Id, Name = "Rác", Price = 30000, Unit = ServiceUnit.PerMonth },
                new Service { BoardingHouseId = luxury.Id, Name = "Gửi xe", Price = 120000, Unit = ServiceUnit.PerPerson });
            await db.SaveChangesAsync();
        }

        var today = DateTime.Today;
        var lastBilledMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1); // tháng gần nhất đã lập hóa đơn

        // ----- Kịch bản từng phòng -----
        var plans = new List<Plan>
        {
            // Còn hiệu lực
            new("Nhà trọ An Bình", "101", "sinhvien1@nestu.vn", new(2025, 12, 1), new(2026, 11, 30), new[] { "Trần Văn Bình" }, new[] { "Wifi", "Rác", "Gửi xe" }, null, "pending"), // sắp hết hạn, hóa đơn chờ xác nhận
            new("Nhà trọ An Bình", "102", "sinhvien2@nestu.vn", new(2026, 3, 1), new(2027, 2, 28), Array.Empty<string>(), new[] { "Wifi", "Rác" }, null, "unpaid"),
            new("Nhà trọ An Bình", "103", "nguoithue1@nestu.vn", new(2025, 11, 1), new(2026, 10, 31), Array.Empty<string>(), new[] { "Wifi", "Rác", "Gửi xe" }, null, "overdue"), // đã quá hạn hợp đồng + hóa đơn quá hạn
            new("Nhà trọ An Bình", "201", "nguoithue2@nestu.vn", new(2026, 1, 15), new(2027, 1, 14), new[] { "Bùi Thị Hoa" }, new[] { "Wifi", "Rác", "Gửi xe" }, null, "paid"),
            new("Nhà trọ An Bình", "203", "nguoithue3@nestu.vn", new(2026, 5, 1), new(2026, 12, 31), Array.Empty<string>(), new[] { "Wifi" }, null, "paid"),
            new("Nhà trọ An Bình", "301", "nguoithue4@nestu.vn", new(2026, 8, 1), new(2027, 7, 31), Array.Empty<string>(), new[] { "Wifi", "Rác" }, null, "unpaid"),
            new("Nhà Trọ Luxury", "201", "khach1@nestu.vn", new(2026, 6, 15), new(2026, 12, 14), Array.Empty<string>(), new[] { "Wifi", "Rác", "Gửi xe" }, null, "unpaid"),
            new("Nhà Trọ Luxury", "302", "nguoithue6@nestu.vn", new(2026, 8, 1), new(2027, 7, 31), Array.Empty<string>(), new[] { "Wifi", "Gửi xe" }, null, "paid"),
            // Đã kết thúc (có quyết toán cọc + đánh giá)
            new("Nhà trọ An Bình", "104", "nguoithue5@nestu.vn", new(2025, 11, 1), new(2026, 4, 30), Array.Empty<string>(), new[] { "Wifi", "Rác" }, new DateTime(2026, 4, 30), "paid"),
            new("Nhà Trọ Luxury", "101", "nguoithue6@nestu.vn", new(2026, 1, 1), new(2026, 6, 30), Array.Empty<string>(), new[] { "Wifi", "Rác" }, new DateTime(2026, 6, 30), "paid"),
        };

        var rnd = new Random(2026);
        var reviews = new (int Rating, string Comment)[]
        {
            (5, "Phòng sạch, chủ trọ nhiệt tình, hỗ trợ sửa chữa nhanh. Sẽ giới thiệu bạn bè."),
            (4, "Vị trí thuận tiện, giá hợp lý. Trừ điểm vì wifi thỉnh thoảng chập chờn."),
        };
        var reviewIdx = 0;
        var payIdx = 0;

        foreach (var p in plans)
        {
            var house = houses.First(h => h.Name == p.HouseName);
            var room = await db.Rooms.Include(r => r.Assets)
                .FirstAsync(r => r.BoardingHouseId == house.Id && r.RoomNumber == p.RoomNumber);
            var tenant = await userManager.FindByEmailAsync(p.TenantEmail)
                ?? throw new Exception($"Thiếu tài khoản {p.TenantEmail}");
            var houseServices = await db.Services.Where(s => s.BoardingHouseId == house.Id && p.ServiceNames.Contains(s.Name)).ToListAsync();
            var occupants = 1 + p.Members.Length;

            // Lịch xem + yêu cầu thuê đã duyệt
            db.ViewingAppointments.Add(new ViewingAppointment
            {
                RoomId = room.Id, TenantId = tenant.Id, ScheduledAt = p.Start.AddDays(-10).AddHours(9),
                Status = AppointmentStatus.Confirmed, Note = "Xem phòng buổi sáng", CreatedAt = p.Start.AddDays(-14)
            });
            var request = new RentalRequest
            {
                RoomId = room.Id, TenantId = tenant.Id, NumberOfOccupants = occupants, MoveInDate = p.Start,
                DurationMonths = Math.Max(1, (p.End.Year - p.Start.Year) * 12 + p.End.Month - p.Start.Month + 1),
                Status = RentalRequestStatus.Approved, CreatedAt = p.Start.AddDays(-9)
            };
            db.RentalRequests.Add(request);
            await db.SaveChangesAsync();

            var initialElectric = rnd.Next(1200, 4800);
            var initialWater = rnd.Next(100, 600);
            var contract = new Contract
            {
                RoomId = room.Id, TenantId = tenant.Id, RentalRequestId = request.Id,
                StartDate = p.Start, EndDate = p.End, MonthlyRent = room.Price, DepositAmount = room.DepositAmount,
                DepositPaidAt = p.Start.AddDays(-5), InitialElectricIndex = initialElectric, InitialWaterIndex = initialWater,
                Status = p.TerminatedAt.HasValue ? ContractStatus.Terminated : ContractStatus.Active,
                TerminatedAt = p.TerminatedAt, CreatedAt = p.Start.AddDays(-5)
            };
            foreach (var m in p.Members)
                contract.Members.Add(new ContractMember { FullName = m, CitizenId = $"0793000{seq++:D5}", Phone = $"09{rnd.Next(10000000, 99999999)}" });
            foreach (var s in houseServices)
                contract.Services.Add(new ContractService { ServiceId = s.Id });
            foreach (var a in room.Assets)
                contract.Assets.Add(new ContractAsset
                {
                    RoomAssetId = a.Id, Stage = AssetStage.CheckIn, Name = a.Name, Quantity = a.Quantity,
                    Condition = AssetCondition.Good, ConfirmedByTenantAt = p.Start.AddHours(10)
                });
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            // Chỉ số + hóa đơn từng tháng
            var lastMonth = p.TerminatedAt.HasValue
                ? new DateTime(p.TerminatedAt.Value.Year, p.TerminatedAt.Value.Month, 1)
                : lastBilledMonth;
            var electric = initialElectric;
            var water = initialWater;
            var unpaidTotal = 0m;
            for (var month = new DateTime(p.Start.Year, p.Start.Month, 1); month <= lastMonth; month = month.AddMonths(1))
            {
                var prevElectric = electric;
                var prevWater = water;
                // mùa nóng (4–8) dùng điện nhiều hơn
                var hot = month.Month is >= 4 and <= 8 ? 40 : 0;
                electric += rnd.Next(55, 110) + hot + (occupants - 1) * 25;
                water += rnd.Next(3, 6) + (occupants - 1) * 2;

                var monthEnd = month.AddMonths(1).AddDays(-1);
                db.UtilityReadings.Add(new UtilityReading
                {
                    ContractId = contract.Id, Month = month.Month, Year = month.Year,
                    ElectricIndex = electric, WaterIndex = water, RecordedAt = monthEnd.AddHours(18)
                });

                var kwh = electric - prevElectric;
                var m3 = water - prevWater;
                var details = new List<InvoiceDetail>
                {
                    new() { ItemName = $"Tiền phòng tháng {month.Month}/{month.Year}", Quantity = 1, Unit = "tháng", UnitPrice = contract.MonthlyRent, Amount = contract.MonthlyRent },
                    new() { ItemName = $"Tiền điện ({prevElectric} → {electric})", Quantity = kwh, Unit = "kWh", UnitPrice = house.ElectricityPrice, Amount = kwh * house.ElectricityPrice }
                };
                if (house.WaterBillingType == WaterBillingType.PerM3)
                    details.Add(new InvoiceDetail { ItemName = $"Tiền nước ({prevWater} → {water})", Quantity = m3, Unit = "m³", UnitPrice = house.WaterPrice, Amount = m3 * house.WaterPrice });
                else
                    details.Add(new InvoiceDetail { ItemName = "Tiền nước (theo người)", Quantity = occupants, Unit = "người", UnitPrice = house.WaterPrice, Amount = occupants * house.WaterPrice });
                foreach (var s in houseServices)
                {
                    var qty = s.Unit == ServiceUnit.PerPerson ? occupants : 1;
                    details.Add(new InvoiceDetail { ItemName = s.Name, Quantity = qty, Unit = s.Unit == ServiceUnit.PerPerson ? "người" : "tháng", UnitPrice = s.Price, Amount = qty * s.Price });
                }

                var createdAt = month.AddMonths(1).AddHours(8);           // lập đầu tháng sau
                var dueDate = month.AddMonths(1).AddDays(9);              // hạn ngày 10 tháng sau
                var isLast = month == lastMonth && !p.TerminatedAt.HasValue;
                var mode = isLast ? p.LastInvoiceMode : "paid";
                if (mode == "overdue") { createdAt = createdAt.AddMonths(-1); dueDate = dueDate.AddMonths(-1); } // hóa đơn tháng trước nữa còn nợ

                var invoice = new Invoice
                {
                    ContractId = contract.Id, Month = month.Month, Year = month.Year,
                    Total = details.Sum(d => d.Amount), DueDate = dueDate, CreatedAt = createdAt,
                    Status = mode switch { "paid" => InvoiceStatus.Paid, "pending" => InvoiceStatus.PendingConfirm, _ => InvoiceStatus.Unpaid },
                    Details = details
                };
                if (mode is "paid" or "pending")
                {
                    var reported = createdAt.AddDays(rnd.Next(1, 8)).AddHours(rnd.Next(8, 21));
                    invoice.Payments.Add(new Payment
                    {
                        Amount = invoice.Total, Note = "Chuyển khoản",
                        ProofImageUrl = $"https://picsum.photos/seed/nestupay{++payIdx}/600/800",
                        ReportedAt = reported,
                        ConfirmedAt = mode == "paid" ? reported.AddHours(rnd.Next(1, 30)) : null
                    });
                }
                else unpaidTotal += invoice.Total;
                db.Invoices.Add(invoice);
            }

            // Sự cố: 1 đã xử lý cho hợp đồng dài, 1 đang mở cho phòng 101
            if (contract.StartDate < today.AddMonths(-4))
            {
                var asset = room.Assets.FirstOrDefault(a => a.Name.Contains("Quạt")) ?? room.Assets.First();
                db.MaintenanceRequests.Add(new MaintenanceRequest
                {
                    ContractId = contract.Id, RoomAssetId = asset.Id, Title = $"{asset.Name} kêu to",
                    Description = "Quạt chạy rung và kêu to khi bật số lớn.", Status = MaintenanceStatus.Resolved,
                    ResolveNote = "Đã tra dầu, siết lại ốc.", CreatedAt = contract.StartDate.AddMonths(2).AddDays(3), ResolvedAt = contract.StartDate.AddMonths(2).AddDays(5)
                });
            }
            if (p.RoomNumber == "101" && p.HouseName == "Nhà trọ An Bình")
                db.MaintenanceRequests.Add(new MaintenanceRequest
                {
                    ContractId = contract.Id, Title = "Vòi nước WC rỉ nước", Description = "Vòi lavabo rỉ nước liên tục, xin kiểm tra giúp.",
                    Status = MaintenanceStatus.Open, CreatedAt = today.AddDays(-2).AddHours(20)
                });

            if (p.TerminatedAt.HasValue)
            {
                // Biên bản trả phòng + quyết toán cọc + đánh giá
                var damageIdx = 0;
                foreach (var a in room.Assets)
                {
                    var damaged = damageIdx++ == 1; // 1 tài sản hư nhẹ
                    contract.Assets.Add(new ContractAsset
                    {
                        RoomAssetId = a.Id, Stage = AssetStage.CheckOut, Name = a.Name, Quantity = a.Quantity,
                        Condition = damaged ? AssetCondition.Damaged : AssetCondition.Good,
                        DeductionAmount = damaged ? 200000 : 0, Note = damaged ? "Trầy xước, cần sơn lại" : null,
                        ConfirmedByTenantAt = p.TerminatedAt.Value.AddHours(15)
                    });
                }
                var deposit = contract.DepositAmount;
                var damage = Math.Min(200000, deposit);
                var unpaid = Math.Min(unpaidTotal, deposit - damage);
                db.DepositSettlements.Add(new DepositSettlement
                {
                    ContractId = contract.Id, DepositAmount = deposit, DamageDeduction = damage, UnpaidDeduction = unpaid,
                    RefundAmount = deposit - damage - unpaid, Note = "Hoàn cọc qua chuyển khoản", SettledAt = p.TerminatedAt.Value.AddHours(16)
                });
                var rv = reviews[reviewIdx++ % reviews.Length];
                db.Reviews.Add(new Review { ContractId = contract.Id, Rating = rv.Rating, Comment = rv.Comment, CreatedAt = p.TerminatedAt.Value.AddDays(1) });
                room.Status = RoomStatus.Available;
            }
            else room.Status = RoomStatus.Occupied;

            await db.SaveChangesAsync();
        }

        // Vài yêu cầu đang chờ để demo luồng duyệt
        var pendingTenant = await userManager.FindByEmailAsync("nguoithue5@nestu.vn");
        var anBinh = houses.First(h => h.Name == "Nhà trọ An Bình");
        var room202 = await db.Rooms.FirstAsync(r => r.BoardingHouseId == anBinh.Id && r.RoomNumber == "202");
        var room302 = await db.Rooms.FirstAsync(r => r.BoardingHouseId == anBinh.Id && r.RoomNumber == "302");
        if (pendingTenant != null)
        {
            db.ViewingAppointments.Add(new ViewingAppointment
            {
                RoomId = room302.Id, TenantId = pendingTenant.Id, ScheduledAt = today.AddDays(2).AddHours(10),
                Status = AppointmentStatus.Pending, Note = "Muốn xem phòng cuối tuần", CreatedAt = today.AddDays(-1)
            });
            db.RentalRequests.Add(new RentalRequest
            {
                RoomId = room202.Id, TenantId = pendingTenant.Id, NumberOfOccupants = 1, MoveInDate = today.AddDays(7),
                DurationMonths = 6, Status = RentalRequestStatus.Pending, Note = "Em muốn dọn vào tuần sau ạ.", CreatedAt = today.AddHours(-5)
            });
        }
        await db.SaveChangesAsync();
    }
}
