using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Stats;
using NestU.API.Entities.Enums;
using NestU.API.Helpers;
using NestU.API.Services.Interfaces;

namespace NestU.API.Services;

public class StatsService : IStatsService
{
    private readonly NestUDbContext _db;
    public StatsService(NestUDbContext db) => _db = db;

    public async Task<LandlordStatsDto> GetLandlordStatsAsync(string landlordId, int? year = null, int? houseId = null)
    {
        var targetYear = year ?? DateTime.Today.Year;

        // Kiểm tra quyền sở hữu nhà trọ nếu được truyền vào
        string? selectedHouseName = null;
        if (houseId.HasValue)
        {
            var house = await _db.BoardingHouses.FirstOrDefaultAsync(h => h.Id == houseId.Value && h.LandlordId == landlordId)
                        ?? throw ApiException.NotFound("Không tìm thấy nhà trọ hoặc bạn không có quyền.");
            selectedHouseName = house.Name;
        }

        // Lấy danh sách ID các nhà trọ thuộc chủ
        var houseIdsQuery = _db.BoardingHouses.Where(h => h.LandlordId == landlordId);
        if (houseId.HasValue) houseIdsQuery = houseIdsQuery.Where(h => h.Id == houseId.Value);
        var targetHouseIds = await houseIdsQuery.Select(h => h.Id).ToListAsync();

        if (targetHouseIds.Count == 0)
        {
            return new LandlordStatsDto
            {
                Year = targetYear,
                SelectedHouseId = houseId,
                SelectedHouseName = selectedHouseName
            };
        }

        // 1. Doanh thu 12 tháng theo hóa đơn Paid
        var paidInvoices = await _db.Invoices
            .Include(i => i.Details)
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Where(i => targetHouseIds.Contains(i.Contract.Room.BoardingHouseId)
                        && i.Year == targetYear
                        && i.Status == InvoiceStatus.Paid)
            .ToListAsync();

        var monthlyRevenues = new List<MonthlyRevenueDto>();
        for (var m = 1; m <= 12; m++)
        {
            var monthInvs = paidInvoices.Where(i => i.Month == m).ToList();
            var total = monthInvs.Sum(i => i.Total);

            decimal rent = 0;
            decimal electric = 0;
            decimal water = 0;
            decimal service = 0;

            foreach (var inv in monthInvs)
            {
                foreach (var d in inv.Details)
                {
                    var name = d.ItemName.ToLowerInvariant();
                    if (name.Contains("phòng")) rent += d.Amount;
                    else if (name.Contains("điện")) electric += d.Amount;
                    else if (name.Contains("nước")) water += d.Amount;
                    else service += d.Amount;
                }
            }

            // Nếu hóa đơn không có chi tiết cụ thể, tiền phòng = total
            if (total > 0 && rent == 0 && electric == 0 && water == 0 && service == 0)
            {
                rent = total;
            }

            monthlyRevenues.Add(new MonthlyRevenueDto
            {
                Month = m,
                Year = targetYear,
                TotalRevenue = total,
                RentRevenue = rent,
                ElectricRevenue = electric,
                WaterRevenue = water,
                ServiceRevenue = service,
                PaidInvoiceCount = monthInvs.Count
            });
        }

        var totalRevYear = monthlyRevenues.Sum(x => x.TotalRevenue);

        // Doanh thu tháng này và tháng trước (tính theo năm hiện tại thực tế)
        var now = DateTime.Today;
        var thisMonthInvs = await _db.Invoices
            .Where(i => targetHouseIds.Contains(i.Contract.Room.BoardingHouseId)
                        && i.Year == now.Year && i.Month == now.Month
                        && i.Status == InvoiceStatus.Paid)
            .SumAsync(i => (decimal?)i.Total) ?? 0;

        var lastMonthDate = now.AddMonths(-1);
        var lastMonthInvs = await _db.Invoices
            .Where(i => targetHouseIds.Contains(i.Contract.Room.BoardingHouseId)
                        && i.Year == lastMonthDate.Year && i.Month == lastMonthDate.Month
                        && i.Status == InvoiceStatus.Paid)
            .SumAsync(i => (decimal?)i.Total) ?? 0;

        double? momGrowth = null;
        if (lastMonthInvs > 0)
        {
            momGrowth = Math.Round((double)((thisMonthInvs - lastMonthInvs) / lastMonthInvs) * 100, 1);
        }

        // Hóa đơn chưa thanh toán / nợ
        var unpaidInvs = await _db.Invoices
            .Include(i => i.Payments)
            .Where(i => targetHouseIds.Contains(i.Contract.Room.BoardingHouseId)
                        && i.Status != InvoiceStatus.Paid)
            .ToListAsync();
        var unpaidTotal = unpaidInvs.Sum(i => i.Total - i.Payments.Where(p => p.ConfirmedAt != null).Sum(p => p.Amount));
        var unpaidCount = unpaidInvs.Count;

        // 2. Thống kê lấp đầy
        var (overallOcc, housesOcc) = await CalculateOccupancyAsync(landlordId, houseId);

        // 3. Dự phóng doanh thu tháng tới
        var projectedRev = await CalculateProjectedRevenueAsync(targetHouseIds);

        return new LandlordStatsDto
        {
            Year = targetYear,
            SelectedHouseId = houseId,
            SelectedHouseName = selectedHouseName,
            TotalRevenueThisYear = totalRevYear,
            TotalRevenueThisMonth = thisMonthInvs,
            TotalRevenueLastMonth = lastMonthInvs,
            MonthOverMonthGrowthPercent = momGrowth,
            UnpaidInvoicesTotal = unpaidTotal,
            UnpaidInvoicesCount = unpaidCount,
            MonthlyRevenues = monthlyRevenues,
            OverallOccupancy = overallOcc,
            HousesOccupancy = housesOcc,
            ProjectedRevenue = projectedRev
        };
    }

