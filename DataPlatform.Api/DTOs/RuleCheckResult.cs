using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class RuleCheckResult
{
    public string RuleName { get; set; } = string.Empty;
    public RuleType Type { get; set; }
    public bool Passed { get; set; }
    public double Score { get; set; }
    public string? Details { get; set; }
}