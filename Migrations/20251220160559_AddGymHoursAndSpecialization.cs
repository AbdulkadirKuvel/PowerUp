using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerUp.Migrations
{
    /// <inheritdoc />
    public partial class AddGymHoursAndSpecialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Trainers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ClosingTime",
                table: "Gyms",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OpeningTime",
                table: "Gyms",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "Gyms");
        }
    }
}