    public async Task<HouseOccupancyDto> GetLandlordOccupancyAsync(string landlordId, int? houseId = null)
    {
        var (overall, _) = await CalculateOccupancyAsync(landlordId, houseId);
        return overall;
    }

    private async Task<(HouseOccupancyDto Overall, List<HouseOccupancyDto> Houses)> CalculateOccupancyAsync(string landlordId, int? houseId = null)
    {
        var q = _db.BoardingHouses
            .Include(h => h.Rooms)
            .Where(h => h.LandlordId == landlordId);

        if (houseId.HasValue) q = q.Where(h => h.Id == houseId.Value);

        var houses = await q.ToListAsync();

        var list = new List<HouseOccupancyDto>();
        int totalAll = 0, occAll = 0, availAll = 0, maintAll = 0;

        foreach (var h in houses)
        {
            var hDto = new HouseOccupancyDto
            {
                HouseId = h.Id,
                HouseName = h.Name,
                TotalFloors = h.TotalFloors,
                TotalRooms = h.Rooms.Count,
                OccupiedRooms = h.Rooms.Count(r => r.Status == RoomStatus.Occupied),
                AvailableRooms = h.Rooms.Count(r => r.Status == RoomStatus.Available),
                MaintenanceRooms = h.Rooms.Count(r => r.Status == RoomStatus.Maintenance)
            };
            hDto.OccupancyRate = hDto.TotalRooms == 0 ? 0 : Math.Round((double)hDto.OccupiedRooms / hDto.TotalRooms * 100, 1);

            // Gom theo tầng
            var floors = h.Rooms.GroupBy(r => r.Floor)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var tot = g.Count();
                    var occ = g.Count(r => r.Status == RoomStatus.Occupied);
                    return new FloorOccupancyDto
                    {
                        Floor = g.Key,
                        TotalRooms = tot,
                        OccupiedRooms = occ,
                        AvailableRooms = g.Count(r => r.Status == RoomStatus.Available),
                        MaintenanceRooms = g.Count(r => r.Status == RoomStatus.Maintenance),
                        OccupancyRate = tot == 0 ? 0 : Math.Round((double)occ / tot * 100, 1)
                    };
                }).ToList();

            hDto.Floors = floors;
            list.Add(hDto);

