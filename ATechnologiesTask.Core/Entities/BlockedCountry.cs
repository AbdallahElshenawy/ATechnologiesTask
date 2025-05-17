namespace ATechnologiesTask.Core.Entities
{
    public class BlockedCountry
    {
        public string CountryCode { get; set; } = string.Empty;
        public string? CountryName { get; set; }
        public DateTime BlockedAt { get; set; }
    }
}
