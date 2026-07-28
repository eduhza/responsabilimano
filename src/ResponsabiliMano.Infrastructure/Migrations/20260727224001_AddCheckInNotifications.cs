using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResponsabiliMano.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "check_in_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_number = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_check_in_notifications_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_check_in_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_notifications_project_id_user_id_period_number_kind",
                table: "check_in_notifications",
                columns: new[] { "project_id", "user_id", "period_number", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_check_in_notifications_user_id",
                table: "check_in_notifications",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_notifications");
        }
    }
}
