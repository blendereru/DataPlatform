namespace DataPlatform.Api.Models;

/// <summary>
/// Represents a dataset (table, collection, or file) from a data source
/// </summary>
public class Dataset
{
    public Guid Id { get; set; }
    public Guid DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<DatasetColumn> Schema { get; set; } = new();
    public long? RowCount { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // NEW: Data warehouse properties
    public DataWarehouseLayer Layer { get; set; } = DataWarehouseLayer.Source;
    public TableType TableType { get; set; } = TableType.Operational;
    public string? BusinessKey { get; set; } // Natural key from source system
    public string? GrainDescription { get; set; } // What does one row represent?
    
    // Navigation
    public DataSource DataSource { get; set; } = null!;
}