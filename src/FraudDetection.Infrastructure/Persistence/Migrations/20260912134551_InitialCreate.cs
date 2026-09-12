using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudDetection.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transaction_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "UUID", nullable: false),
                    AccountId = table.Column<Guid>(type: "UUID", nullable: false),
                    Category = table.Column<string>(type: "VARCHAR(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "VARCHAR(3)", maxLength: 3, nullable: false),
                    MerchantName = table.Column<string>(type: "VARCHAR(200)", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TIMESTAMP WITH TIME ZONE", nullable: false),
                    IngestedAtUtc = table.Column<DateTime>(type: "TIMESTAMP WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_flags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "UUID", nullable: false),
                    TransactionEventId = table.Column<Guid>(type: "UUID", nullable: false),
                    RuleName = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "VARCHAR(1000)", maxLength: 1000, nullable: false),
                    FlaggedAtUtc = table.Column<DateTime>(type: "TIMESTAMP WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_flags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fraud_flags_transaction_events_TransactionEventId",
                        column: x => x.TransactionEventId,
                        principalTable: "transaction_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fraud_flags_TransactionEventId",
                table: "fraud_flags",
                column: "TransactionEventId");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_events_AccountId",
                table: "transaction_events",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_events_AccountId_OccurredAtUtc",
                table: "transaction_events",
                columns: new[] { "AccountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_events_OccurredAtUtc",
                table: "transaction_events",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fraud_flags");

            migrationBuilder.DropTable(
                name: "transaction_events");
        }
    }
}
