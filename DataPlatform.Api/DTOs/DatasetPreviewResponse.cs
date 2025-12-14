using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class DatasetPreviewResponse
{
    public Guid DatasetId { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public List<DatasetColumn> Schema { get; set; } = new();
    public int RowsShown { get; set; }
    public long TotalRows { get; set; }
}