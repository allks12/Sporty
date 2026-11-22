using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporty.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupOrganizer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "Groups",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizerId",
                table: "Groups",
                column: "OrganizerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_AspNetUsers_OrganizerId",
                table: "Groups",
                column: "OrganizerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_AspNetUsers_OrganizerId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_OrganizerId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Groups");
        }
    }
}
