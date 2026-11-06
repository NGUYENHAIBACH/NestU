using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Billing;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class BillingService : IBillingService
{
    private readonly NestUDbContext _db;
    private readonly IRentalService _rentals;
    private readonly IFileStorage _files;

    public BillingService(NestUDbContext db, IRentalService rentals, IFileStorage files)
    {
        _db = db;
        _rentals = rentals;
        _files = files;
    }

    // =====================================================================
    // Chỉ số điện nước
    // =====================================================================
    public async Task<List<UtilityReadingDto>> GetReadingsAsync(string landlordId, int? contractId, int? houseId)
    {
        var q = _db.UtilityReadings.AsNoTracking().Include(r => r.Contract).ThenInclude(c => c.Room).Include(r => r.Contract).ThenInclude(c => c.Tenant)
            .Where(r => r.Contract.Room.BoardingHouse.LandlordId == landlordId);
        if (contractId is not null) q = q.Where(r => r.ContractId == contractId);
        if (houseId is not null) q = q.Where(r => r.Contract.Room.BoardingHouseId == houseId);
        var rows = await q.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenBy(r => r.Contract.Room.RoomNumber).ToListAsync();

        var cids = rows.Select(r => r.ContractId).Distinct().ToList();
        var contracts = await _db.Contracts.AsNoTracking().Where(c => cids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new { c.InitialElectricIndex, c.InitialWaterIndex });
        var invoiced = await _db.Invoices.AsNoTracking().Where(i => cids.Contains(i.ContractId)).Select(i => new { i.ContractId, i.Year, i.Month }).ToListAsync();
        var invoicedSet = invoiced.Select(i => (i.ContractId, i.Year, i.Month)).ToHashSet();

        return rows.Select(r =>
        {
            var prev = rows.Where(x => x.ContractId == r.ContractId && (x.Year < r.Year || (x.Year == r.Year && x.Month < r.Month)))
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).FirstOrDefault();
            contracts.TryGetValue(r.ContractId, out var initC);
            return new UtilityReadingDto
            {
                Id = r.Id, ContractId = r.ContractId, RoomNumber = r.Contract.Room.RoomNumber, TenantName = r.Contract.Tenant.FullName,
                Month = r.Month, Year = r.Year, ElectricIndex = r.ElectricIndex, WaterIndex = r.WaterIndex,
                PreviousElectricIndex = prev?.ElectricIndex ?? initC?.InitialElectricIndex,
                PreviousWaterIndex = prev?.WaterIndex ?? initC?.InitialWaterIndex,
                RecordedAt = r.RecordedAt,
                HasInvoice = invoicedSet.Contains((r.ContractId, r.Year, r.Month))
            };
        }).ToList();
    }

    public async Task<UtilityReadingDto> AddReadingAsync(string landlordId, UtilityReadingDto dto)
    {
        var c = await _rentals.GetOwnedContractAsync(landlordId, Roles.Landlord, dto.ContractId);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc.");
        var readingPeriod = new DateTime(dto.Year, dto.Month, 1);
        if (readingPeriod < new DateTime(c.StartDate.Year, c.StartDate.Month, 1))
            throw ApiException.BadRequest($"Tháng ghi chỉ số ({dto.Month}/{dto.Year}) trước ngày bắt đầu hợp đồng ({c.StartDate:MM/yyyy}).");
        if (readingPeriod > new DateTime(c.EndDate.Year, c.EndDate.Month, 1))
            throw ApiException.BadRequest($"Tháng ghi chỉ số ({dto.Month}/{dto.Year}) vượt quá ngày kết thúc hợp đồng ({c.EndDate:MM/yyyy}).");
        if (await _db.UtilityReadings.AnyAsync(r => r.ContractId == dto.ContractId && r.Year == dto.Year && r.Month == dto.Month))
            throw ApiException.Conflict($"Đã ghi chỉ số tháng {dto.Month}/{dto.Year} cho hợp đồng này.");

        if (await _db.Invoices.AnyAsync(i => i.ContractId == dto.ContractId && (i.Year > dto.Year || (i.Year == dto.Year && i.Month > dto.Month))))
            throw new BusinessRuleException($"Không thể ghi chỉ số tháng {dto.Month}/{dto.Year} vì đã tồn tại hóa đơn ở các kỳ tương lai.");

        var prev = await _db.UtilityReadings.AsNoTracking().Where(r => r.ContractId == dto.ContractId && (r.Year < dto.Year || (r.Year == dto.Year && r.Month < dto.Month)))
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).FirstOrDefaultAsync();
        var baseElectric = prev?.ElectricIndex ?? c.InitialElectricIndex;
        var baseWater = prev?.WaterIndex ?? c.InitialWaterIndex;

        if (dto.ElectricIndex < baseElectric)
            throw new BusinessRuleException($"Chỉ số điện mới ({dto.ElectricIndex}) không được nhỏ hơn chỉ số kỳ liền trước ({baseElectric}).");
        if (dto.WaterIndex < baseWater)
            throw new BusinessRuleException($"Chỉ số nước mới ({dto.WaterIndex}) không được nhỏ hơn chỉ số kỳ liền trước ({baseWater}).");

        var next = await _db.UtilityReadings.AsNoTracking().Where(r => r.ContractId == dto.ContractId && (r.Year > dto.Year || (r.Year == dto.Year && r.Month > dto.Month)))
            .OrderBy(r => r.Year).ThenBy(r => r.Month).FirstOrDefaultAsync();
        if (next is not null)
        {
            if (dto.ElectricIndex > next.ElectricIndex)
                throw new BusinessRuleException($"Chỉ số điện mới ({dto.ElectricIndex}) không được lớn hơn chỉ số kỳ sau ({next.Month}/{next.Year}: {next.ElectricIndex}).");
            if (dto.WaterIndex > next.WaterIndex)
                throw new BusinessRuleException($"Chỉ số nước mới ({dto.WaterIndex}) không được lớn hơn chỉ số kỳ sau ({next.Month}/{next.Year}: {next.WaterIndex}).");
        }

        var r = new UtilityReading { ContractId = dto.ContractId, Month = dto.Month, Year = dto.Year, ElectricIndex = dto.ElectricIndex, WaterIndex = dto.WaterIndex };
        _db.UtilityReadings.Add(r);
        await _db.SaveChangesAsync();
        dto.Id = r.Id; dto.RecordedAt = r.RecordedAt; dto.PreviousElectricIndex = baseElectric; dto.PreviousWaterIndex = baseWater;
        dto.RoomNumber = c.Room.RoomNumber;
        return dto;
    }

    // =====================================================================
    // Hóa đơn (luồng 4)
    // =====================================================================
    public async Task<InvoiceDto> GenerateInvoiceAsync(string landlordId, GenerateInvoiceDto dto)
    {
        var c = await _rentals.GetOwnedContractAsync(landlordId, Roles.Landlord, dto.ContractId);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc.");
        if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.Today)
            throw ApiException.BadRequest("Hạn thanh toán không được ở quá khứ.");
        var invoicePeriod = new DateTime(dto.Year, dto.Month, 1);
        if (invoicePeriod < new DateTime(c.StartDate.Year, c.StartDate.Month, 1))
            throw ApiException.BadRequest($"Tháng hóa đơn ({dto.Month}/{dto.Year}) trước ngày bắt đầu hợp đồng ({c.StartDate:MM/yyyy}).");
        if (invoicePeriod > new DateTime(c.EndDate.Year, c.EndDate.Month, 1))
            throw ApiException.BadRequest($"Tháng hóa đơn ({dto.Month}/{dto.Year}) vượt quá ngày kết thúc hợp đồng ({c.EndDate:MM/yyyy}).");
        if (await _db.Invoices.AnyAsync(i => i.ContractId == dto.ContractId && i.Year == dto.Year && i.Month == dto.Month))
            throw ApiException.Conflict($"Hóa đơn tháng {dto.Month}/{dto.Year} đã được lập.");

        var reading = await _db.UtilityReadings.AsNoTracking().FirstOrDefaultAsync(r => r.ContractId == dto.ContractId && r.Year == dto.Year && r.Month == dto.Month)
            ?? throw ApiException.BadRequest($"Chưa ghi chỉ số điện nước tháng {dto.Month}/{dto.Year}.");
        var prev = await _db.UtilityReadings.AsNoTracking().Where(r => r.ContractId == dto.ContractId && (r.Year < dto.Year || (r.Year == dto.Year && r.Month < dto.Month)))
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).FirstOrDefaultAsync();

        var house = c.Room.BoardingHouse;
        var occupants = 1 + await _db.ContractMembers.CountAsync(m => m.ContractId == c.Id);
        var services = await _db.ContractServices.AsNoTracking().Include(s => s.Service).Where(s => s.ContractId == c.Id).Select(s => s.Service).ToListAsync();

        var baseElectric = prev?.ElectricIndex ?? c.InitialElectricIndex;
        var baseWater = prev?.WaterIndex ?? c.InitialWaterIndex;

        var kwh = reading.ElectricIndex - baseElectric;
        var m3 = reading.WaterIndex - baseWater;

        var details = new List<InvoiceDetail>
        {
            new() { ItemName = $"Tiền phòng tháng {dto.Month}/{dto.Year}", Quantity = 1, Unit = "tháng", UnitPrice = c.MonthlyRent, Amount = c.MonthlyRent },
            // Quy định 2: đơn giá tại thời điểm lập, đã bị chặn ≤ 4.000 ở nhà trọ
            new() { ItemName = $"Tiền điện ({baseElectric} → {reading.ElectricIndex})", Quantity = kwh, Unit = "kWh", UnitPrice = house.ElectricityPrice, Amount = kwh * house.ElectricityPrice }
        };
        if (house.WaterBillingType == WaterBillingType.PerM3)
            details.Add(new InvoiceDetail { ItemName = $"Tiền nước ({baseWater} → {reading.WaterIndex})", Quantity = m3, Unit = "m³", UnitPrice = house.WaterPrice, Amount = m3 * house.WaterPrice });
        else
            details.Add(new InvoiceDetail { ItemName = "Tiền nước (theo người)", Quantity = occupants, Unit = "người", UnitPrice = house.WaterPrice, Amount = occupants * house.WaterPrice });
        foreach (var s in services)
        {
            var qty = s.Unit == ServiceUnit.PerPerson ? occupants : 1;
            details.Add(new InvoiceDetail { ItemName = s.Name, Quantity = qty, Unit = s.Unit == ServiceUnit.PerPerson ? "người" : "tháng", UnitPrice = s.Price, Amount = qty * s.Price });
        }

        var defaultDueDate = new DateTime(dto.Year, dto.Month, 1).AddMonths(1).AddDays(9);
        var calculatedDueDate = dto.DueDate?.Date ?? (defaultDueDate < DateTime.Today ? DateTime.Today.AddDays(7) : defaultDueDate);

        var invoice = new Invoice
        {
            ContractId = c.Id, Month = dto.Month, Year = dto.Year, Total = details.Sum(d => d.Amount),
            DueDate = calculatedDueDate, Status = InvoiceStatus.Unpaid
        };
        foreach (var d in details) invoice.Details.Add(d);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return await LoadInvoiceAsync(invoice.Id);
    }

    private IQueryable<Invoice> InvoicesQuery() => _db.Invoices.AsNoTracking()
        .Include(i => i.Details).Include(i => i.Payments)
        .Include(i => i.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse)
        .Include(i => i.Contract).ThenInclude(c => c.Tenant);

    private static InvoiceDto Map(Invoice i) => new()
    {
        Id = i.Id, ContractId = i.ContractId, RoomNumber = i.Contract.Room.RoomNumber, HouseName = i.Contract.Room.BoardingHouse.Name,
        TenantName = i.Contract.Tenant.FullName, Month = i.Month, Year = i.Year, Total = i.Total, DueDate = i.DueDate,
        Status = i.Status.ToString(), DisplayStatus = i.DisplayStatus, RejectReason = i.RejectReason, CreatedAt = i.CreatedAt,
        Details = i.Details.Select(d => new InvoiceDetailDto { ItemName = d.ItemName, Quantity = d.Quantity, Unit = d.Unit, UnitPrice = d.UnitPrice, Amount = d.Amount }).ToList(),
        Payments = i.Payments.OrderByDescending(p => p.ReportedAt).Select(p => new PaymentDto { Id = p.Id, Amount = p.Amount, ProofImageUrl = p.ProofImageUrl, Note = p.Note, ReportedAt = p.ReportedAt, ConfirmedAt = p.ConfirmedAt }).ToList(),
        BankCode = i.Contract.Room.BoardingHouse.BankCode, BankAccount = i.Contract.Room.BoardingHouse.BankAccount, AccountName = i.Contract.Room.BoardingHouse.AccountName
    };

    private async Task<InvoiceDto> LoadInvoiceAsync(int id) => Map(await InvoicesQuery().FirstAsync(i => i.Id == id));

    public async Task<List<InvoiceDto>> GetLandlordInvoicesAsync(string landlordId, string? status, int? houseId)
    {
        var q = InvoicesQuery().Where(i => i.Contract.Room.BoardingHouse.LandlordId == landlordId);
        if (houseId is not null) q = q.Where(i => i.Contract.Room.BoardingHouseId == houseId);
        var rows = (await q.OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).ToListAsync()).Select(Map).ToList();
        if (!string.IsNullOrEmpty(status)) rows = rows.Where(r => r.DisplayStatus.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        return rows;
    }

    public async Task<List<InvoiceDto>> GetTenantInvoicesAsync(string tenantId)
        => (await InvoicesQuery().Where(i => i.Contract.TenantId == tenantId).OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).ToListAsync()).Select(Map).ToList();

    public async Task<InvoiceDto> GetInvoiceAsync(string userId, string role, int id)
    {
        var inv = await InvoicesQuery().FirstOrDefaultAsync(i => i.Id == id) ?? throw ApiException.NotFound("Không tìm thấy hóa đơn.");
        var owned = role switch
        {
            Roles.Tenant => inv.Contract.TenantId == userId,
            Roles.Landlord => inv.Contract.Room.BoardingHouse.LandlordId == userId,
            _ => role == Roles.Admin
        };
        if (!owned) throw ApiException.Forbidden();
        return Map(inv);
    }

    // =====================================================================
    // Thanh toán (luồng 5)
    // =====================================================================
    public async Task ReportPaymentAsync(string tenantId, int invoiceId, ReportPaymentDto dto)
    {
        var inv = await _db.Invoices.Include(i => i.Contract).FirstOrDefaultAsync(i => i.Id == invoiceId) ?? throw ApiException.NotFound("Không tìm thấy hóa đơn.");
        if (inv.Contract.TenantId != tenantId) throw ApiException.Forbidden();
        if (inv.Status == InvoiceStatus.Paid) throw ApiException.Conflict("Hóa đơn đã thanh toán.");
        if (inv.Status == InvoiceStatus.PendingConfirm) throw ApiException.Conflict("Bạn đã báo thanh toán, đang chờ chủ trọ xác nhận.");
        if (dto.Amount < inv.Total) throw ApiException.BadRequest($"Số tiền phải đủ {inv.Total:N0}đ.");

        string? url = null;
        if (dto.ProofImage is not null) url = await _files.SaveAsync(dto.ProofImage, "payments", new[] { ".jpg", ".jpeg", ".png", ".webp" });

        _db.Payments.Add(new Payment { InvoiceId = invoiceId, Amount = dto.Amount, ProofImageUrl = url, Note = dto.Note });
        inv.Status = InvoiceStatus.PendingConfirm; inv.RejectReason = null;
        await _db.SaveChangesAsync();
    }

    public async Task ConfirmPaymentAsync(string landlordId, int invoiceId)
    {
        var inv = await _db.Invoices.Include(i => i.Payments).Include(i => i.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse).FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw ApiException.NotFound("Không tìm thấy hóa đơn.");
        if (inv.Contract.Room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (inv.Status != InvoiceStatus.PendingConfirm) throw ApiException.Conflict("Hóa đơn chưa có báo thanh toán chờ xác nhận.");
        var last = inv.Payments.Where(p => p.ConfirmedAt == null).OrderByDescending(p => p.ReportedAt).FirstOrDefault();
        if (last is not null) last.ConfirmedAt = DateTime.UtcNow;
        inv.Status = InvoiceStatus.Paid;
        await _db.SaveChangesAsync();
    }

    public async Task RejectPaymentAsync(string landlordId, int invoiceId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw ApiException.BadRequest("Vui lòng ghi lý do từ chối thanh toán.");
        var inv = await _db.Invoices.Include(i => i.Payments).Include(i => i.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse).FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw ApiException.NotFound("Không tìm thấy hóa đơn.");
        if (inv.Contract.Room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (inv.Status != InvoiceStatus.PendingConfirm) throw ApiException.Conflict("Hóa đơn chưa có báo thanh toán chờ xác nhận.");
        var last = inv.Payments.Where(p => p.ConfirmedAt == null).OrderByDescending(p => p.ReportedAt).FirstOrDefault();
        if (last is not null) _db.Payments.Remove(last);
        inv.Status = InvoiceStatus.Unpaid; inv.RejectReason = reason.Trim();
        await _db.SaveChangesAsync();
    }

    /// <summary>VietQR (Nên có): dùng dịch vụ ảnh img.vietqr.io, không cần cổng thanh toán.</summary>
    public async Task<QrDto> GetQrAsync(string tenantId, int invoiceId)
    {
        var inv = await GetInvoiceAsync(tenantId, Roles.Tenant, invoiceId);
        if (string.IsNullOrEmpty(inv.BankCode) || string.IsNullOrEmpty(inv.BankAccount)) throw ApiException.BadRequest("Chủ trọ chưa cấu hình tài khoản nhận tiền.");
        var content = $"NESTU-{inv.Id}";
        var url = $"https://img.vietqr.io/image/{inv.BankCode}-{inv.BankAccount}-compact2.png?amount={(long)inv.Total}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(inv.AccountName ?? "")}";
        return new QrDto { Content = content, ImageUrl = url };
    }
}
