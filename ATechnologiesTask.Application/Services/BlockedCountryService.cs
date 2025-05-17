using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Core.Entities;
using ATechnologiesTask.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;

namespace ATechnologiesTask.Application.Services;

public class BlockedCountryService(IBlockedCountryRepository blockedCountryRepository, IGeoLocationService geoLocationService,
    IHttpContextAccessor httpContextAccessor, ILogger<BlockedCountryService> logger) : IBlockedCountryService
{
    public async Task<ApiResponse<string>> BlockCountryAsync(BlockCountryRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CountryCode))
        {
            return ApiResponse<string>.Failure("Country code is required", 400);
        }

        if (!IsValidCountryCode(request.CountryCode, out var expectedCountryName))
        {
            return ApiResponse<string>.Failure("Invalid country code: Must be a valid ISO 3166-1 alpha-2 code", 400);
        }

        if (string.IsNullOrWhiteSpace(request.CountryName))
        {
            return ApiResponse<string>.Failure("Country name is required", 400);
        }

        if (!string.Equals(request.CountryName.Trim(), expectedCountryName, StringComparison.OrdinalIgnoreCase))
        {
            
            return ApiResponse<string>.Failure($"Country name '{request.CountryName}' does not match expected '{expectedCountryName}' for code '{request.CountryCode}'", 400);
        }

        var countryCode = request.CountryCode.ToUpper();
        if (await blockedCountryRepository.IsCountryBlockedAsync(countryCode))
        {
            return ApiResponse<string>.Failure($"{countryCode} is already blocked (permanent or temporary)", 409);
        }

        var country = new BlockedCountry
        {
            CountryCode = countryCode,
            CountryName = request.CountryName.Trim(),
            BlockedAt = DateTime.UtcNow
        };
        await blockedCountryRepository.AddBlockedCountryAsync(country);
        return ApiResponse<string>.Ok($"{countryCode} is blocked ", 201);
    }

    public async Task<ApiResponse<string>> UnblockCountryAsync(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return ApiResponse<string>.Failure("Country code is required", 400);
        }

        if (!IsValidCountryCode(countryCode, out _))
        {
            return ApiResponse<string>.Failure("Invalid country code: Must be a valid ISO 3166-1 alpha-2 code", 400);
        }

        var result = await blockedCountryRepository.RemoveBlockedCountryAsync(countryCode.ToUpper());
        if (!result)
        {
            return ApiResponse<string>.Failure("Country not found", 404);
        }

        return ApiResponse<string>.Ok($"{countryCode} is unblocked");
    }

    public async Task<ApiResponse<PaginatedResponse<BlockedCountry>>> GetBlockedCountriesAsync(string? search, int page, int pageSize)
    {
        if (page < 1 || pageSize < 1)
        {
            return ApiResponse<PaginatedResponse<BlockedCountry>>.Failure("Page and pageSize must be positive", 400);
        }

        var countries = await blockedCountryRepository.GetBlockedCountriesAsync(search?.Trim(), page, pageSize);
        var count = await blockedCountryRepository.GetBlockedCountriesCountAsync(search?.Trim());

        return ApiResponse<PaginatedResponse<BlockedCountry>>.Ok(new PaginatedResponse<BlockedCountry>
        {
            Items = countries,
            TotalCount = count
        });
    }

    public async Task<ApiResponse<string>> BlockCountryTemporarilyAsync(TemporalBlockRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CountryCode))
        {
            return ApiResponse<string>.Failure("Country code is required", 400);
        }

        if (!IsValidCountryCode(request.CountryCode, out _))
        {
            return ApiResponse<string>.Failure("Invalid country code: Must be a valid ISO 3166-1 alpha-2 code", 400);
        }

        if (request.DurationMinutes < 1 || request.DurationMinutes > 1440)
        {
            return ApiResponse<string>.Failure("Duration must be between 1 and 1440 minutes", 400);
        }

        var countryCode = request.CountryCode.ToUpper();
        if (await blockedCountryRepository.IsCountryTemporarilyBlockedAsync(countryCode))
        {
            return ApiResponse<string>.Failure($"{countryCode} is already temporarily blocked", 409);
        }

        if (await blockedCountryRepository.IsCountryPermanentlyBlockedAsync(countryCode))
        {
            return ApiResponse<string>.Failure($"{countryCode} is already permanently blocked", 409);
        }

        var block = new TemporalBlock
        {
            CountryCode = countryCode,
            BlockedUntil = DateTime.UtcNow.AddMinutes(request.DurationMinutes)
        };
        await blockedCountryRepository.AddTemporalBlockAsync(block);
        return ApiResponse<string>.Ok($"{countryCode} temporarily blocked", 201);
    }

    public async Task<ApiResponse<bool>> CheckIpBlockAsync()
    {
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        if (ip == "::1" || ip == "127.0.0.1")
        {
            logger.LogDebug("CheckIpBlockAsync: Local IP {IpAddress} detected; not blocked", ip);
            await LogAttemptAsync(ip, false, "LOCAL");
            return ApiResponse<bool>.Ok(false);
        }

        var response = await geoLocationService.GetGeoLocationAsync(ip);
        logger.LogDebug("CheckIpBlockAsync: GeoLocation response for IP {IpAddress}: {CountryCode}", ip, response?.CountryCode ?? "null");

        if (response == null || string.IsNullOrEmpty(response.CountryCode))
        {
            logger.LogWarning("CheckIpBlockAsync: Failed to fetch country code for IP {IpAddress}", ip);
            return ApiResponse<bool>.Ok(false);
        }

        var isBlocked = await blockedCountryRepository.IsCountryBlockedAsync(response.CountryCode);
        await LogAttemptAsync(ip, isBlocked, response.CountryCode);
        logger.LogInformation("CheckIpBlockAsync: IP {IpAddress} isBlocked={IsBlocked}, CountryCode={CountryCode}", ip, isBlocked, response.CountryCode);
        return ApiResponse<bool>.Ok(isBlocked);
    }

    public async Task<ApiResponse<PaginatedResponse<BlockedAttemptLog>>> GetBlockedAttemptsAsync(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1)
        {
            return ApiResponse<PaginatedResponse<BlockedAttemptLog>>.Failure("Page and pageSize must be positive", 400);
        }

        var attempts = await blockedCountryRepository.GetBlockedAttemptsAsync(page, pageSize);
        var count = await blockedCountryRepository.GetBlockedAttemptsCountAsync();

        return ApiResponse<PaginatedResponse<BlockedAttemptLog>>.Ok(new PaginatedResponse<BlockedAttemptLog>
        {
            Items = attempts,
            TotalCount = count
        });
    }

    public async Task<ApiResponse<GeoLocationResponse>> LookupIpAsync(string? ipAddress)
    {
        var ip = ipAddress ?? httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        if (!IPAddress.TryParse(ip, out _))
        {
            logger.LogWarning("LookupIpAsync: Invalid IP format: {IpAddress}", ip);
            return ApiResponse<GeoLocationResponse>.Failure("Invalid IP address format", 400);
        }

        var response = await geoLocationService.GetGeoLocationAsync(ip);
        if (response == null)
        {
            logger.LogWarning("LookupIpAsync: Failed to fetch geolocation for IP {IpAddress}", ip);
            return ApiResponse<GeoLocationResponse>.Failure("Failed to fetch geolocation data", 404);
        }

        logger.LogInformation("LookupIpAsync: Completed for IP {IpAddress}, CountryCode={CountryCode}, CountryName={CountryName}, Isp={Isp}",
            ip, response.CountryCode, response.CountryName, response.Isp);
        return ApiResponse<GeoLocationResponse>.Ok(response);
    }

    private async Task LogAttemptAsync(string ip, bool isBlocked, string countryCode)
    {
        var log = new BlockedAttemptLog
        {
            IpAddress = ip,
            Timestamp = DateTime.UtcNow,
            CountryCode = countryCode,
            IsBlocked = isBlocked,
            UserAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty
        };
        await blockedCountryRepository.AddBlockedAttemptAsync(log);
        logger.LogInformation("Logged attempt for IP {IpAddress}: CountryCode={CountryCode}, IsBlocked={IsBlocked}", ip, countryCode, isBlocked);
    }

    private bool IsValidCountryCode(string countryCode, out string countryName)
    {
        countryName = string.Empty;
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
        {
            logger.LogDebug("Country code validation failed: {CountryCode} (invalid length or empty)", countryCode);
            return false;
        }

        try
        {
            var region = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c => new RegionInfo(c.Name))
                .FirstOrDefault(r => r.TwoLetterISORegionName.Equals(countryCode, StringComparison.OrdinalIgnoreCase));

            if (region == null)
            {
                logger.LogDebug("Country code validation failed: {CountryCode} (not a valid ISO 3166-1 alpha-2 code)", countryCode);
                return false;
            }

            countryName = region.EnglishName;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Country code validation failed: {CountryCode} (exception during validation)", countryCode);
            return false;
        }
    }
}