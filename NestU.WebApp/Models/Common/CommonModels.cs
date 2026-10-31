using System.ComponentModel.DataAnnotations;

namespace NestU.WebApp.Models.Common;

public class RejectModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do")] [StringLength(500)] [Display(Name = "Lý do")]
    public string Reason { get; set; } = string.Empty;
}

public class DecisionModel
{
    public int Id { get; set; }
    public bool Approve { get; set; }
    [StringLength(500)] [Display(Name = "Ghi chú")] public string? Note { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

/// <summary>Kết quả OData: { "@odata.count": n, "value": [...] }</summary>
public class ODataResult<T>
{
    [System.Text.Json.Serialization.JsonPropertyName("@odata.count")] public int? Count { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("value")] public List<T> Value { get; set; } = new();
}
