using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NestU.API.Helpers;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            ApiException api => (api.StatusCode, api.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Dữ liệu vừa bị người khác thay đổi, vui lòng tải lại."),
            DbUpdateException dbEx => (StatusCodes.Status409Conflict, TranslateDbError(dbEx)),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Chưa đăng nhập."),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau.")
        };

        if (status >= 500) _logger.LogError(ex, "Unhandled exception");

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = http.Request.Path
        }, ct);
        return true;
    }

    private static string TranslateDbError(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        if (msg.Contains("CK_BoardingHouse_ElectricityPrice")) return "Giá điện phải từ 1 đến 4.000đ/kWh.";
        if (msg.Contains("IX_Rooms_BoardingHouseId_RoomNumber")) return "Số phòng đã tồn tại trong nhà trọ này.";
        if (msg.Contains("UX_Contract_OneActivePerRoom")) return "Phòng này đã có hợp đồng đang hiệu lực.";
        if (msg.Contains("UX_Contract_OneActivePerTenant")) return "Người thuê này đã có hợp đồng đang hiệu lực.";
        if (msg.Contains("UX_RentalRequest_OnePendingPerRoomAndTenant")) return "Bạn đã gửi yêu cầu thuê phòng này rồi, vui lòng chờ chủ trọ duyệt.";
        if (msg.Contains("UX_Appointment_OnePendingPerRoomAndTenant")) return "Bạn đã có lịch xem đang chờ cho phòng này rồi.";
        if (msg.Contains("IX_Invoices_ContractId_Year_Month")) return "Hóa đơn tháng này đã được lập.";
        if (msg.Contains("IX_UtilityReadings_ContractId_Year_Month")) return "Chỉ số điện nước tháng này đã được ghi.";
        if (msg.Contains("IX_Reviews_ContractId")) return "Hợp đồng này đã được đánh giá.";
        if (msg.Contains("CK_Review_Rating")) return "Điểm đánh giá phải từ 1 đến 5 sao.";
        if (msg.Contains("CK_")) return "Dữ liệu vi phạm ràng buộc của hệ thống.";
        if (msg.Contains("REFERENCE constraint")) return "Không thể xóa vì dữ liệu đang được sử dụng ở nơi khác.";
        return "Không lưu được dữ liệu.";
    }
}
