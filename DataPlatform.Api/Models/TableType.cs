namespace DataPlatform.Api.Models;

// <summary>
/// Type of table in dimensional modeling
/// </summary>
public enum TableType
{
    /// <summary>
    /// Regular operational table
    /// </summary>
    Operational,
    
    /// <summary>
    /// Staging table for data loading
    /// </summary>
    Staging,
    
    /// <summary>
    /// Fact table containing business metrics
    /// </summary>
    Fact,
    
    /// <summary>
    /// Dimension table containing descriptive attributes
    /// </summary>
    Dimension,
    
    /// <summary>
    /// Pre-aggregated summary table
    /// </summary>
    Aggregate
}