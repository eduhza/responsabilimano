using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResponsabiliMano.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalNegotiation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "accepted_by_creator",
                table: "goal_targets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "accepted_by_partner",
                table: "goal_targets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_proposed_at",
                table: "goal_targets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_proposed_by_user_id",
                table: "goal_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "goal_targets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                @"UPDATE goal_targets gt
                SET
                    status = CASE WHEN p.status = 1 THEN 2 ELSE 1 END,
                    accepted_by_creator = true,
                    accepted_by_partner = p.status = 1,
                    last_proposed_by_user_id = p.creator_id,
                    last_proposed_at = NOW()
                FROM goal_fields gf
                INNER JOIN projects p ON p.id = gf.project_id
                WHERE gt.goal_field_id = gf.id;");

            migrationBuilder.CreateTable(
                name: "goal_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    baseline = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_goal_proposals_goal_targets_goal_target_id",
                        column: x => x.goal_target_id,
                        principalTable: "goal_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goal_proposals_users_proposed_by_user_id",
                        column: x => x.proposed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_goal_targets_status",
                table: "goal_targets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_goal_proposals_goal_target_id_created_at",
                table: "goal_proposals",
                columns: new[] { "goal_target_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_goal_proposals_proposed_by_user_id",
                table: "goal_proposals",
                column: "proposed_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goal_proposals");

            migrationBuilder.DropIndex(
                name: "IX_goal_targets_status",
                table: "goal_targets");

            migrationBuilder.DropColumn(
                name: "accepted_by_creator",
                table: "goal_targets");

            migrationBuilder.DropColumn(
                name: "accepted_by_partner",
                table: "goal_targets");

            migrationBuilder.DropColumn(
                name: "last_proposed_at",
                table: "goal_targets");

            migrationBuilder.DropColumn(
                name: "last_proposed_by_user_id",
                table: "goal_targets");

            migrationBuilder.DropColumn(
                name: "status",
                table: "goal_targets");
        }
    }
}
