using NestU.API.DTOs.Stats;

namespace NestU.API.Services.Interfaces;

public interface IStatsService
{
    Task<LandlordStatsDto> GetLandlordStatsAsync(string landlordId, int? year = null, int? houseId = null);
    Task<HouseOccupancyDto> GetLandlordOccupancyAsync(string landlordId, int? houseId = null);
}

