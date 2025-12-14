namespace DataPlatform.Api.Models;

/// <summary>
/// Data quality rules and validations
/// </summary>
public class DataQualityRule
{
    public Guid Id { get; set; }
    public Guid DatasetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RuleType Type { get; set; }
    public string ValidationQuery { get; set; } = string.Empty; // SQL that returns pass/fail
    public string? Threshold { get; set; } // e.g., "95%" for completeness
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation
    public Dataset Dataset { get; set; } = null!;
}