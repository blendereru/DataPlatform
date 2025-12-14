namespace DataPlatform.Api.DTOs;

public class DataQualityReportResponse
{
    public Guid DatasetId { get; set; }
    public string DatasetName { get; set; } = string.Empty;
    public double OverallScore { get; set; }
    public List<RuleCheckResult> RuleResults { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}