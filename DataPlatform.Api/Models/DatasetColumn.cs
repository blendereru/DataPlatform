namespace DataPlatform.Api.Models;

/// <summary>
/// Schema definition for a dataset column
/// </summary>
public class DatasetColumn
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? Description { get; set; }
}