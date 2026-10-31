namespace NestU.API.Entities;

public class University
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
