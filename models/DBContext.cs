using Microsoft.EntityFrameworkCore;

namespace Simapd.Models
{
    public class SimapdDb: DbContext
    {
      public SimapdDb(DbContextOptions<SimapdDb> options) : base(options)
      {
      }

      public DbSet<RiskArea> RiskArea => Set<RiskArea>();
    }
}
