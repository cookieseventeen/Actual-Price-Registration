using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shijiatong.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crawl_tasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DistrictName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Records = table.Column<int>(type: "integer", nullable: false),
                    LastRun = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Next = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crawl_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Zone = table.Column<string>(type: "text", nullable: false),
                    AvgUnitPrice = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ChangePct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Volume = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Plan = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Community = table.Column<string>(type: "text", nullable: false),
                    DistrictId = table.Column<string>(type: "character varying(32)", nullable: false),
                    Road = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Ping = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Floor = table.Column<string>(type: "text", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Layout = table.Column<string>(type: "text", nullable: false),
                    Rooms = table.Column<int>(type: "integer", nullable: false),
                    Trade = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CrawledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Lng = table.Column<double>(type: "double precision", nullable: false),
                    Parking = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_members_Email",
                table: "members",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Community",
                table: "transactions",
                column: "Community");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Date",
                table: "transactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_DistrictId",
                table: "transactions",
                column: "DistrictId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crawl_tasks");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "districts");
        }
    }
}
