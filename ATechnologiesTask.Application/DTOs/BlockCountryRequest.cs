using System.ComponentModel.DataAnnotations;

namespace ATechnologiesTask.Application.DTOs
{
    public class BlockCountryRequest
    {
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters (ISO 3166-1 alpha-2).")]
        public string CountryCode { get; set; } = string.Empty;
        public string? CountryName { get; set; } 

    }
}
