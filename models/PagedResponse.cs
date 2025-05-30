namespace Simapd.Models
{
  public record PagedResponse<T>
  {
      public int PageNumber { get; set; }
      public int PageSize { get; set; }
      public int TotalRecords { get; set; }
      public int TotalPages { get; set; }
      public List<T> Data { get; set; }

      public PagedResponse(List<T> data, int pageNumber, int pageSize, int totalRecords)
      {
          Data = data;
          PageNumber = pageNumber;
          PageSize = pageSize;
          TotalRecords = totalRecords;
          TotalPages = (int)Math.Ceiling((decimal)totalRecords / (decimal)pageSize);
      }
  }
}
