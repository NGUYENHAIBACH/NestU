namespace NestU.API.Entities;

public class RoomImage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
