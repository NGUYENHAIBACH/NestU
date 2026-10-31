namespace NestU.API.Entities;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<BoardingHouse> BoardingHouses { get; set; } = new List<BoardingHouse>();
}
