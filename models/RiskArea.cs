using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Visus.Cuid;

namespace Simapd.Models
{
  [Table("risk_areas")]
  public class RiskArea
  {
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; private set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("latitude")]
    public double Latitude { get; init; }

    [Column("longitude")]
    public double Longitude { get; init; }

    public RiskArea() {
        Id = new Cuid2().ToString();
    }
  }
}
