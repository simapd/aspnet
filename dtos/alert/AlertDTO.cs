using Simapd.Models;

namespace Simapd.Dtos
{
    public record AlertDto
    {
        public required string Id { get; init; }
        public required string Message { get; init; }
        public RiskLevel Level { get; init; }
        public AlertOrigin Origin { get; init; }
        public DateTime EmmitedAt  { get; init; }
        public required RiskAreaDto Area { get; set; }
    }

    public record AlertRequestDto
    {
        public string? Message { get; set; }
        public RiskLevel? Level { get; set; }
        public AlertOrigin? Origin { get; set; }
        public DateTime? EmmitedAt  { get; set; }
    }
}
