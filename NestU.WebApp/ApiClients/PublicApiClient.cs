using NestU.WebApp.Models.Common;

namespace NestU.WebApp.ApiClients;

public class LookupsDto
{
    public List<LookupItem> Areas { get; set; } = new();
    public List<LookupItem> Amenities { get; set; } = new();
    public List<LookupItem> AssetTypes { get; set; } = new();
    public List<LookupItem> Universities { get; set; } = new();
}

public class HomeStatsDto
{
    public int AvailableRooms { get; set; }
    public int ActiveHouses { get; set; }
    public int MaxElectricityPrice { get; set; }
    public int ActiveTenants { get; set; }
}

public class PublicApiClient : ApiClientBase
{
    public PublicApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<LookupsDto>> LookupsAsync() => GetAsync<LookupsDto>("api/public/lookups");
    public Task<ApiResult<HomeStatsDto>> StatsAsync() => GetAsync<HomeStatsDto>("api/public/stats");
}
