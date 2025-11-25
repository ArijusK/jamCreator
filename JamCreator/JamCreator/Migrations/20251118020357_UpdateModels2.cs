using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamCreator.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientToken",
                table: "Participants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientToken",
                table: "Participants",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
