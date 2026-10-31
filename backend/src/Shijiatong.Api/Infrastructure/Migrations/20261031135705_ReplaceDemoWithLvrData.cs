using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shijiatong.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDemoWithLvrData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Section",
                table: "transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Community",
                table: "transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "transactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOutlier",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "districts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "ChangePct",
                table: "districts",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "districts",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "districts",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            // ── 資料：真實資料「直接取代」demo ──
            // 刪除 demo 成交（T0001~T0022；官方編號為英文字母開頭的 19 碼，不會誤刪）
            migrationBuilder.Sql(
                """DELETE FROM transactions WHERE "Id" ~ '^T[0-9]{4}$' AND "Source" = '內政部';""");
            // demo 行政區統計清掉，待 /api/admin/lvr/rebuild 以真實成交重算；
            // 29 區的名稱 / 中心點由啟動時的 DbSeeder 補齊
            migrationBuilder.Sql(
                """UPDATE districts SET "AvgUnitPrice" = 0, "ChangePct" = NULL, "Volume" = 0;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 只還原 schema；demo 資料不會復原。先清掉會違反 NOT NULL 的資料
            migrationBuilder.Sql("DELETE FROM transactions;");
            migrationBuilder.Sql("""UPDATE districts SET "ChangePct" = 0 WHERE "ChangePct" IS NULL;""");
            migrationBuilder.Sql("""UPDATE districts SET "Zone" = '' WHERE "Zone" IS NULL;""");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "IsOutlier",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "districts");

            migrationBuilder.DropColumn(
                name: "Lng",
                table: "districts");

            migrationBuilder.AlterColumn<string>(
                name: "Section",
                table: "transactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Community",
                table: "transactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "districts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ChangePct",
                table: "districts",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
