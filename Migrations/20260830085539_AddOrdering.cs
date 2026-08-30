using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                WITH ordered AS (
                    SELECT ""StatusId"", ROW_NUMBER() OVER (PARTITION BY ""BoardId"" ORDER BY ""StatusId"") - 1 AS rn
                    FROM ""Statuses""
                )
                UPDATE ""Statuses"" s
                SET ""Order"" = ordered.rn
                FROM ordered
                WHERE s.""StatusId"" = ordered.""StatusId"";
            ");

            migrationBuilder.Sql(@"
                WITH ordered AS (
                    SELECT ""TaskId"", ROW_NUMBER() OVER (PARTITION BY ""StatusId"" ORDER BY ""CreationDate"", ""TaskId"") - 1 AS rn
                    FROM ""Tasks""
                )
                UPDATE ""Tasks"" t
                SET ""Order"" = ordered.rn
                FROM ordered
                WHERE t.""TaskId"" = ordered.""TaskId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Statuses");
        }
    }
}
