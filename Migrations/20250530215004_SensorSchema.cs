using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimapdApi.Migrations
{
    /// <inheritdoc />
    public partial class SensorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_sensors_area_id",
                table: "sensors",
                column: "area_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensors");
        }
    }
}
