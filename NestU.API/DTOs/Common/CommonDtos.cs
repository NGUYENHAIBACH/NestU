using System.ComponentModel.DataAnnotations;

namespace NestU.API.DTOs.Common;

public class RejectDto
{
    [Required(ErrorMessage = "Vui lòng nhập lý do")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class DecisionDto
{
    [Required] public bool Approve { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
