using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    [DbContext(typeof(SqliteServerDbContext))]
    [Migration("20261111223000_StalkerRemoveCharacterRanks")]
    public partial class StalkerRemoveCharacterRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stalker_character_ranks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stalker_character_ranks",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    character_name = table.Column<string>(type: "TEXT", nullable: false),
                    time_spent = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stalker_character_ranks", x => new { x.user_id, x.character_name });
                });
        }
    }
}
