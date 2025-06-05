using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimapdApi.Migrations
{
    /// <inheritdoc />
    public partial class FixMeasurementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_measurements_risk_areas_AreaId1",
                table: "measurements");

            migrationBuilder.DropForeignKey(
                name: "FK_measurements_sensors_SensorId1",
                table: "measurements");

            migrationBuilder.DropIndex(
                name: "IX_measurements_AreaId1",
                table: "measurements");

            migrationBuilder.DropIndex(
                name: "IX_measurements_SensorId1",
                table: "measurements");

            migrationBuilder.DropColumn(
                name: "AreaId1",
                table: "measurements");

            migrationBuilder.DropColumn(
                name: "SensorId1",
                table: "measurements");

            migrationBuilder.AlterColumn<string>(
                name: "sensor_id",
                table: "measurements",
                type: "text",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<string>(
                name: "area_id",
                table: "measurements",
                type: "text",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_area_id",
                table: "measurements",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_sensor_id",
                table: "measurements",
                column: "sensor_id");

            migrationBuilder.AddForeignKey(
                name: "FK_measurements_risk_areas_area_id",
                table: "measurements",
                column: "area_id",
                principalTable: "risk_areas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_measurements_sensors_sensor_id",
                table: "measurements",
                column: "sensor_id",
                principalTable: "sensors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_measurements_risk_areas_area_id",
                table: "measurements");

            migrationBuilder.DropForeignKey(
                name: "FK_measurements_sensors_sensor_id",
                table: "measurements");

            migrationBuilder.DropIndex(
                name: "IX_measurements_area_id",
                table: "measurements");

            migrationBuilder.DropIndex(
                name: "IX_measurements_sensor_id",
                table: "measurements");

            migrationBuilder.AlterColumn<double>(
                name: "sensor_id",
                table: "measurements",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<double>(
                name: "area_id",
                table: "measurements",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AreaId1",
                table: "measurements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SensorId1",
                table: "measurements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_measurements_AreaId1",
                table: "measurements",
                column: "AreaId1");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_SensorId1",
                table: "measurements",
                column: "SensorId1");

            migrationBuilder.AddForeignKey(
                name: "FK_measurements_risk_areas_AreaId1",
                table: "measurements",
                column: "AreaId1",
                principalTable: "risk_areas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_measurements_sensors_SensorId1",
                table: "measurements",
                column: "SensorId1",
                principalTable: "sensors",
                principalColumn: "id");
        }
    }
}
