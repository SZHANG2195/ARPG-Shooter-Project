using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShooterARPGProject.Migrations
{
    /// <inheritdoc />
    public partial class AddTagMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TagEntity",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsPlayerVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagEntity", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatTagEntity_Tag",
                table: "StatTagEntity",
                column: "Tag");

            migrationBuilder.AddForeignKey(
                name: "FK_StatTagEntity_TagEntity_Tag",
                table: "StatTagEntity",
                column: "Tag",
                principalTable: "TagEntity",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatTagEntity_TagEntity_Tag",
                table: "StatTagEntity");

            migrationBuilder.DropTable(
                name: "TagEntity");

            migrationBuilder.DropIndex(
                name: "IX_StatTagEntity_Tag",
                table: "StatTagEntity");
        }
    }
}
