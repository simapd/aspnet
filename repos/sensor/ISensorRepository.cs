using Simapd.Models;

namespace Simapd.Repositories
{
  public interface ISensorRepository
  {
    Task<Sensor?> FindAsync(string id);
    Task<PagedResponse<Sensor>> ListPagedAsync(string areaId, int pageNumber, int pageSize);
    Task<Sensor> CreateAsync(Sensor sensor);
    Task UpdateAsync();
    Task DeleteAsync(Sensor sensor);
  }
}
