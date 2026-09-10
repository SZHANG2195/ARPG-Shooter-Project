using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShooterARPGProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeName",
                table: "StatDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBaseStatEntity",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", nullable: false),
                    StatId = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBaseStatEntity", x => new { x.CharacterId, x.StatId });
                    table.ForeignKey(
                        name: "FK_CharacterBaseStatEntity_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterBaseStatEntity_StatDefinitions_StatId",
                        column: x => x.StatId,
                        principalTable: "StatDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStartingResourceEntity",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", nullable: false),
                    StatId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStartingResourceEntity", x => new { x.CharacterId, x.StatId });
                    table.ForeignKey(
                        name: "FK_CharacterStartingResourceEntity_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterStartingResourceEntity_StatDefinitions_StatId",
                        column: x => x.StatId,
                        principalTable: "StatDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBaseStatEntity_StatId",
                table: "CharacterBaseStatEntity",
                column: "StatId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStartingResourceEntity_StatId",
                table: "CharacterStartingResourceEntity",
                column: "StatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterBaseStatEntity");

            migrationBuilder.DropTable(
                name: "CharacterStartingResourceEntity");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropColumn(
                name: "CodeName",
                table: "StatDefinitions");
        }
    }
}
