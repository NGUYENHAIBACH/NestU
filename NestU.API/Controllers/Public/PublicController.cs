using Microsoft.AspNetCore.Mvc;
using NestU.API.DTOs.Public;
using NestU.API.Services.Interfaces;

namespace NestU.API.Controllers.Public;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IPublicService _publicService;
    public PublicController(IPublicService publicService) => _publicService = publicService;

    /// <summary>Danh mục dùng cho form: khu vực, tiện ích, loại tài sản, trường (kèm tọa độ).</summary>
    [HttpGet("lookups")]
    public async Task<ActionResult<LookupsDto>> Lookups() => Ok(await _publicService.GetLookupsAsync());

    /// <summary>Số liệu trang Home.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<HomeStatsDto>> Stats() => Ok(await _publicService.GetStatsAsync());
}
