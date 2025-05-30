namespace Simapd.Dtos
{
    public record SensorDto
    {
        public required string Id { get; init; }
        public string? Description { get; init; }
        public DateTime InstalledAt { get; init; }
        public DateTime? MaintainedAt { get; init; }
        public required RiskAreaDto Area { get; set; }
    }
}
