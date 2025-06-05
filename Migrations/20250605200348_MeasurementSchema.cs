using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimapdApi.Migrations
{
    /// <inheritdoc />
    public partial class MeasurementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "measurements",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    measured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    sensor_id = table.Column<double>(type: "double precision", nullable: false),
                    SensorId1 = table.Column<string>(type: "text", nullable: true),
                    area_id = table.Column<double>(type: "double precision", nullable: false),
                    AreaId1 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurements", x => x.id);
                    table.ForeignKey(
                        name: "FK_measurements_risk_areas_AreaId1",
                        column: x => x.AreaId1,
                        principalTable: "risk_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_measurements_sensors_SensorId1",
                        column: x => x.SensorId1,
                        principalTable: "sensors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_measurements_AreaId1",
                table: "measurements",
                column: "AreaId1");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_SensorId1",
                table: "measurements",
                column: "SensorId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "measurements");
        }
    }
}
