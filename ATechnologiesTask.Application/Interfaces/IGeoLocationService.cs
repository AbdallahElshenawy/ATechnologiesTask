using ATechnologiesTask.Application.DTOs;

namespace ATechnologiesTask.Application.Interfaces;

public interface IGeoLocationService
{
    Task<GeoLocationResponse?> GetGeoLocationAsync(string ipAddress);
}