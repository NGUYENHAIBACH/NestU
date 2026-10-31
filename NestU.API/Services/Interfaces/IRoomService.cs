using NestU.API.DTOs.Assets;
using NestU.API.DTOs.Rooms;

namespace NestU.API.Services.Interfaces;

public interface IRoomService
{
    Task<List<RoomListDto>> GetMineAsync(string landlordId, int? houseId, int? floor);
    Task<RoomDetailDto> GetMineByIdAsync(string landlordId, int roomId);
    Task<RoomDetailDto> CreateAsync(string landlordId, CreateRoomDto dto);
    Task<List<RoomListDto>> BulkCreateAsync(string landlordId, int houseId, BulkCreateRoomsDto dto);
    Task UpdateAsync(string landlordId, int roomId, UpdateRoomDto dto);
    Task DeleteAsync(string landlordId, int roomId);
    Task SetStatusAsync(string landlordId, int roomId, SetRoomStatusDto dto);

    Task<List<RoomAssetDto>> GetAssetsAsync(string landlordId, int roomId);
    Task<RoomAssetDto> AddAssetAsync(string landlordId, int roomId, RoomAssetDto dto);
    Task UpdateAssetAsync(string landlordId, int roomId, int assetId, RoomAssetDto dto);
    Task DeleteAssetAsync(string landlordId, int roomId, int assetId);
    Task CopyAssetsAsync(string landlordId, int roomId, CopyAssetsDto dto);

    // Công khai
    Task<RoomDetailDto> GetPublicDetailAsync(int roomId, string viewerKey);
}
