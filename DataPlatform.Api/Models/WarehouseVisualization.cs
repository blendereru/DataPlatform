namespace DataPlatform.Api.Models;

public class WarehouseVisualization
{
    public List<VisualizationNode> Nodes { get; set; } = new();
    public List<VisualizationEdge> Edges { get; set; } = new();
}