using System.ComponentModel.DataAnnotations;

namespace ATechnologiesTask.Application.DTOs;

public class TemporalBlockRequest
{
    public string CountryCode { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes (24 hours).")]

    public int DurationMinutes { get; set; }
}