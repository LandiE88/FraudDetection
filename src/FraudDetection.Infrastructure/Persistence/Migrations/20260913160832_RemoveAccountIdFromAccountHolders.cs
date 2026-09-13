using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudDetection.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccountIdFromAccountHolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_holders_AccountId",
                table: "account_holders");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "account_holders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "account_holders",
                type: "UUID",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_account_holders_AccountId",
                table: "account_holders",
                column: "AccountId");
        }
    }
}
