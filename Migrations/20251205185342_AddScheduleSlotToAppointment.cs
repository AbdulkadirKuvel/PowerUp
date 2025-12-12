using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerUp.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleSlotToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleSlotId",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduleSlotId",
                table: "Appointments",
                column: "ScheduleSlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_ScheduleSlots_ScheduleSlotId",
                table: "Appointments",
                column: "ScheduleSlotId",
                principalTable: "ScheduleSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_ScheduleSlots_ScheduleSlotId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ScheduleSlotId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ScheduleSlotId",
                table: "Appointments");
        }
    }
}
