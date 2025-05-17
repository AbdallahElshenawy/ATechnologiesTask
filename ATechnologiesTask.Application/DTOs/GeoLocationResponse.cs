namespace ATechnologiesTask.Application.DTOs;

public class GeoLocationResponse
{
    public string Ip { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string? Isp { get; set; }
}