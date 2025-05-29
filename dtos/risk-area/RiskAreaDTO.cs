namespace Simapd.Dtos
{
    public record RiskAreaDto
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }
}
