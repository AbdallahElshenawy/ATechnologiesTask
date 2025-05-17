using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace ATechnologiesTask.API.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController(IBlockedCountryService blockedCountryService) : ControllerBase
{
    [HttpGet("blocked-attempts")]
    [SwaggerOperation(Summary = "List blocked IP attempts", Description = "Returns a paginated list of blocked IP access attempts, including IP, timestamp, country code, status, and UserAgent.")]
    public async Task<IActionResult> GetBlockedAttempts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = await blockedCountryService.GetBlockedAttemptsAsync(page, pageSize);
        return StatusCode(response.StatusCode, response);
    }
}