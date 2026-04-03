using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abdullhak_Khalaf.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppFileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "AppFiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "AppFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "AppFiles");

            migrationBuilder.DropColumn(
                name: "OwnerEmail",
                table: "AppFiles");
        }
    }
}
