using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shijiatong.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLvrStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lvr_import_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FromCache = table.Column<bool>(type: "boolean", nullable: false),
                    RawRows = table.Column<int>(type: "integer", nullable: false),
                    Malformed = table.Column<int>(type: "integer", nullable: false),
                    Inserted = table.Column<int>(type: "integer", nullable: false),
                    Updated = table.Column<int>(type: "integer", nullable: false),
                    Unchanged = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lvr_import_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lvr_staging_records",
                columns: table => new
                {
                    SerialNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    County = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Kind = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    District = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Fields = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FirstSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FirstImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRunId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lvr_staging_records", x => x.SerialNo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lvr_import_runs_StartedAt",
                table: "lvr_import_runs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_lvr_staging_records_District",
                table: "lvr_staging_records",
                column: "District");

            migrationBuilder.CreateIndex(
                name: "IX_lvr_staging_records_LastRunId",
                table: "lvr_staging_records",
                column: "LastRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lvr_import_runs");

            migrationBuilder.DropTable(
                name: "lvr_staging_records");
        }
    }
}
