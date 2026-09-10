using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShooterARPGProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    IsRangePaired = table.Column<bool>(type: "INTEGER", nullable: false),
                    PairedCounterpartId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatTagEntity",
                columns: table => new
                {
                    StatId = table.Column<string>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatTagEntity", x => new { x.StatId, x.Tag });
                    table.ForeignKey(
                        name: "FK_StatTagEntity_StatDefinitions_StatId",
                        column: x => x.StatId,
                        principalTable: "StatDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatTagEntity");

            migrationBuilder.DropTable(
                name: "StatDefinitions");
        }
    }
}
