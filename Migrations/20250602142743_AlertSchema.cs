using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimapdApi.Migrations
{
    /// <inheritdoc />
    public partial class AlertSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_alerts_area_id",
                table: "alerts",
                column: "area_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");
        }
    }
}
