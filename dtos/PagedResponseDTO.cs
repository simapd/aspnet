namespace Simapd.Dtos
{
    public record PagedResponseDto<T>
    {
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public int TotalRecords { get; init; }
        public required List<T> Data { get; init; }
    }
}
