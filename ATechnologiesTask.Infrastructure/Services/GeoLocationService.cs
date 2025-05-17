using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ATechnologiesTask.Infrastructure.Services;

public class GeoLocationService : IGeoLocationService
{
    private readonly HttpClient _httpClient;
    private readonly IpGeolocationSettings _settings;
    private readonly ILogger<GeoLocationService> _logger;

    public GeoLocationService(HttpClient httpClient, IOptions<IpGeolocationSettings> settings, ILogger<GeoLocationService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.ipgeolocation.io/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ATechnologiesTask/c-sharp-v1.0");
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GeoLocationResponse?> GetGeoLocationAsync(string ipAddress)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.AccessKey))
            {
                _logger.LogError("ipgeolocation.io API key is missing");
                throw new InvalidOperationException("API key is required for ipgeolocation.io");
            }

            // Skip API call for local IPs
            if (ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                _logger.LogInformation("Local IP {IpAddress} detected; skipping API call", ipAddress);
                return null;
            }

            var endpoint = $"ipgeo?apiKey={_settings.AccessKey}&ip={ipAddress}";
            _logger.LogInformation("Calling ipgeolocation.io for IP {IpAddress} with endpoint {Endpoint}", ipAddress, endpoint);
            var response = await _httpClient.GetAsync(endpoint);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit exceeded for IP {IpAddress}", ipAddress);
                throw new InvalidOperationException("API rate limit exceeded");
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Raw response from ipgeolocation.io for IP {IpAddress}: {Content}", ipAddress, content);
            var data = await response.Content.ReadFromJsonAsync<IpGeolocationResponse>();
            if (data == null || !string.IsNullOrEmpty(data.Message))
            {
                _logger.LogWarning("Invalid response or error from ipgeolocation.io for IP {IpAddress}: {Message}", ipAddress, data?.Message);
                return null;
            }

            if (string.IsNullOrEmpty(data.CountryCode2) || string.IsNullOrEmpty(data.CountryName))
            {
                _logger.LogWarning("Missing country data for IP {IpAddress}: CountryCode2={CountryCode2}, CountryName={CountryName}",
                    ipAddress, data.CountryCode2, data.CountryName);
                return null;
            }

            _logger.LogInformation("Successfully fetched geolocation for IP {IpAddress}: {CountryCode}", ipAddress, data.CountryCode2);
            return new GeoLocationResponse
            {
                Ip = ipAddress,
                CountryCode = data.CountryCode2,
                CountryName = data.CountryName,
                Isp = data.Isp
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch geolocation for IP {IpAddress}", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching geolocation for IP {IpAddress}", ipAddress);
            return null;
        }
    }

    private class IpGeolocationResponse
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonPropertyName("country_code2")]
        public string CountryCode2 { get; set; } = string.Empty;

        [JsonPropertyName("country_name")]
        public string CountryName { get; set; } = string.Empty;

        [JsonPropertyName("isp")]
        public string Isp { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}