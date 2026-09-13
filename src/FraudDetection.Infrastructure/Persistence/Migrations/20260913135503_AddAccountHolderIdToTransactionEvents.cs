using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudDetection.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountHolderIdToTransactionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountHolderId",
                table: "transaction_events",
                type: "UUID",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_events_AccountHolderId",
                table: "transaction_events",
                column: "AccountHolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_transaction_events_account_holders_AccountHolderId",
                table: "transaction_events",
                column: "AccountHolderId",
                principalTable: "account_holders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transaction_events_account_holders_AccountHolderId",
                table: "transaction_events");

            migrationBuilder.DropIndex(
                name: "IX_transaction_events_AccountHolderId",
                table: "transaction_events");

            migrationBuilder.DropColumn(
                name: "AccountHolderId",
                table: "transaction_events");
        }
    }
}
