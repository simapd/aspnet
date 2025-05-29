using Microsoft.EntityFrameworkCore;
using Simapd.Models;

namespace Simapd.Repositories
{
    class RiskAreaRepository : IRiskAreaRepository
    {
        private readonly SimapdDb _db;

        public RiskAreaRepository(SimapdDb db)
        {
          this._db = db;
        }

        public async Task<PagedResponse<RiskArea>> ListPagedAsync(int pageNumber, int pageSize)
        {
          var totalRecords = await _db.RiskArea.AsNoTracking().CountAsync();

          var riskAreas = await _db.RiskArea.AsNoTracking()
              .OrderBy(x => x.Id)
              .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

          var pagedResponse = new PagedResponse<RiskArea>(riskAreas, pageNumber, pageSize, totalRecords);

          return pagedResponse;
        }
    }
}
