using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Rentals;
using NestU.API.Entities;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class RentalService : IRentalService
{
    private readonly NestUDbContext _db;
    public RentalService(NestUDbContext db) => _db = db;

    // =====================================================================
    // Hồ sơ Tenant
    // =====================================================================
    public async Task<TenantProfileDto> GetTenantProfileAsync(string tenantId)
    {
        var p = await _db.TenantProfiles.AsNoTracking().Include(x => x.User).Include(x => x.University)
            .FirstOrDefaultAsync(x => x.UserId == tenantId) ?? throw ApiException.NotFound("Chưa có hồ sơ người thuê.");
        return new TenantProfileDto
        {
            UserId = tenantId, Email = p.User.Email ?? "", FullName = p.FullName, CitizenId = p.CitizenId, Phone = p.Phone,
            IsStudent = p.IsStudent, StudentCode = p.StudentCode, UniversityId = p.UniversityId, UniversityName = p.University?.Name
        };
    }

    public async Task UpdateTenantProfileAsync(string tenantId, TenantProfileDto dto)
    {
        var p = await _db.TenantProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == tenantId) ?? throw ApiException.NotFound("Chưa có hồ sơ người thuê.");
        if (dto.IsStudent)
        {
            if (string.IsNullOrWhiteSpace(dto.StudentCode)) throw ApiException.BadRequest("Sinh viên phải nhập mã số sinh viên.");
            if (dto.UniversityId is null || !await _db.Universities.AnyAsync(u => u.Id == dto.UniversityId)) throw ApiException.BadRequest("Vui lòng chọn trường hợp lệ.");
        }
        p.FullName = dto.FullName.Trim(); p.CitizenId = dto.CitizenId; p.Phone = dto.Phone;
        p.IsStudent = dto.IsStudent; p.StudentCode = dto.IsStudent ? dto.StudentCode : null; p.UniversityId = dto.IsStudent ? dto.UniversityId : null;
        p.User.FullName = p.FullName; p.User.PhoneNumber = dto.Phone;
        await _db.SaveChangesAsync();
    }

    // =====================================================================
    // Lịch xem phòng
    // =====================================================================
    private static AppointmentDto MapAppointment(ViewingAppointment a) => new()
    {
        Id = a.Id, RoomId = a.RoomId, RoomNumber = a.Room.RoomNumber, HouseName = a.Room.BoardingHouse.Name, Address = a.Room.BoardingHouse.Address,
        TenantId = a.TenantId, TenantName = a.Tenant.FullName, TenantPhone = a.Tenant.PhoneNumber, ScheduledAt = a.ScheduledAt,
        Status = a.Status.ToString(), Note = a.Note, RejectReason = a.RejectReason, CreatedAt = a.CreatedAt
    };

    private IQueryable<ViewingAppointment> Appointments() => _db.ViewingAppointments.AsNoTracking()
        .Include(a => a.Room).ThenInclude(r => r.BoardingHouse).Include(a => a.Tenant);

    public async Task<List<AppointmentDto>> GetTenantAppointmentsAsync(string tenantId)
        => (await Appointments().Where(a => a.TenantId == tenantId).OrderByDescending(a => a.ScheduledAt).ToListAsync()).Select(MapAppointment).ToList();

    public async Task<AppointmentDto> CreateAppointmentAsync(string tenantId, CreateAppointmentDto dto)
    {
        var room = await _db.Rooms.AsNoTracking().Include(r => r.BoardingHouse).FirstOrDefaultAsync(r => r.Id == dto.RoomId) ?? throw ApiException.NotFound("Không tìm thấy phòng.");
        if (room.BoardingHouse.Status != HouseStatus.Active) throw ApiException.BadRequest("Nhà trọ chưa được duyệt PCCC, chưa nhận lịch xem.");
        if (room.Status != RoomStatus.Available) throw ApiException.BadRequest("Phòng hiện không còn trống.");
        if (dto.ScheduledAt < DateTime.Now.AddHours(1)) throw ApiException.BadRequest("Thời gian xem phải sau thời điểm hiện tại ít nhất 1 giờ.");
        if (await _db.ViewingAppointments.AnyAsync(a => a.TenantId == tenantId && a.RoomId == dto.RoomId && a.Status == AppointmentStatus.Pending))
            throw ApiException.Conflict("Bạn đã có lịch xem đang chờ cho phòng này.");

        var a = new ViewingAppointment { RoomId = dto.RoomId, TenantId = tenantId, ScheduledAt = dto.ScheduledAt, Note = dto.Note };
        _db.ViewingAppointments.Add(a);
        await _db.SaveChangesAsync();
        return MapAppointment(await Appointments().FirstAsync(x => x.Id == a.Id));
    }

    public async Task CancelAppointmentAsync(string tenantId, int id)
    {
        var a = await _db.ViewingAppointments.FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy lịch xem.");
        if (a.TenantId != tenantId) throw ApiException.Forbidden();
        if (a.Status != AppointmentStatus.Pending) throw ApiException.Conflict("Chỉ hủy được lịch đang chờ xác nhận.");
        a.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    public async Task<List<AppointmentDto>> GetLandlordAppointmentsAsync(string landlordId, string? status)
    {
        var q = Appointments().Where(a => a.Room.BoardingHouse.LandlordId == landlordId);
        if (Enum.TryParse<AppointmentStatus>(status, true, out var st)) q = q.Where(a => a.Status == st);
        return (await q.OrderBy(a => a.Status).ThenBy(a => a.ScheduledAt).ToListAsync()).Select(MapAppointment).ToList();
    }

    public async Task DecideAppointmentAsync(string landlordId, int id, bool confirm, string? reason)
    {
        var a = await _db.ViewingAppointments.Include(x => x.Room).ThenInclude(r => r.BoardingHouse).FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy lịch xem.");
        if (a.Room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (a.Status != AppointmentStatus.Pending) throw ApiException.Conflict("Lịch này đã được xử lý.");
        if (!confirm && string.IsNullOrWhiteSpace(reason)) throw ApiException.BadRequest("Vui lòng nhập lý do từ chối.");
        a.Status = confirm ? AppointmentStatus.Confirmed : AppointmentStatus.Rejected;
        a.RejectReason = confirm ? null : reason;
        await _db.SaveChangesAsync();
    }

    // =====================================================================
    // Yêu cầu thuê
    // =====================================================================
    private IQueryable<RentalRequest> Requests() => _db.RentalRequests.AsNoTracking()
        .Include(r => r.Room).ThenInclude(x => x.BoardingHouse).Include(r => r.Tenant).ThenInclude(t => t.TenantProfile);

    private async Task<RentalRequestDto> MapRequestAsync(RentalRequest r)
    {
        var contractId = await _db.Contracts.AsNoTracking().Where(c => c.RentalRequestId == r.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        return new RentalRequestDto
        {
            Id = r.Id, RoomId = r.RoomId, RoomNumber = r.Room.RoomNumber, HouseName = r.Room.BoardingHouse.Name, Price = r.Room.Price,
            DepositAmount = r.Room.DepositAmount, Capacity = r.Room.Capacity, TenantId = r.TenantId, TenantName = r.Tenant.FullName,
            TenantPhone = r.Tenant.PhoneNumber, TenantIsStudent = r.Tenant.TenantProfile?.IsStudent ?? false,
            NumberOfOccupants = r.NumberOfOccupants, MoveInDate = r.MoveInDate, DurationMonths = r.DurationMonths,
            Status = r.Status.ToString(), Note = r.Note, RejectReason = r.RejectReason, CreatedAt = r.CreatedAt, ContractId = contractId
        };
    }

    private async Task<List<RentalRequestDto>> MapRequestsAsync(List<RentalRequest> rows)
    {
        var ids = rows.Select(r => r.Id).ToList();
        var contracts = await _db.Contracts.AsNoTracking().Where(c => c.RentalRequestId != null && ids.Contains(c.RentalRequestId.Value))
            .Select(c => new { c.RentalRequestId, c.Id }).ToDictionaryAsync(x => x.RentalRequestId!.Value, x => x.Id);
        return rows.Select(r => new RentalRequestDto
        {
            Id = r.Id, RoomId = r.RoomId, RoomNumber = r.Room.RoomNumber, HouseName = r.Room.BoardingHouse.Name, Price = r.Room.Price,
            DepositAmount = r.Room.DepositAmount, Capacity = r.Room.Capacity, TenantId = r.TenantId, TenantName = r.Tenant.FullName,
            TenantPhone = r.Tenant.PhoneNumber, TenantIsStudent = r.Tenant.TenantProfile?.IsStudent ?? false,
            NumberOfOccupants = r.NumberOfOccupants, MoveInDate = r.MoveInDate, DurationMonths = r.DurationMonths,
            Status = r.Status.ToString(), Note = r.Note, RejectReason = r.RejectReason, CreatedAt = r.CreatedAt,
            ContractId = contracts.TryGetValue(r.Id, out var cid) ? cid : null
        }).ToList();
    }

    public async Task<List<RentalRequestDto>> GetTenantRequestsAsync(string tenantId)
        => await MapRequestsAsync(await Requests().Where(r => r.TenantId == tenantId).OrderByDescending(r => r.CreatedAt).ToListAsync());

    public async Task<RentalRequestDto> CreateRequestAsync(string tenantId, CreateRentalRequestDto dto)
    {
        var room = await _db.Rooms.AsNoTracking().Include(r => r.BoardingHouse).FirstOrDefaultAsync(r => r.Id == dto.RoomId) ?? throw ApiException.NotFound("Không tìm thấy phòng.");
        var profile = await _db.TenantProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == tenantId) ?? throw ApiException.BadRequest("Vui lòng hoàn thiện hồ sơ người thuê trước.");

        // Quy định 1: chỉ nhà trọ Active (đã duyệt PCCC)
        if (room.BoardingHouse.Status != HouseStatus.Active) throw ApiException.BadRequest("Nhà trọ chưa được duyệt PCCC, chưa nhận yêu cầu thuê.");
        if (room.Status != RoomStatus.Available) throw ApiException.BadRequest("Phòng hiện không còn trống.");
        if (dto.NumberOfOccupants > room.Capacity) throw ApiException.BadRequest($"Phòng chỉ chứa tối đa {room.Capacity} người.");
        if (room.BoardingHouse.AllowedTenantType == AllowedTenantType.StudentOnly && !profile.IsStudent) throw ApiException.BadRequest("Nhà trọ này chỉ nhận sinh viên.");
        if (dto.MoveInDate.Date < DateTime.Today) throw ApiException.BadRequest("Ngày dọn vào không được ở quá khứ.");
        if (await _db.Contracts.AnyAsync(c => c.TenantId == tenantId && c.Status == ContractStatus.Active)) throw ApiException.Conflict("Bạn đang có hợp đồng hiệu lực.");
        if (await _db.RentalRequests.AnyAsync(r => r.TenantId == tenantId && r.RoomId == dto.RoomId && r.Status == RentalRequestStatus.Pending))
            throw ApiException.Conflict("Bạn đã gửi yêu cầu thuê phòng này rồi.");

        var req = new RentalRequest
        {
            RoomId = dto.RoomId, TenantId = tenantId, NumberOfOccupants = dto.NumberOfOccupants, MoveInDate = dto.MoveInDate.Date,
            DurationMonths = dto.DurationMonths, Note = dto.Note
        };
        _db.RentalRequests.Add(req);
        await _db.SaveChangesAsync();
        return await MapRequestAsync(await Requests().FirstAsync(r => r.Id == req.Id));
    }

    public async Task CancelRequestAsync(string tenantId, int id)
    {
        var r = await _db.RentalRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy yêu cầu.");
        if (r.TenantId != tenantId) throw ApiException.Forbidden();
        if (r.Status != RentalRequestStatus.Pending) throw ApiException.Conflict("Chỉ hủy được yêu cầu đang chờ.");
        r.Status = RentalRequestStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    public async Task<List<RentalRequestDto>> GetLandlordRequestsAsync(string landlordId, string? status)
    {
        var q = Requests().Where(r => r.Room.BoardingHouse.LandlordId == landlordId);
        if (Enum.TryParse<RentalRequestStatus>(status, true, out var st)) q = q.Where(r => r.Status == st);
        return await MapRequestsAsync(await q.OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAt).ToListAsync());
    }

    /// <summary>Luồng 3: duyệt thuê → hợp đồng + biên bản CheckIn + phòng Occupied, trong một transaction.</summary>
    public async Task<ContractDto> ApproveRequestAsync(string landlordId, int id, ApproveRentalDto dto)
    {
        var req = await _db.RentalRequests.Include(r => r.Room).ThenInclude(x => x.BoardingHouse).Include(r => r.Tenant).ThenInclude(t => t.TenantProfile)
            .FirstOrDefaultAsync(r => r.Id == id) ?? throw ApiException.NotFound("Không tìm thấy yêu cầu.");
        var room = req.Room; var house = room.BoardingHouse;
        if (house.LandlordId != landlordId) throw ApiException.Forbidden();
        if (req.Status != RentalRequestStatus.Pending) throw ApiException.Conflict("Yêu cầu này đã được xử lý.");
        if (house.Status != HouseStatus.Active) throw ApiException.BadRequest("Nhà trọ chưa được duyệt PCCC.");
        if (room.Status != RoomStatus.Available) throw ApiException.Conflict("Phòng đã có người thuê.");
        if (req.NumberOfOccupants > room.Capacity) throw ApiException.BadRequest($"Số người vượt sức chứa ({room.Capacity}).");
        if (house.AllowedTenantType == AllowedTenantType.StudentOnly && !(req.Tenant.TenantProfile?.IsStudent ?? false)) throw ApiException.BadRequest("Nhà trọ chỉ nhận sinh viên.");
        if (await _db.Contracts.AnyAsync(c => c.TenantId == req.TenantId && c.Status == ContractStatus.Active)) throw ApiException.Conflict("Người thuê đang có hợp đồng hiệu lực khác.");

        var start = (dto.StartDate ?? req.MoveInDate).Date;
        var months = dto.DurationMonths ?? req.DurationMonths;
        var end = start.AddMonths(months);
        if (end <= start) throw ApiException.BadRequest("Ngày kết thúc phải sau ngày bắt đầu.");

        var serviceIds = dto.ServiceIds.Distinct().ToList();
        if (serviceIds.Count > 0)
        {
            var ok = await _db.Services.CountAsync(s => serviceIds.Contains(s.Id) && s.BoardingHouseId == house.Id && s.IsActive);
            if (ok != serviceIds.Count) throw ApiException.BadRequest("Có dịch vụ không thuộc nhà trọ này.");
        }
        if (dto.Members.Count > room.Capacity - 1) throw ApiException.BadRequest("Số người ở cùng vượt sức chứa phòng.");

        var assets = await _db.RoomAssets.AsNoTracking().Where(a => a.RoomId == room.Id).ToListAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        var contract = new Contract
        {
            RoomId = room.Id, TenantId = req.TenantId, RentalRequestId = req.Id, StartDate = start, EndDate = end,
            MonthlyRent = room.Price, DepositAmount = dto.DepositAmount ?? room.DepositAmount,
            DepositPaidAt = dto.DepositPaid ? DateTime.UtcNow : null, Status = ContractStatus.Active,
            InitialElectricIndex = dto.InitialElectricIndex, InitialWaterIndex = dto.InitialWaterIndex
        };
        foreach (var m in dto.Members) contract.Members.Add(new ContractMember { FullName = m.FullName.Trim(), CitizenId = m.CitizenId, Phone = m.Phone });
        foreach (var sid in serviceIds) contract.Services.Add(new ContractService { ServiceId = sid });
        // Biên bản bàn giao nhận phòng = ảnh chụp tài sản phòng lúc này
        foreach (var a in assets)
            contract.Assets.Add(new ContractAsset { RoomAssetId = a.Id, Stage = AssetStage.CheckIn, Name = a.Name, Quantity = a.Quantity, Condition = a.Condition });
        _db.Contracts.Add(contract);

        room.Status = RoomStatus.Occupied;           // RowVersion → 409 nếu hai người duyệt cùng lúc
        req.Status = RentalRequestStatus.Approved;

        var others = await _db.RentalRequests.Where(r => r.RoomId == room.Id && r.Id != req.Id && r.Status == RentalRequestStatus.Pending).ToListAsync();
        foreach (var o in others) { o.Status = RentalRequestStatus.Rejected; o.RejectReason = "Phòng đã được cho người khác thuê."; }
        var appts = await _db.ViewingAppointments.Where(a => a.RoomId == room.Id && a.Status == AppointmentStatus.Pending).ToListAsync();
        foreach (var a in appts) { a.Status = AppointmentStatus.Rejected; a.RejectReason = "Phòng đã có người thuê."; }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return await LoadContractAsync(contract.Id);
    }

    public async Task RejectRequestAsync(string landlordId, int id, string reason)
    {
        var r = await _db.RentalRequests.Include(x => x.Room).ThenInclude(x => x.BoardingHouse).FirstOrDefaultAsync(x => x.Id == id) ?? throw ApiException.NotFound("Không tìm thấy yêu cầu.");
        if (r.Room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (r.Status != RentalRequestStatus.Pending) throw ApiException.Conflict("Yêu cầu này đã được xử lý.");
        r.Status = RentalRequestStatus.Rejected; r.RejectReason = reason;
        await _db.SaveChangesAsync();
    }

    // =====================================================================
    // Hợp đồng
    // =====================================================================
    public async Task<Contract> GetOwnedContractAsync(string userId, string role, int contractId, bool track = false)
    {
        IQueryable<Contract> q = _db.Contracts.Include(c => c.Room).ThenInclude(r => r.BoardingHouse);
        if (!track) q = q.AsNoTracking();
        var c = await q.FirstOrDefaultAsync(x => x.Id == contractId) ?? throw ApiException.NotFound("Không tìm thấy hợp đồng.");
        var owned = role switch
        {
            Roles.Tenant => c.TenantId == userId,
            Roles.Landlord => c.Room.BoardingHouse.LandlordId == userId,
            Roles.Admin => true,
            _ => false
        };
        if (!owned) throw ApiException.Forbidden("Hợp đồng này không thuộc về bạn.");
        return c;
    }

    private IQueryable<Contract> ContractsQuery() => _db.Contracts.AsNoTracking()
        .Include(c => c.Room).ThenInclude(r => r.BoardingHouse).ThenInclude(h => h.Landlord)
        .Include(c => c.Tenant).Include(c => c.Members).Include(c => c.Services).ThenInclude(s => s.Service)
        .Include(c => c.DepositSettlement);

    private async Task<List<ContractDto>> MapContractsAsync(List<Contract> rows)
    {
        var ids = rows.Select(c => c.Id).ToList();
        // g.All(...) không phải aggregate SQL nên không translate được trong GroupBy → lấy dòng về rồi gom trong bộ nhớ
        var checkInRows = await _db.ContractAssets.AsNoTracking().Where(a => ids.Contains(a.ContractId) && a.Stage == AssetStage.CheckIn)
            .Select(a => new { a.ContractId, Confirmed = a.ConfirmedByTenantAt != null }).ToListAsync();
        var checkIn = checkInRows.GroupBy(a => a.ContractId).Select(g => new { g.Key, Confirmed = g.All(a => a.Confirmed) }).ToList();
        var checkout = await _db.ContractAssets.AsNoTracking().Where(a => ids.Contains(a.ContractId) && a.Stage == AssetStage.CheckOut).Select(a => a.ContractId).Distinct().ToListAsync();
        var unpaid = await _db.Invoices.AsNoTracking().Where(i => ids.Contains(i.ContractId) && i.Status != InvoiceStatus.Paid)
            .GroupBy(i => i.ContractId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);

        return rows.Select(c => new ContractDto
        {
            Id = c.Id, RoomId = c.RoomId, RoomNumber = c.Room.RoomNumber, Floor = c.Room.Floor, HouseId = c.Room.BoardingHouseId,
            HouseName = c.Room.BoardingHouse.Name, Address = c.Room.BoardingHouse.Address, TenantId = c.TenantId, TenantName = c.Tenant.FullName,
            TenantPhone = c.Tenant.PhoneNumber, LandlordName = c.Room.BoardingHouse.Landlord.FullName, LandlordPhone = c.Room.BoardingHouse.Landlord.PhoneNumber,
            StartDate = c.StartDate, EndDate = c.EndDate, MonthlyRent = c.MonthlyRent, DepositAmount = c.DepositAmount, DepositPaidAt = c.DepositPaidAt,
            InitialElectricIndex = c.InitialElectricIndex, InitialWaterIndex = c.InitialWaterIndex,
            Status = c.Status.ToString(),
            DisplayStatus = c.Status == ContractStatus.Active && c.EndDate.Date < DateTime.Today ? "Expired" : c.Status.ToString(),
            TerminatedAt = c.TerminatedAt, CreatedAt = c.CreatedAt,
            Members = c.Members.Select(m => new ContractMemberDto { FullName = m.FullName, CitizenId = m.CitizenId, Phone = m.Phone }).ToList(),
            Services = c.Services.Select(s => new ServiceDto { Id = s.ServiceId, BoardingHouseId = s.Service.BoardingHouseId, Name = s.Service.Name, Price = s.Service.Price, Unit = s.Service.Unit.ToString(), IsActive = s.Service.IsActive }).ToList(),
            CheckInConfirmed = checkIn.FirstOrDefault(x => x.Key == c.Id)?.Confirmed ?? true,
            HasCheckout = checkout.Contains(c.Id),
            DepositSettlement = c.DepositSettlement is null ? null : new DepositSettlementDto
            {
                ContractId = c.Id, DepositAmount = c.DepositSettlement.DepositAmount, DamageDeduction = c.DepositSettlement.DamageDeduction,
                UnpaidDeduction = c.DepositSettlement.UnpaidDeduction, RefundAmount = c.DepositSettlement.RefundAmount, Note = c.DepositSettlement.Note, SettledAt = c.DepositSettlement.SettledAt
            },
            UnpaidInvoices = unpaid.GetValueOrDefault(c.Id)
        }).ToList();
    }

    private async Task<ContractDto> LoadContractAsync(int id) => (await MapContractsAsync(await ContractsQuery().Where(c => c.Id == id).ToListAsync())).First();

    public async Task<List<ContractDto>> GetTenantContractsAsync(string tenantId)
        => await MapContractsAsync(await ContractsQuery().Where(c => c.TenantId == tenantId).OrderByDescending(c => c.StartDate).ToListAsync());

    public async Task<ContractDto> GetContractAsync(string userId, string role, int id)
    {
        await GetOwnedContractAsync(userId, role, id);
        return await LoadContractAsync(id);
    }

    public async Task<List<ContractDto>> GetLandlordContractsAsync(string landlordId, string? status, int? houseId)
    {
        var q = ContractsQuery().Where(c => c.Room.BoardingHouse.LandlordId == landlordId);
        if (houseId is not null) q = q.Where(c => c.Room.BoardingHouseId == houseId);
        if (Enum.TryParse<ContractStatus>(status, true, out var st)) q = q.Where(c => c.Status == st);
        return await MapContractsAsync(await q.OrderBy(c => c.Status).ThenByDescending(c => c.StartDate).ToListAsync());
    }

    public async Task ExtendContractAsync(string landlordId, int id, ExtendContractDto dto)
    {
        var c = await GetOwnedContractAsync(landlordId, Roles.Landlord, id, track: true);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc.");
        if (dto.NewEndDate.Date <= c.EndDate.Date) throw ApiException.BadRequest("Ngày kết thúc mới phải sau ngày kết thúc hiện tại.");
        c.EndDate = dto.NewEndDate.Date;
        await _db.SaveChangesAsync();
    }

    // =====================================================================
    // Bàn giao tài sản
    // =====================================================================
    public async Task<List<ContractAssetDto>> GetContractAssetsAsync(string userId, string role, int contractId, string? stage)
    {
        await GetOwnedContractAsync(userId, role, contractId);
        var q = _db.ContractAssets.AsNoTracking().Include(a => a.RoomAsset).ThenInclude(r => r.AssetType).Where(a => a.ContractId == contractId);
        if (Enum.TryParse<AssetStage>(stage, true, out var st)) q = q.Where(a => a.Stage == st);
        var rows = await q.OrderBy(a => a.Stage).ThenBy(a => a.Name).ToListAsync();
        return rows.Select(a => new ContractAssetDto
        {
            Id = a.Id, ContractId = a.ContractId, RoomAssetId = a.RoomAssetId, Stage = a.Stage.ToString(), Name = a.Name,
            AssetTypeName = a.RoomAsset.AssetType.Name, Quantity = a.Quantity, Condition = a.Condition.ToString(),
            DeductionAmount = a.DeductionAmount, Note = a.Note, ConfirmedByTenantAt = a.ConfirmedByTenantAt
        }).ToList();
    }

    public async Task ConfirmCheckInAsync(string tenantId, int contractId)
    {
        await GetOwnedContractAsync(tenantId, Roles.Tenant, contractId);
        var items = await _db.ContractAssets.Where(a => a.ContractId == contractId && a.Stage == AssetStage.CheckIn).ToListAsync();
        if (items.Count == 0) throw ApiException.NotFound("Hợp đồng không có biên bản bàn giao.");
        if (items.All(a => a.ConfirmedByTenantAt != null)) throw ApiException.Conflict("Biên bản đã được xác nhận trước đó.");
        var now = DateTime.UtcNow;
        foreach (var a in items) a.ConfirmedByTenantAt ??= now;
        await _db.SaveChangesAsync();
    }

    public async Task CheckoutAssetsAsync(string landlordId, int contractId, CheckoutAssetsDto dto)
    {
        var c = await GetOwnedContractAsync(landlordId, Roles.Landlord, contractId);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc, không ghi biên bản được nữa.");
        if (await _db.DepositSettlements.AnyAsync(s => s.ContractId == contractId)) throw ApiException.Conflict("Hợp đồng đã quyết toán cọc.");

        var checkIn = await _db.ContractAssets.AsNoTracking().Where(a => a.ContractId == contractId && a.Stage == AssetStage.CheckIn).ToListAsync();
        var validIds = checkIn.Select(a => a.RoomAssetId).ToHashSet();
        if (dto.Items.Any(i => !validIds.Contains(i.RoomAssetId))) throw ApiException.BadRequest("Có tài sản không nằm trong biên bản nhận phòng.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var existing = await _db.ContractAssets.Where(a => a.ContractId == contractId && a.Stage == AssetStage.CheckOut).ToListAsync();
        _db.ContractAssets.RemoveRange(existing);   // cho phép ghi lại biên bản trả phòng trước khi quyết toán
        foreach (var i in dto.Items)
        {
            var baseline = checkIn.First(a => a.RoomAssetId == i.RoomAssetId);
            _db.ContractAssets.Add(new ContractAsset
            {
                ContractId = contractId, RoomAssetId = i.RoomAssetId, Stage = AssetStage.CheckOut, Name = baseline.Name, Quantity = i.Quantity,
                Condition = Enum.Parse<AssetCondition>(i.Condition), DeductionAmount = i.DeductionAmount, Note = i.Note
            });
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // =====================================================================
    // Gia hạn / trả phòng
    // =====================================================================
    private IQueryable<ContractChangeRequest> ChangeRequests() => _db.ContractChangeRequests.AsNoTracking()
        .Include(x => x.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse).Include(x => x.Contract).ThenInclude(c => c.Tenant);

    private static ContractChangeRequestDto MapChange(ContractChangeRequest x) => new()
    {
        Id = x.Id, ContractId = x.ContractId, RoomNumber = x.Contract.Room.RoomNumber, HouseName = x.Contract.Room.BoardingHouse.Name,
        TenantName = x.Contract.Tenant.FullName, Type = x.Type.ToString(), NewEndDate = x.NewEndDate, Reason = x.Reason,
        Status = x.Status.ToString(), DecisionNote = x.DecisionNote, CreatedAt = x.CreatedAt, DecidedAt = x.DecidedAt
    };

    public async Task<ContractChangeRequestDto> CreateChangeRequestAsync(string tenantId, int contractId, string type, CreateContractChangeDto dto)
    {
        var c = await GetOwnedContractAsync(tenantId, Roles.Tenant, contractId);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc.");
        var t = Enum.Parse<ChangeRequestType>(type, true);
        if (t == ChangeRequestType.Renew)
        {
            if (dto.NewEndDate is null) throw ApiException.BadRequest("Vui lòng chọn ngày kết thúc mới.");
            if (dto.NewEndDate.Value.Date <= c.EndDate.Date) throw ApiException.BadRequest("Ngày kết thúc mới phải sau ngày kết thúc hiện tại.");
        }
        if (await _db.ContractChangeRequests.AnyAsync(x => x.ContractId == contractId && x.Status == ChangeRequestStatus.Pending))
            throw ApiException.Conflict("Đang có yêu cầu chờ xử lý cho hợp đồng này.");

        var req = new ContractChangeRequest { ContractId = contractId, Type = t, NewEndDate = t == ChangeRequestType.Renew ? dto.NewEndDate!.Value.Date : null, Reason = dto.Reason };
        _db.ContractChangeRequests.Add(req);
        await _db.SaveChangesAsync();
        return MapChange(await ChangeRequests().FirstAsync(x => x.Id == req.Id));
    }

    public async Task<List<ContractChangeRequestDto>> GetChangeRequestsAsync(string userId, string role, string? status)
    {
        var q = role == Roles.Tenant ? ChangeRequests().Where(x => x.Contract.TenantId == userId) : ChangeRequests().Where(x => x.Contract.Room.BoardingHouse.LandlordId == userId);
        if (Enum.TryParse<ChangeRequestStatus>(status, true, out var st)) q = q.Where(x => x.Status == st);
        return (await q.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt).ToListAsync()).Select(MapChange).ToList();
    }

    public async Task DecideChangeRequestAsync(string landlordId, int id, DecisionDto dto)
    {
        var x = await _db.ContractChangeRequests.Include(r => r.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.BoardingHouse).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw ApiException.NotFound("Không tìm thấy yêu cầu.");
        if (x.Contract.Room.BoardingHouse.LandlordId != landlordId) throw ApiException.Forbidden();
        if (x.Status != ChangeRequestStatus.Pending) throw ApiException.Conflict("Yêu cầu đã được xử lý.");
        if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Note)) throw ApiException.BadRequest("Vui lòng ghi lý do từ chối.");

        x.Status = dto.Approve ? ChangeRequestStatus.Approved : ChangeRequestStatus.Rejected;
        x.DecisionNote = dto.Note; x.DecidedAt = DateTime.UtcNow;
        if (dto.Approve && x.Type == ChangeRequestType.Renew && x.NewEndDate is not null && x.Contract.Status == ContractStatus.Active)
            x.Contract.EndDate = x.NewEndDate.Value;
        // Terminate được duyệt: Landlord tiếp tục bước biên bản trả phòng → quyết toán cọc (luồng 6)
        await _db.SaveChangesAsync();
    }

    // =====================================================================
    // Quyết toán cọc + chấm dứt hợp đồng (luồng 6)
    // =====================================================================
    public async Task<DepositSettlementDto> SettleDepositAsync(string landlordId, int contractId, CreateDepositSettlementDto dto)
    {
        var c = await GetOwnedContractAsync(landlordId, Roles.Landlord, contractId, track: true);
        if (c.Status != ContractStatus.Active) throw ApiException.Conflict("Hợp đồng đã kết thúc.");
        if (await _db.DepositSettlements.AnyAsync(s => s.ContractId == contractId)) throw ApiException.Conflict("Hợp đồng đã quyết toán cọc.");

        var checkout = await _db.ContractAssets.AsNoTracking().Where(a => a.ContractId == contractId && a.Stage == AssetStage.CheckOut).ToListAsync();
        var damage = checkout.Sum(a => a.DeductionAmount);
        var unpaidInvoices = dto.DeductUnpaidInvoices
            ? await _db.Invoices.Where(i => i.ContractId == contractId && i.Status != InvoiceStatus.Paid).ToListAsync()
            : new List<Invoice>();
        var unpaid = unpaidInvoices.Sum(i => i.Total);

        var totalDeduct = damage + unpaid;
        if (totalDeduct > c.DepositAmount)
            throw ApiException.BadRequest($"Tổng khấu trừ ({totalDeduct:N0}đ) vượt tiền cọc ({c.DepositAmount:N0}đ). Hãy giảm khấu trừ hoặc thu thêm ngoài hệ thống.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var settlement = new DepositSettlement
        {
            ContractId = contractId, DepositAmount = c.DepositAmount, DamageDeduction = damage, UnpaidDeduction = unpaid,
            RefundAmount = c.DepositAmount - totalDeduct, Note = dto.Note
        };
        _db.DepositSettlements.Add(settlement);

        // Hóa đơn chưa trả được trừ vào cọc → coi như đã thanh toán
        foreach (var inv in unpaidInvoices)
        {
            inv.Status = InvoiceStatus.Paid;
            _db.Payments.Add(new Payment { InvoiceId = inv.Id, Amount = inv.Total, Note = "Trừ vào tiền cọc khi trả phòng", ConfirmedAt = DateTime.UtcNow });
        }

        c.Status = ContractStatus.Terminated; c.TerminatedAt = DateTime.UtcNow;
        c.Room.Status = RoomStatus.Available;

        // Cập nhật tình trạng tài sản phòng theo biên bản trả phòng
        if (checkout.Count > 0)
        {
            var assetIds = checkout.Select(a => a.RoomAssetId).ToList();
            var roomAssets = await _db.RoomAssets.Where(a => assetIds.Contains(a.Id)).ToListAsync();
            foreach (var ra in roomAssets)
            {
                var co = checkout.First(a => a.RoomAssetId == ra.Id);
                ra.Condition = co.Condition;
                if (co.Quantity >= 1) ra.Quantity = co.Quantity;
            }
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new DepositSettlementDto
        {
            ContractId = contractId, DepositAmount = settlement.DepositAmount, DamageDeduction = settlement.DamageDeduction,
            UnpaidDeduction = settlement.UnpaidDeduction, RefundAmount = settlement.RefundAmount, Note = settlement.Note, SettledAt = settlement.SettledAt
        };
    }
}
