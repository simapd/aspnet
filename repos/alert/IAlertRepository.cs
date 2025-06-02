using Simapd.Models;

namespace Simapd.Repositories
{
  public interface IAlertRepository
  {
    Task<Alert?> FindAsync(string id);
    Task<PagedResponse<Alert>> ListPagedAsync(string areaId, int pageNumber, int pageSize);
    Task<Alert> CreateAsync(Alert alert);
    Task UpdateAsync();
    Task DeleteAsync(Alert alert);
  }
}
