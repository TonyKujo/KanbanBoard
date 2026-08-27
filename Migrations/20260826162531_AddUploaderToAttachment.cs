using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddUploaderToAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoardUsers_UserId",
                table: "BoardUsers");

            migrationBuilder.AddColumn<int>(
                name: "UploaderId",
                table: "Attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BoardUsers_UserId_BoardId",
                table: "BoardUsers",
                columns: new[] { "UserId", "BoardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UploaderId",
                table: "Attachments",
                column: "UploaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_BoardUsers_UploaderId",
                table: "Attachments",
                column: "UploaderId",
                principalTable: "BoardUsers",
                principalColumn: "BoardUserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_BoardUsers_UploaderId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_BoardUsers_UserId_BoardId",
                table: "BoardUsers");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_UploaderId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "UploaderId",
                table: "Attachments");

            migrationBuilder.CreateIndex(
                name: "IX_BoardUsers_UserId",
                table: "BoardUsers",
                column: "UserId");
        }
    }
}
