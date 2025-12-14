namespace DataPlatform.Api.Models;

/// <summary>
/// Result of a data quality check
/// </summary>
public class DataQualityCheck
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public bool Passed { get; set; }
    public double Score { get; set; } // 0-100
    public long RowsChecked { get; set; }
    public long RowsFailed { get; set; }
    public string? Details { get; set; }
    
    // Navigation
    public DataQualityRule Rule { get; set; } = null!;
}