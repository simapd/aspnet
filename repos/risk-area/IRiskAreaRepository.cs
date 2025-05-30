using Simapd.Models;

namespace Simapd.Repositories
{
  public interface IRiskAreaRepository
  {
    Task<RiskArea?> FindAsync(string id);
    Task<PagedResponse<RiskArea>> ListPagedAsync(int pageNumber, int pageSize);
    Task<RiskArea> CreateAsync(RiskArea riskArea);
    Task UpdateAsync();
  }
}
