using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FraudDetection.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLargeAmountThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "large_amount_thresholds",
                columns: table => new
                {
                    Category = table.Column<string>(type: "VARCHAR(30)", maxLength: 30, nullable: false),
                    ThresholdAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_large_amount_thresholds", x => x.Category);
                });

            migrationBuilder.InsertData(
                table: "large_amount_thresholds",
                columns: new[] { "Category", "ThresholdAmount" },
                values: new object[,]
                {
                    { "Deposit", 20000m },
                    { "Purchase", 5000m },
                    { "Refund", 3000m },
                    { "Transfer", 10000m },
                    { "Withdrawal", 2000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "large_amount_thresholds");
        }
    }
}
