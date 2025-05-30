using Microsoft.EntityFrameworkCore;
using Simapd.Models;

namespace Simapd.Repositories
{
    class SensorRepository : ISensorRepository
    {
        private readonly SimapdDb _db;

        public SensorRepository(SimapdDb db)
        {
          this._db = db;
        }

        public async Task<Sensor> CreateAsync(Sensor sensor)
        {
            _db.Sensor.Add(sensor);
            await _db.SaveChangesAsync();

            return sensor;
        }

        public async Task<Sensor?> FindAsync(string id)
        {
            return await _db.Sensor.Include(s => s.Area).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PagedResponse<Sensor>> ListPagedAsync(string areaId, int pageNumber, int pageSize)
        {
          var totalRecords = await _db.Sensor.Where(s => s.AreaId == areaId).AsNoTracking().CountAsync();

          var sensors = await _db.Sensor.AsNoTracking()
              .OrderBy(x => x.Id)
              .Where(s => s.AreaId == areaId)
              .Include(s => s.Area)
              .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

          var pagedResponse = new PagedResponse<Sensor>(sensors, pageNumber, pageSize, totalRecords);

          return pagedResponse;
        }

        public async Task UpdateAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Sensor sensor)
        {
            _db.Sensor.Remove(sensor);
            await _db.SaveChangesAsync();
        }
    }
}
