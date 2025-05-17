using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ATechnologiesTask.Application.Services;
using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Core.Interfaces;
using ATechnologiesTask.Application.DTOs;
using ATechnologiesTask.Core.Entities;

namespace ATechnologiesTask.Application.Tests
{
    public class BlockCountryServiceTests
    {
        private readonly Mock<IBlockedCountryRepository> _blockedRepoMock = new();
        private readonly Mock<IGeoLocationService> _geoServiceMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<ILogger<BlockedCountryService>> _loggerMock = new();

        private readonly BlockedCountryService _service;

        public BlockCountryServiceTests()
        {
            _service = new BlockedCountryService(
                _blockedRepoMock.Object,
                _geoServiceMock.Object,
                _httpContextAccessorMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task BlockCountryAsync_ShouldReturnError_WhenCountryCodeIsInvalid()
        {
            var request = new BlockCountryRequest
            {
                CountryCode = "XYZ",
                CountryName = "Invalid Country"
            };

            _geoServiceMock.Setup(g => g.GetGeoLocationAsync("XYZ")).ReturnsAsync((GeoLocationResponse?)null);

            var result = await _service.BlockCountryAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Invalid country code", result.Error);
        }

        [Fact]
        public async Task BlockCountryAsync_ShouldReturnError_WhenCountryNameDoesNotMatch()
        {
            var request = new BlockCountryRequest
            {
                CountryCode = "US",
                CountryName = "Canada"
            };

            _geoServiceMock.Setup(g => g.GetGeoLocationAsync("US")).ReturnsAsync(new GeoLocationResponse
            {
                CountryCode = "US",
                CountryName = "United States"
            });

            var result = await _service.BlockCountryAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("does not match expected", result.Error);
        }

        [Fact]
        public async Task BlockCountryAsync_ShouldReturnError_WhenCountryAlreadyBlocked()
        {
            var request = new BlockCountryRequest
            {
                CountryCode = "US",
                CountryName = "United States"
            };

            _geoServiceMock.Setup(g => g.GetGeoLocationAsync("US")).ReturnsAsync(new GeoLocationResponse
            {
                CountryCode = "US",
                CountryName = "United States"
            });

            _blockedRepoMock.Setup(r => r.IsCountryBlockedAsync("US")).ReturnsAsync(true);

            var result = await _service.BlockCountryAsync(request);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("already blocked", result.Error);
        }

        [Fact]
        public async Task BlockCountryAsync_ShouldReturnSuccess_WhenValidRequest()
        {
            var request = new BlockCountryRequest
            {
                CountryCode = "US",
                CountryName = "United States"
            };

            _geoServiceMock.Setup(g => g.GetGeoLocationAsync("US")).ReturnsAsync(new GeoLocationResponse
            {
                CountryCode = "US",
                CountryName = "United States"
            });

            _blockedRepoMock.Setup(r => r.IsCountryBlockedAsync("US")).ReturnsAsync(false);
            _blockedRepoMock.Setup(r => r.AddBlockedCountryAsync(It.IsAny<BlockedCountry>())).Returns(Task.CompletedTask);

            var result = await _service.BlockCountryAsync(request);

            Assert.True(result.Success);
            Assert.Equal(201, result.StatusCode);

            _blockedRepoMock.Verify(r => r.AddBlockedCountryAsync(It.Is<BlockedCountry>(c =>
                c.CountryCode == "US" &&
                c.CountryName == "United States"
            )), Times.Once);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UnblockCountryAsync_ReturnsBadRequest_WhenCountryCodeIsNullOrEmpty(string countryCode)
        {
            var result = await _service.UnblockCountryAsync(countryCode);
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("required", result.Error, System.StringComparison.OrdinalIgnoreCase);
        }
        [Theory]
        [InlineData("XYZ")]  
        [InlineData("A")]    
        [InlineData("ABC")]  
        public async Task UnblockCountryAsync_ReturnsBadRequest_WhenCountryCodeIsInvalid(string countryCode)
        {
            var result = await _service.UnblockCountryAsync(countryCode);
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("invalid", result.Error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UnblockCountryAsync_ReturnsNotFound_WhenCountryNotBlocked()
        {
            string countryCode = "US";

            _blockedRepoMock.Setup(r => r.RemoveBlockedCountryAsync(countryCode.ToUpper()))
                            .ReturnsAsync(false);

            var result = await _service.UnblockCountryAsync(countryCode);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Contains("not found", result.Error, System.StringComparison.OrdinalIgnoreCase);

            _blockedRepoMock.Verify(r => r.RemoveBlockedCountryAsync(countryCode.ToUpper()), Times.Once);
        }

        [Fact]
        public async Task UnblockCountryAsync_ReturnsSuccess_WhenCountryIsUnblocked()
        {
            string countryCode = "US";

            _blockedRepoMock.Setup(r => r.RemoveBlockedCountryAsync(countryCode.ToUpper()))
                            .ReturnsAsync(true);

            var result = await _service.UnblockCountryAsync(countryCode);

            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("unblocked", result.Data, System.StringComparison.OrdinalIgnoreCase);

            _blockedRepoMock.Verify(r => r.RemoveBlockedCountryAsync(countryCode.ToUpper()), Times.Once);
        }
    }
}
