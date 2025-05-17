using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace ATechnologiesTask.API.Controllers;

[ApiController]
[Route("api/ip")]
public class IpController(IBlockedCountryService blockedCountryService) : ControllerBase
{

    [HttpGet("check-block")]
    [SwaggerOperation(Summary = "Check if IP's country is blocked", Description = "Checks if the caller's IP country is blocked and logs the attempt.")]
    public async Task<IActionResult> CheckIpBlock()
    {
        var response = await blockedCountryService.CheckIpBlockAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("lookup")]
    [SwaggerOperation(Summary = "Look up IP geolocation", Description = "Fetches country details for an IP address using IPGeolocation. If no IP is provided, uses the caller's IP.")]
    public async Task<IActionResult> LookupIp([FromQuery] string? ipAddress)
    {
        var response = await blockedCountryService.LookupIpAsync(ipAddress);
        return StatusCode(response.StatusCode, response);
    }
}