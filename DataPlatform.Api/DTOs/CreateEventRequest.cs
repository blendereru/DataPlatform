using System.ComponentModel.DataAnnotations;

namespace DataPlatform.Api.DTOs;

public class CreateEventRequest
{
    [Required]
    public string Payload { get; set; } = null!;
}