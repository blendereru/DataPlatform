namespace DataPlatform.Api.Models;

public class VisualizationEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? PipelineName { get; set; }
    public string Description { get; set; } = string.Empty;
}