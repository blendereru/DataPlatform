namespace DataPlatform.Api.Models;

public class EventEntity
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}