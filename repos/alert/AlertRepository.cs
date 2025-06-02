using Microsoft.EntityFrameworkCore;
using Simapd.Models;

namespace Simapd.Repositories
{
    class AlertRepository : IAlertRepository
    {
        private readonly SimapdDb _db;

        public AlertRepository(SimapdDb db)
        {
          this._db = db;
        }

        public async Task<Alert> CreateAsync(Alert alert)
        {
            _db.Alert.Add(alert);
            await _db.SaveChangesAsync();

            return alert;
        }

        public async Task<Alert?> FindAsync(string id)
        {
            return await _db.Alert.Include(s => s.Area).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PagedResponse<Alert>> ListPagedAsync(string areaId, int pageNumber, int pageSize)
        {
          var totalRecords = await _db.Alert.Where(s => s.AreaId == areaId).AsNoTracking().CountAsync();

          var alerts = await _db.Alert.AsNoTracking()
              .OrderBy(x => x.Id)
              .Where(s => s.AreaId == areaId)
              .Include(s => s.Area)
              .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

          var pagedResponse = new PagedResponse<Alert>(alerts, pageNumber, pageSize, totalRecords);

          return pagedResponse;
        }

        public async Task UpdateAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Alert alert)
        {
            _db.Alert.Remove(alert);
            await _db.SaveChangesAsync();
        }
    }
}
