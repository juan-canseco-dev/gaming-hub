using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPresences",
                schema: "GameHub",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastActive = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPresences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPresences_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "GameHub",
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [GameHub].[UserPresences] ([Id], [UserId], [LastActive])
                SELECT NEWID(), [Id], [CreatedAt]
                FROM [GameHub].[UserProfiles];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserPresences_LastActive",
                schema: "GameHub",
                table: "UserPresences",
                column: "LastActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserPresences_UserId",
                schema: "GameHub",
                table: "UserPresences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPresences",
                schema: "GameHub");
        }
    }
}
