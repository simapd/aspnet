using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Visus.Cuid;

namespace Simapd.Models
{
  public enum AlertLevel {
     LOW,
     MEDIUM,
     HIGH
  }

  public enum AlertOrigin {
    MANUAL,
    AUTOMATIC
  }

  [Table("alerts")]
  public class Alert
  {
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; private set; }

    [Column("message")]
    public required string Message { get; set; }

    [Column("level")]
    public AlertLevel Level { get; set; }

    [Column("origin")]
    public AlertOrigin Origin { get; set; }

    [Column("emmited_at")]
    public DateTime EmmitedAt { get; set; }

    [Column("area_id")]
    public required string AreaId { get; set; }
    public required RiskArea Area { get; init; }

    public Alert() {
        Id = new Cuid2().ToString();
    }
  }
}
