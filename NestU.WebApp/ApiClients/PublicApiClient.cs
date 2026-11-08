using NestU.WebApp.Models.Common;
using NestU.WebApp.Models.Public;

namespace NestU.WebApp.ApiClients;

public class PublicApiClient : ApiClientBase
{
    public PublicApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<LookupsDto>> LookupsAsync() => GetAsync<LookupsDto>("api/public/lookups");
    public Task<ApiResult<HomeStatsDto>> StatsAsync() => GetAsync<HomeStatsDto>("api/public/stats");
}
