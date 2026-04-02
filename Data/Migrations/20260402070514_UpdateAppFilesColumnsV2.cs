using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abdullhak_Khalaf.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppFilesColumnsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "AppFiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "AppFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "AppFiles");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "AppFiles");
        }
    }
}
