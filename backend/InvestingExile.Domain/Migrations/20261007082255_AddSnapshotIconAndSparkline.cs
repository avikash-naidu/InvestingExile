using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestingExile.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotIconAndSparkline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "PriceSnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal[]>(
                name: "Sparkline",
                table: "PriceSnapshots",
                type: "numeric[]",
                nullable: false,
                defaultValue: new decimal[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "PriceSnapshots");

            migrationBuilder.DropColumn(
                name: "Sparkline",
                table: "PriceSnapshots");
        }
    }
}
