using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Visus.Cuid;

namespace Simapd.Models
{
  public enum MeasurementType {
    RAIN,
    SOIL_MOISTURE,
    MOVEMENT
  }

  [Table("measurements")]
  public class Measurement
  {
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; private set; }

    [Column("type")]
    public required MeasurementType type { get; set; }

    [Column("value")]
    public required string value { get; set; }

    [Column("measured_at")]
    public DateTime MeasuredAt { get; set; }

    [Column("risk_level")]
    public RiskLevel RiskLevel { get; set; }

    [Column("sensor_id")]
    public required string SensorId { get; set; }
    public required Sensor Sensor { get; init; }

    [Column("area_id")]
    public required string AreaId { get; set; }
    public required RiskArea Area { get; init; }

    public Measurement() {
        Id = new Cuid2().ToString();
    }
  }
}
