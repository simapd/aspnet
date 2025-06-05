using Microsoft.EntityFrameworkCore;

namespace Simapd.Models
{
    public class SimapdDb: DbContext
    {
      public SimapdDb(DbContextOptions<SimapdDb> options) : base(options)
      {
      }

      public DbSet<RiskArea> RiskArea => Set<RiskArea>();
      public DbSet<Sensor> Sensor => Set<Sensor>();
      public DbSet<Alert> Alert => Set<Alert>();
      public DbSet<Measurement> Measurement => Set<Measurement>();
    }
}
