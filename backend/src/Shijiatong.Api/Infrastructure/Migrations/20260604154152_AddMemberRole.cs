using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shijiatong.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "members",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "member");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "members");
        }
    }
}
