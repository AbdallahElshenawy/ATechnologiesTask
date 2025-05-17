using ATechnologiesTask.Core.Entities;
using ATechnologiesTask.Core.Interfaces;
using System.Collections.Concurrent;

namespace ATechnologiesTask.Infrastructure.Data;

public class InMemoryBlockedCountryRepository : IBlockedCountryRepository
{
    private readonly ConcurrentDictionary<string, BlockedCountry> _blockedCountries = new();
    private readonly ConcurrentDictionary<string, TemporalBlock> _temporalBlocks = new();
    private readonly ConcurrentBag<BlockedAttemptLog> _blockedAttempts = new();

    public Task AddBlockedCountryAsync(BlockedCountry country)
    {
        _blockedCountries[country.CountryCode] = country;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveBlockedCountryAsync(string countryCode)
    {
        return Task.FromResult(_blockedCountries.TryRemove(countryCode, out _));
    }

    public Task<IEnumerable<BlockedCountry>> GetBlockedCountriesAsync(string? search, int page, int pageSize)
    {
        var query = _blockedCountries.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToUpper();
            query = query.Where(c => c.CountryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     c.CountryName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var result = query
            .OrderBy(c => c.CountryCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IEnumerable<BlockedCountry>>(result);
    }

    public Task<int> GetBlockedCountriesCountAsync(string? search)
    {
        var query = _blockedCountries.Values. AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToUpper();
            query = query.Where(c => c.CountryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     c.CountryName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult(query.Count());
    }

    public Task<bool> IsCountryBlockedAsync(string countryCode)
    {
        countryCode = countryCode.ToUpper();
        var isPermanentlyBlocked = _blockedCountries.ContainsKey(countryCode);
        var isTemporarilyBlocked = _temporalBlocks.TryGetValue(countryCode, out var block) && block.BlockedUntil > DateTime.UtcNow;
        return Task.FromResult(isPermanentlyBlocked || isTemporarilyBlocked);
    }

    public Task<bool> IsCountryTemporarilyBlockedAsync(string countryCode)
    {
        countryCode = countryCode.ToUpper();
        return Task.FromResult(_temporalBlocks.TryGetValue(countryCode, out var block) && block.BlockedUntil > DateTime.UtcNow);
    }

    public Task<bool> IsCountryPermanentlyBlockedAsync(string countryCode)
    {
        countryCode = countryCode.ToUpper();
        return Task.FromResult(_blockedCountries.ContainsKey(countryCode));
    }

    public Task AddTemporalBlockAsync(TemporalBlock block)
    {
        _temporalBlocks[block.CountryCode] = block;
        return Task.CompletedTask;
    }

    public Task<int> RemoveExpiredTemporalBlocksAsync()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _temporalBlocks
            .Where(kvp => kvp.Value.BlockedUntil <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        int removedCount = 0;
        foreach (var key in expiredKeys)
        {
            if (_temporalBlocks.TryRemove(key, out _))
            {
                removedCount++;
            }
        }
        return Task.FromResult(removedCount);
    }

    public Task AddBlockedAttemptAsync(BlockedAttemptLog log)
    {
        _blockedAttempts.Add(log);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<BlockedAttemptLog>> GetBlockedAttemptsAsync(int page, int pageSize)
    {
        var result = _blockedAttempts
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IEnumerable<BlockedAttemptLog>>(result);
    }

    public Task<int> GetBlockedAttemptsCountAsync()
    {
        return Task.FromResult(_blockedAttempts.Count);
    }
}