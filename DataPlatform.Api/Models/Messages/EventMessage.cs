namespace DataPlatform.Api.Models.Messages;

public class EventMessage
{
    public Guid EventId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}