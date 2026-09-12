using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudDetection.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountHolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_holders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "UUID", nullable: false),
                    AccountId = table.Column<Guid>(type: "UUID", nullable: false),
                    FirstName = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    id_passport = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_holders", x => x.Id);
                    table.CheckConstraint("CK_account_holders_email_format", "\"email\" ~* '^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$'");
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_holders_AccountId",
                table: "account_holders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_account_holders_email",
                table: "account_holders",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_account_holders_id_passport",
                table: "account_holders",
                column: "id_passport");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_holders");
        }
    }
}
