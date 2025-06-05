using Simapd.Models;

namespace Simapd.Repositories
{
  public interface IMeasurementRepository
  {
    Task<Measurement?> FindAsync(string id);
    Task<PagedResponse<Measurement>> ListPagedAsync(int pageNumber, int pageSize, string? areaId, string? sensorId);
    Task<Measurement> CreateAsync(Measurement measurement);
    Task UpdateAsync();
    Task DeleteAsync(Measurement measurement);
  }
}
