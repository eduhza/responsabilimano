using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResponsabiliMano.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "min_value",
                table: "goal_fields",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "max_value",
                table: "goal_fields",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                table: "check_in_metrics",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "goal_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    baseline = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_targets", x => x.id);
                    table.ForeignKey(
                        name: "FK_goal_targets_goal_fields_goal_field_id",
                        column: x => x.goal_field_id,
                        principalTable: "goal_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goal_targets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_goal_targets_goal_field_id",
                table: "goal_targets",
                column: "goal_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_goal_targets_goal_field_id_user_id",
                table: "goal_targets",
                columns: new[] { "goal_field_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goal_targets_user_id",
                table: "goal_targets",
                column: "user_id");

            // Migrate the existing single shared target into a creator and a partner row.
            migrationBuilder.Sql(@"
                INSERT INTO goal_targets (id, goal_field_id, user_id, baseline, target_value, direction)
                SELECT gen_random_uuid(), gf.id, p.creator_id, NULL, gf.target_value, 2
                FROM goal_fields gf
                JOIN projects p ON p.id = gf.project_id
                WHERE gf.target_value IS NOT NULL");

            migrationBuilder.Sql(@"
                INSERT INTO goal_targets (id, goal_field_id, user_id, baseline, target_value, direction)
                SELECT gen_random_uuid(), gf.id, p.partner_id, NULL, gf.target_value, 2
                FROM goal_fields gf
                JOIN projects p ON p.id = gf.project_id
                WHERE gf.target_value IS NOT NULL AND p.partner_id IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "target_value",
                table: "goal_fields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "min_value",
                table: "goal_fields",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "max_value",
                table: "goal_fields",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "target_value",
                table: "goal_fields",
                type: "numeric",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE goal_fields gf
                SET target_value = (
                    SELECT gt.target_value
                    FROM goal_targets gt
                    JOIN projects p ON p.id = gf.project_id
                    WHERE gt.goal_field_id = gf.id AND gt.user_id = p.creator_id
                    LIMIT 1
                )");

            migrationBuilder.DropTable(
                name: "goal_targets");

            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                table: "check_in_metrics",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);
        }
    }
}
