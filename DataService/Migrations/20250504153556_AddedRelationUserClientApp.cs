using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddedRelationUserClientApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "ClientApps",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringData_ClientAppId",
                table: "MonitoringData",
                column: "ClientAppId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientApps_DeviceName",
                table: "ClientApps",
                column: "DeviceName");

            migrationBuilder.AddForeignKey(
                name: "FK_MonitoringData_ClientApps_ClientAppId",
                table: "MonitoringData",
                column: "ClientAppId",
                principalTable: "ClientApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonitoringData_ClientApps_ClientAppId",
                table: "MonitoringData");

            migrationBuilder.DropIndex(
                name: "IX_MonitoringData_ClientAppId",
                table: "MonitoringData");

            migrationBuilder.DropIndex(
                name: "IX_ClientApps_DeviceName",
                table: "ClientApps");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "ClientApps",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}
