using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerUp.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleSlotHourlyStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "ScheduleSlots");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "ScheduleSlots");

            migrationBuilder.AddColumn<int>(
                name: "GymId",
                table: "ScheduleSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Hour",
                table: "ScheduleSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_GymId",
                table: "ScheduleSlots",
                column: "GymId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlots_Gyms_GymId",
                table: "ScheduleSlots",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlots_Gyms_GymId",
                table: "ScheduleSlots");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlots_GymId",
                table: "ScheduleSlots");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "ScheduleSlots");

            migrationBuilder.DropColumn(
                name: "Hour",
                table: "ScheduleSlots");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "ScheduleSlots",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "ScheduleSlots",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }
    }
}
