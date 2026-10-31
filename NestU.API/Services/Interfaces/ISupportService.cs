using NestU.API.DTOs.Admin;
using NestU.API.DTOs.Support;

namespace NestU.API.Services.Interfaces;

public interface ISupportService
{
    Task<List<MaintenanceDto>> GetMaintenanceAsync(string userId, string role, string? status);
    Task<MaintenanceDto> CreateMaintenanceAsync(string tenantId, CreateMaintenanceDto dto);
    Task UpdateMaintenanceStatusAsync(string landlordId, int id, UpdateMaintenanceStatusDto dto);

    Task<ReviewDto> CreateReviewAsync(string tenantId, CreateReviewDto dto);
    Task<List<ReviewDto>> GetRoomReviewsAsync(int roomId);
    Task<List<ReviewDto>> GetTenantReviewsAsync(string tenantId);

    Task<ReportDto> CreateReportAsync(string reporterId, CreateReportDto dto);
    Task<List<ReportDto>> GetMyReportsAsync(string reporterId);
}
