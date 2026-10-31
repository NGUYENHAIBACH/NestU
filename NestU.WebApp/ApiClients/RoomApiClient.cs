using System.Globalization;
using NestU.WebApp.Models.Common;
using NestU.WebApp.Models.Houses;
using NestU.WebApp.Models.Rooms;
using NestU.WebApp.Models.Support;

namespace NestU.WebApp.ApiClients;

/// <summary>API công khai: tìm phòng qua OData, chi tiết phòng/nhà trọ.</summary>
public class RoomApiClient : ApiClientBase
{
    public RoomApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<ODataResult<RoomListDto>>> SearchAsync(RoomSearchModel m)
    {
        var filters = new List<string>();
        if (m.OnlyAvailable) filters.Add("Status eq 'Available'");
        if (m.AreaId is > 0) filters.Add($"AreaId eq {m.AreaId}");
        if (m.MinPrice is > 0) filters.Add($"Price ge {Inv(m.MinPrice.Value)}");
        if (m.MaxPrice is > 0) filters.Add($"Price le {Inv(m.MaxPrice.Value)}");
        if (m.MinArea is > 0) filters.Add($"Area ge {Inv(m.MinArea.Value)}");
        if (m.Floor is > 0) filters.Add($"Floor eq {m.Floor}");
        if (!string.IsNullOrWhiteSpace(m.Keyword))
        {
            var k = m.Keyword.Replace("'", "''");
            filters.Add($"(contains(HouseName,'{k}') or contains(Address,'{k}') or contains(AreaName,'{k}'))");
        }
        var orderby = m.Sort switch
        {
            "price_asc" => "Price asc",
            "price_desc" => "Price desc",
            "views" => "ViewCount desc",
            _ => "CreatedAt desc"
        };
        var size = Math.Clamp(m.PageSize, 1, 50);
        var skip = (Math.Max(1, m.Page) - 1) * size;
        var url = $"odata/Rooms?$count=true&$top={size}&$skip={skip}&$orderby={Uri.EscapeDataString(orderby)}";
        if (filters.Count > 0) url += "&$filter=" + Uri.EscapeDataString(string.Join(" and ", filters));
        return GetAsync<ODataResult<RoomListDto>>(url);
    }

    public Task<ApiResult<List<RoomListDto>>> FeaturedAsync(int take = 6) => GetAsync<List<RoomListDto>>($"api/rooms/featured?take={take}");
    public Task<ApiResult<RoomDetailDto>> DetailAsync(int id) => GetAsync<RoomDetailDto>($"api/rooms/{id}");
    public Task<ApiResult<List<ReviewDto>>> ReviewsAsync(int roomId) => GetAsync<List<ReviewDto>>($"api/rooms/{roomId}/reviews");
    public Task<ApiResult<HouseDetailDto>> HouseAsync(int id) => GetAsync<HouseDetailDto>($"api/houses/{id}");

    private static string Inv(decimal d) => d.ToString(CultureInfo.InvariantCulture);
}
