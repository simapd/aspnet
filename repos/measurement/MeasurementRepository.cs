using Microsoft.EntityFrameworkCore;
using Simapd.Models;

namespace Simapd.Repositories
{
    class MeasurementRepository : IMeasurementRepository
    {
        private readonly SimapdDb _db;

        public MeasurementRepository(SimapdDb db)
        {
          this._db = db;
        }

        public async Task<Measurement> CreateAsync(Measurement measurement)
        {
            _db.Measurement.Add(measurement);
            await _db.SaveChangesAsync();

            return measurement;
        }

        public async Task<Measurement?> FindAsync(string id)
        {
            return await _db.Measurement.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PagedResponse<Measurement>> ListPagedAsync(int pageNumber, int pageSize, string? areaId, string? sensorId)
        {
          var query = _db.Measurement.AsQueryable();

          if (areaId is not null) {
              query = query.Where(m => m.AreaId == areaId);
          }

          if (sensorId is not null) {
              query = query.Where(m => m.SensorId == sensorId);
          }

          var totalRecords = await query.AsNoTracking().CountAsync();

          var measurements = await query.AsNoTracking()
              .OrderBy(x => x.MeasuredAt)
              .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

          var pagedResponse = new PagedResponse<Measurement>(measurements, pageNumber, pageSize, totalRecords);

          return pagedResponse;
        }

        public async Task UpdateAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Measurement measurement)
        {
            _db.Measurement.Remove(measurement);
            await _db.SaveChangesAsync();
        }
    }
}
