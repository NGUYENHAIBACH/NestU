using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using NestU.API.Data;
using NestU.API.DTOs.Rooms;
using NestU.API.Entities.Enums;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.OData;

/// <summary>
/// GET /odata/Rooms?$filter=Price le 3000000 and Floor eq 2&amp;$orderby=Price&amp;$top=10&amp;$skip=0&amp;$count=true
/// Chỉ trả phòng của nhà trọ Active (đã duyệt PCCC). Mục 1.9.
/// </summary>
public class RoomsController : ODataController
{
    private readonly IRoomService _rooms;
    public RoomsController(IRoomService rooms) => _rooms = rooms;

    [EnableQuery(PageSize = 20, MaxTop = 50,
        AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Top
                            | AllowedQueryOptions.Skip | AllowedQueryOptions.Select | AllowedQueryOptions.Count)]
    public IQueryable<RoomListDto> Get() => _rooms.GetPublicRoomsQuery();
}

public static class RoomQueries
{
    /// <summary>Projection dùng chung cho OData và các API công khai.</summary>
    public static IQueryable<RoomListDto> PublicRooms(NestUDbContext db) =>
        db.Rooms.AsNoTracking()
            .Where(r => r.BoardingHouse.Status == HouseStatus.Active)
            .Select(r => new RoomListDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                Area = r.Area,
                Price = r.Price,
                DepositAmount = r.DepositAmount,
                Capacity = r.Capacity,
                Status = r.Status == RoomStatus.Available ? "Available" : r.Status == RoomStatus.Occupied ? "Occupied" : "Maintenance",
                AvailabilityLabel = r.Status == RoomStatus.Available ? "Còn phòng"
                                  : r.Status == RoomStatus.Occupied ? "Hết phòng" : "Đang sửa",
                ViewCount = r.ViewCount,
                ThumbnailUrl = r.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                HouseId = r.BoardingHouseId,
                HouseName = r.BoardingHouse.Name,
                Address = r.Address ?? r.BoardingHouse.Address,
                AreaId = r.BoardingHouse.AreaId,
                AreaName = r.BoardingHouse.Area.Name,
                ElectricityPrice = r.BoardingHouse.ElectricityPrice,
                StudentOnly = r.BoardingHouse.AllowedTenantType == AllowedTenantType.StudentOnly,
                FireSafetyApproved = true,
                CreatedAt = r.CreatedAt
            });
}
