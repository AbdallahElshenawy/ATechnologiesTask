namespace ATechnologiesTask.Core.Entities
{
    public class TemporalBlock
    {
        public string CountryCode { get; init; } = string.Empty;
        public DateTime BlockedUntil { get; init; }
    }
}
