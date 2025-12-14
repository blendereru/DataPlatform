namespace DataPlatform.Api.Models.Messages;

public class PipelineExecutionMessage
{
    public Guid PipelineId { get; set; }
    public Guid RunId { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
}