namespace NestU.API.DTOs.Stats;

public class MonthlyRevenueDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RentRevenue { get; set; }
    public decimal ElectricRevenue { get; set; }
    public decimal WaterRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public int PaidInvoiceCount { get; set; }
}

public class FloorOccupancyDto
{
    public int Floor { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public double OccupancyRate { get; set; }
}

public class HouseOccupancyDto
{
    public int HouseId { get; set; }
    public string HouseName { get; set; } = string.Empty;
    public int TotalFloors { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public double OccupancyRate { get; set; }
    public List<FloorOccupancyDto> Floors { get; set; } = new();
}

public class ProjectedRevenueDto
{
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
    public int ActiveContractsCount { get; set; }
    public decimal ProjectedRent { get; set; }
    public decimal ProjectedServices { get; set; }
    public decimal EstimatedUtility { get; set; }
    public decimal TotalProjected { get; set; }
}

public class LandlordStatsDto
{
    public int Year { get; set; }
    public int? SelectedHouseId { get; set; }
    public string? SelectedHouseName { get; set; }
    public decimal TotalRevenueThisYear { get; set; }
    public decimal TotalRevenueThisMonth { get; set; }
    public decimal TotalRevenueLastMonth { get; set; }
    public double? MonthOverMonthGrowthPercent { get; set; }
    public decimal UnpaidInvoicesTotal { get; set; }
    public int UnpaidInvoicesCount { get; set; }

    /// <summary>12 tháng của năm được chọn</summary>
    public List<MonthlyRevenueDto> MonthlyRevenues { get; set; } = new();

    /// <summary>Tỷ lệ lấp đầy tổng thể hoặc của nhà trọ được chọn</summary>
    public HouseOccupancyDto OverallOccupancy { get; set; } = new();

    /// <summary>Danh sách tỷ lệ lấp đầy theo từng nhà trọ</summary>
    public List<HouseOccupancyDto> HousesOccupancy { get; set; } = new();

    /// <summary>Dự phóng doanh thu tháng tới</summary>
    public ProjectedRevenueDto ProjectedRevenue { get; set; } = new();
}

