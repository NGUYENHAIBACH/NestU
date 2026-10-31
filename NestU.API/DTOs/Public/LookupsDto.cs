namespace NestU.API.DTOs.Public;

public class LookupItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UniversityDto : LookupItemDto
{
    public string? ShortName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class LookupsDto
{
    public List<LookupItemDto> Areas { get; set; } = new();
    public List<LookupItemDto> Amenities { get; set; } = new();
    public List<LookupItemDto> AssetTypes { get; set; } = new();
    public List<UniversityDto> Universities { get; set; } = new();
}

public class HomeStatsDto
{
    public int AvailableRooms { get; set; }
    public int ActiveHouses { get; set; }
    public int MaxElectricityPrice { get; set; } = 4000;
    public int ActiveTenants { get; set; }
}
