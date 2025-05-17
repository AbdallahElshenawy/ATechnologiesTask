using ATechnologiesTask.Core.Entities;

namespace ATechnologiesTask.Core.Interfaces;

public interface IBlockedCountryRepository
{
    Task AddBlockedCountryAsync(BlockedCountry country);
    Task<bool> RemoveBlockedCountryAsync(string countryCode);
    Task<IEnumerable<BlockedCountry>> GetBlockedCountriesAsync(string? search, int page, int pageSize);
    Task<int> GetBlockedCountriesCountAsync(string? search);
    Task<bool> IsCountryBlockedAsync(string countryCode);
    Task<bool> IsCountryTemporarilyBlockedAsync(string countryCode);
    Task<bool> IsCountryPermanentlyBlockedAsync(string countryCode);
    Task AddTemporalBlockAsync(TemporalBlock block);
    Task<int> RemoveExpiredTemporalBlocksAsync();
    Task AddBlockedAttemptAsync(BlockedAttemptLog log);
    Task<IEnumerable<BlockedAttemptLog>> GetBlockedAttemptsAsync(int page, int pageSize);
    Task<int> GetBlockedAttemptsCountAsync();
}