            totalAll += hDto.TotalRooms;
            occAll += hDto.OccupiedRooms;
            availAll += hDto.AvailableRooms;
            maintAll += hDto.MaintenanceRooms;
        }

        var overall = new HouseOccupancyDto
        {
            HouseId = houseId ?? 0,
            HouseName = houseId.HasValue && list.Count > 0 ? list[0].HouseName : "Tất cả nhà trọ",
            TotalFloors = list.Sum(x => x.TotalFloors),
            TotalRooms = totalAll,
            OccupiedRooms = occAll,
            AvailableRooms = availAll,
            MaintenanceRooms = maintAll,
            OccupancyRate = totalAll == 0 ? 0 : Math.Round((double)occAll / totalAll * 100, 1),
            Floors = list.SelectMany(x => x.Floors).GroupBy(f => f.Floor).OrderBy(g => g.Key).Select(g =>
            {
                var tot = g.Sum(f => f.TotalRooms);
                var occ = g.Sum(f => f.OccupiedRooms);
                return new FloorOccupancyDto
                {
                    Floor = g.Key,
                    TotalRooms = tot,
                    OccupiedRooms = occ,
                    AvailableRooms = g.Sum(f => f.AvailableRooms),
                    MaintenanceRooms = g.Sum(f => f.MaintenanceRooms),
                    OccupancyRate = tot == 0 ? 0 : Math.Round((double)occ / tot * 100, 1)
                };
            }).ToList()
        };

        return (overall, list);
    }

    private async Task<ProjectedRevenueDto> CalculateProjectedRevenueAsync(List<int> targetHouseIds)
    {
        var nextMonthDate = DateTime.Today.AddMonths(1);
        var targetMonth = nextMonthDate.Month;
        var targetYear = nextMonthDate.Year;

        var startOfTarget = new DateTime(targetYear, targetMonth, 1);
        var endOfTarget = startOfTarget.AddMonths(1).AddDays(-1);

        // Hợp đồng Active còn hiệu lực trong tháng tới
        var activeContracts = await _db.Contracts
            .Include(c => c.Members)
            .Include(c => c.Services).ThenInclude(cs => cs.Service)
            .Include(c => c.Room)
            .Where(c => targetHouseIds.Contains(c.Room.BoardingHouseId)
                        && c.Status == ContractStatus.Active
                        && c.StartDate <= endOfTarget
                        && c.EndDate >= startOfTarget)
            .ToListAsync();

        var projectedRent = activeContracts.Sum(c => c.MonthlyRent);

        decimal projectedServices = 0;
        foreach (var c in activeContracts)
        {
            foreach (var cs in c.Services)
            {
                if (cs.Service != null && cs.Service.IsActive)
                {
                    var multiplier = cs.Service.Unit == ServiceUnit.PerPerson ? (c.Members.Count + 1) : 1;
                    projectedServices += cs.Service.Price * multiplier;
                }
            }
        }

        // Ước tính tiền điện nước: lấy trung bình điện nước từ các hóa đơn gần nhất / phòng
        var recentInvoices = await _db.Invoices
            .Include(i => i.Details)
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Where(i => targetHouseIds.Contains(i.Contract.Room.BoardingHouseId)
                        && i.Status == InvoiceStatus.Paid)
            .OrderByDescending(i => i.Year).ThenByDescending(i => i.Month)
            .Take(30)
            .ToListAsync();

        decimal estUtilityPerRoom = 0;
        if (recentInvoices.Count > 0)
        {
            decimal totalUtil = 0;
            foreach (var inv in recentInvoices)
            {
                foreach (var d in inv.Details)
                {
                    var name = d.ItemName.ToLowerInvariant();
                    if (name.Contains("điện") || name.Contains("nước"))
                    {
                        totalUtil += d.Amount;
                    }
                }
            }
            estUtilityPerRoom = totalUtil / recentInvoices.Count;
        }
        else
        {
            // Mặc định ước lượng 250.000đ điện nước / phòng nếu chưa có hóa đơn nào
            estUtilityPerRoom = 250000m;
        }

        var estimatedUtility = activeContracts.Count * estUtilityPerRoom;
        var total = projectedRent + projectedServices + estimatedUtility;

        return new ProjectedRevenueDto
        {
            TargetMonth = targetMonth,
            TargetYear = targetYear,
            ActiveContractsCount = activeContracts.Count,
            ProjectedRent = projectedRent,
            ProjectedServices = projectedServices,
            EstimatedUtility = Math.Round(estimatedUtility, 0),
            TotalProjected = Math.Round(total, 0)
        };
    }
}

