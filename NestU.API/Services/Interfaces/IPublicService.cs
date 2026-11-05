using NestU.API.DTOs.Houses;
using NestU.API.DTOs.Public;
using NestU.API.DTOs.Rooms;

namespace NestU.API.Services.Interfaces;

public interface IPublicService
{
    Task<LookupsDto> GetLookupsAsync();
    Task<HomeStatsDto> GetStatsAsync();
    Task<List<RoomListDto>> GetFeaturedRoomsAsync(int take = 6);
    Task<HouseDetailDto> GetHouseDetailAsync(int houseId);
    Task<List<MapMarkerDto>> GetMapMarkersAsync();
}

