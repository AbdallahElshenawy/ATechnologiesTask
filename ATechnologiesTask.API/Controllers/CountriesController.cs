using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Application.Services;
using ATechnologiesTask.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace ATechnologiesTask.API.Controllers;

[ApiController]
[Route("api/countries")]
public class BlockedCountriesController(IBlockedCountryService blockedCountryService) : ControllerBase
{
    [HttpPost("block")]
    [SwaggerOperation(Summary = "Block a country", Description = "Adds a country to the blocked list using its code.")]
    public async Task<IActionResult> BlockCountry([FromBody] BlockCountryRequest request)
    {
        var response = await blockedCountryService.BlockCountryAsync(request);
        return StatusCode(response.StatusCode, response);
    }


    [HttpDelete("block/{countryCode}")]
    [SwaggerOperation(Summary = "Unblock a country", Description = "Removes a country from the blocked list by its code.")]
    public async Task<IActionResult> UnblockCountry(string countryCode)
    {
        var response = await blockedCountryService.UnblockCountryAsync(countryCode);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("blocked")]
    [SwaggerOperation(Summary = "List blocked countries", Description = "Returns a paginated list of blocked countries, with optional search by code or name.")]
    public async Task<IActionResult> GetBlockedCountries(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await blockedCountryService.GetBlockedCountriesAsync(search, page, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("temporal-block")]
    [SwaggerOperation(Summary = "Temporarily block a country", Description = "Blocks a country for a specified duration (1–1440 minutes). Auto-unblocked by background service.")]
    public async Task<IActionResult> BlockCountryTemporarily([FromBody] TemporalBlockRequest request)
    {
        var response = await blockedCountryService.BlockCountryTemporarilyAsync(request);
        return StatusCode(response.StatusCode, response);
    }
}