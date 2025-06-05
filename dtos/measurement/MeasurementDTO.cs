using Simapd.Models;

namespace Simapd.Dtos
{
    public record MeasurementDto
    {
        public required string Id { get; init; }
        public required MeasurementType type { get; init; }
        public required string value { get; init; }
        public required DateTime MeasuredAt { get; init; }
        public required RiskLevel RiskLevel { get; init; }
        public required string SensorId { get; init; }
        public required string AreaId { get; init; }
    }

    public record MeasurementRequestDto
    {
        public required MeasurementType type { get; set; }
        public required string value { get; set; }
        public DateTime? MeasuredAt { get; set; }
        public required RiskLevel RiskLevel { get; set; }
        public required string SensorId { get; set; }
        public required string AreaId { get; set; }
    }
}
