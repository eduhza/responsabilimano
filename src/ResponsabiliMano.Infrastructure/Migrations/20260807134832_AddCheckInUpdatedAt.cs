using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResponsabiliMano.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "check_ins",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "check_ins");
        }
    }
}
