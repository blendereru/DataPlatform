namespace DataPlatform.Api.Models;

public class VisualizationNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Layer { get; set; } = string.Empty;
    public string TableType { get; set; } = string.Empty;
    public long? RowCount { get; set; }
}