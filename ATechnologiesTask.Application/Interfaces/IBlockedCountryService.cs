using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Core.Entities;

namespace ATechnologiesTask.Application.Interfaces;

public interface IBlockedCountryService
{
    Task<ApiResponse<string>> BlockCountryAsync(BlockCountryRequest request);
    Task<ApiResponse<string>> UnblockCountryAsync(string countryCode);
    Task<ApiResponse<PaginatedResponse<BlockedCountry>>> GetBlockedCountriesAsync(string? search, int page, int pageSize);
    Task<ApiResponse<string>> BlockCountryTemporarilyAsync(TemporalBlockRequest request);
    Task<ApiResponse<bool>> CheckIpBlockAsync();
    Task<ApiResponse<PaginatedResponse<BlockedAttemptLog>>> GetBlockedAttemptsAsync(int page, int pageSize);
    Task<ApiResponse<GeoLocationResponse>> LookupIpAsync(string? ipAddress);
}