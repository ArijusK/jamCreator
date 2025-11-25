using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JamCreator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Avatar = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JamSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Genre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    Password = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mood = table.Column<int>(type: "integer", maxLength: 60, nullable: false),
                    MaxPeople = table.Column<int>(type: "integer", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    AllowSkipVote = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TempPassword = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JamSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FavoriteGenre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Avatar = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JamSessionId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_JamSessions_JamSessionId",
                        column: x => x.JamSessionId,
                        principalTable: "JamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JamSessionId = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Mood = table.Column<int>(type: "integer", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_JamSessions_JamSessionId",
                        column: x => x.JamSessionId,
                        principalTable: "JamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentAtUtc",
                table: "ChatMessages",
                column: "SentAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_JamSessions_RoomName",
                table: "JamSessions",
                column: "RoomName");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_JamSessionId_DisplayName",
                table: "Participants",
                columns: new[] { "JamSessionId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_JamSessionId_FileName",
                table: "Tracks",
                columns: new[] { "JamSessionId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UpdatedAtUtc",
                table: "UserProfiles",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "JamSessions");
        }
    }
}
