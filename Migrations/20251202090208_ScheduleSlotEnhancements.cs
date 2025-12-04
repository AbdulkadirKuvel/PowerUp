using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerUp.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleSlotEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "ScheduleSlots");

            migrationBuilder.AddColumn<bool>(
                name: "IsWeekly",
                table: "ScheduleSlots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScheduleSlotServices",
                columns: table => new
                {
                    ScheduleSlotId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSlotServices", x => new { x.ScheduleSlotId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_ScheduleSlotServices_ScheduleSlots_ScheduleSlotId",
                        column: x => x.ScheduleSlotId,
                        principalTable: "ScheduleSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSlotServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlotServices_ServiceId",
                table: "ScheduleSlotServices",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleSlotServices");

            migrationBuilder.DropColumn(
                name: "IsWeekly",
                table: "ScheduleSlots");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "ScheduleSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
