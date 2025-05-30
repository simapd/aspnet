namespace Simapd.Dtos
{
    public record RiskAreaDto
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    public record RiskAreaRequestDto
    {
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
