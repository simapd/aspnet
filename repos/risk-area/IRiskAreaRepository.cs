using Simapd.Models;

namespace Simapd.Repositories
{
  public interface IRiskAreaRepository
  {
    Task<PagedResponse<RiskArea>> ListPagedAsync(int pageNumber, int pageSize);
  }
}
