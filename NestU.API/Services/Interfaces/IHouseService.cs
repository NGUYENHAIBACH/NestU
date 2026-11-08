using NestU.API.DTOs.Houses;

namespace NestU.API.Services.Interfaces;

public interface IHouseService
{
    Task<LandlordProfileDto> GetProfileAsync(string landlordId);
    Task UpdateProfileAsync(string landlordId, LandlordProfileDto dto);

    Task<List<HouseDto>> GetMineAsync(string landlordId);
    Task<HouseDto> GetMineByIdAsync(string landlordId, int id);
    Task<HouseDto> CreateAsync(string landlordId, CreateHouseDto dto);
    Task UpdateAsync(string landlordId, int id, UpdateHouseDto dto);
    Task DeleteAsync(string landlordId, int id);
    Task<List<FloorSummaryDto>> GetFloorsAsync(string landlordId, int houseId);

    Task<List<CertificateDto>> GetCertificatesAsync(string landlordId, int houseId);
    Task<CertificateDto> UploadCertificateAsync(string landlordId, int houseId, CreateCertificateDto dto);

    Task<List<ServiceDto>> GetServicesAsync(string landlordId, int? houseId);
    Task<ServiceDto> CreateServiceAsync(string landlordId, ServiceDto dto);
    Task UpdateServiceAsync(string landlordId, int id, ServiceDto dto);
    Task DeleteServiceAsync(string landlordId, int id);

    /// <summary>Ném 403 nếu nhà trọ không thuộc landlord. Trả về entity.</summary>
    Task<Entities.BoardingHouse> GetOwnedHouseAsync(string landlordId, int houseId, bool track = false);
}
