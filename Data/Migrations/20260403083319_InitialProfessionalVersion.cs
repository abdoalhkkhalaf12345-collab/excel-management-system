using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abdullhak_Khalaf.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialProfessionalVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerFullName",
                table: "AppFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserName",
                table: "AppFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnerFullName",
                table: "AppFiles");

            migrationBuilder.DropColumn(
                name: "OwnerUserName",
                table: "AppFiles");
        }
    }
}
