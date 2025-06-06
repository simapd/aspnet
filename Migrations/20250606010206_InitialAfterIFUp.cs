using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimapdApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialAfterIFUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "risk_areas",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    emmited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    area_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_alerts_risk_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "risk_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    installed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    maintained_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    area_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensors", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensors_risk_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "risk_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "measurements",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    measured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    sensor_id = table.Column<string>(type: "text", nullable: false),
                    area_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurements", x => x.id);
                    table.ForeignKey(
                        name: "FK_measurements_risk_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "risk_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_measurements_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_area_id",
                table: "alerts",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_area_id",
                table: "measurements",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_sensor_id",
                table: "measurements",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensors_area_id",
                table: "sensors",
                column: "area_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "measurements");

            migrationBuilder.DropTable(
                name: "sensors");

            migrationBuilder.DropTable(
                name: "risk_areas");
        }
    }
}
