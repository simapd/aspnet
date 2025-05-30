using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Visus.Cuid;

namespace Simapd.Models
{
  [Table("sensors")]
  public class Sensor
  {
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; private set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("installed_at")]
    public DateTime InstalledAt { get; init; }

    [Column("maintained_at")]
    public DateTime? MaintainedAt { get; init; }

    [Column("area_id")]
    public required string AreaId { get; init; }
    public required RiskArea Area { get; init; }

    public Sensor() {
        Id = new Cuid2().ToString();
    }
  }
}
