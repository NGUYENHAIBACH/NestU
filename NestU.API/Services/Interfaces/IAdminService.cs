using NestU.API.DTOs.Admin;
using NestU.API.DTOs.Common;
using NestU.API.DTOs.Houses;

namespace NestU.API.Services.Interfaces;

public interface IAdminService
{
    Task<PagedResult<UserListDto>> GetUsersAsync(string? role, string? status, string? search, int page, int pageSize);
    Task SetLockAsync(string adminId, string userId, bool locked);
    Task SetRoleAsync(string userId, SetRoleDto dto);

    Task<List<LandlordApprovalDto>> GetLandlordApprovalsAsync(string? status);
    Task DecideLandlordAsync(int profileId, DecisionDto dto);

    Task<List<CertificateDto>> GetCertificatesAsync(string? status);
    Task<CertificateDto> GetCertificateAsync(int id);
    Task ReviewCertificateAsync(int id, ReviewCertificateDto dto);

    Task<List<ReportDto>> GetReportsAsync(string? status);
    Task ResolveReportAsync(int id, ResolveReportDto dto);

    Task<List<AdminHouseDto>> GetHousesAsync();
    Task SetHouseHiddenAsync(int id, SetHouseHiddenDto dto);

    Task<List<CatalogItemDto>> GetCatalogAsync(string type);
    Task<CatalogItemDto> CreateCatalogAsync(string type, CatalogItemDto dto);
    Task UpdateCatalogAsync(string type, int id, CatalogItemDto dto);
    Task DeleteCatalogAsync(string type, int id);
    Task<MergeAreaResultDto> MergeAreaAsync(int sourceId, int targetId);

    Task<AdminStatsDto> GetStatsAsync();
}
