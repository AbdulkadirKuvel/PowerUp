using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PowerUp.Migrations
{
    /// <inheritdoc />
    public partial class AddGymFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GymGymFeature",
                columns: table => new
                {
                    FeaturesId = table.Column<int>(type: "int", nullable: false),
                    GymsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymGymFeature", x => new { x.FeaturesId, x.GymsId });
                    table.ForeignKey(
                        name: "FK_GymGymFeature_GymFeatures_FeaturesId",
                        column: x => x.FeaturesId,
                        principalTable: "GymFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymGymFeature_Gyms_GymsId",
                        column: x => x.GymsId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "GymFeatures",
                columns: new[] { "Id", "IconClass", "Name" },
                values: new object[,]
                {
                    { 1, "fas fa-dumbbell", "Profesyonel Ekipmanlar" },
                    { 2, "fas fa-shower", "Duş ve Soyunma" },
                    { 3, "fas fa-wifi", "Ücretsiz Wi-Fi" },
                    { 4, "fas fa-hot-tub", "Sauna" },
                    { 5, "fas fa-parking", "Otopark" },
                    { 6, "fas fa-coffee", "Kafeterya" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymGymFeature_GymsId",
                table: "GymGymFeature",
                column: "GymsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymGymFeature");

            migrationBuilder.DropTable(
                name: "GymFeatures");
        }
    }
}
