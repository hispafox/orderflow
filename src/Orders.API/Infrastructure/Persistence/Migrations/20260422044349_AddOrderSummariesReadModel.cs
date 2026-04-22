using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSummariesReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderSummaries",
                schema: "orders",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LinesCount = table.Column<int>(type: "int", nullable: false),
                    FirstItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSummaries", x => x.OrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_CreatedAt",
                schema: "orders",
                table: "OrderSummaries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_CustomerId",
                schema: "orders",
                table: "OrderSummaries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_CustomerId_Status",
                schema: "orders",
                table: "OrderSummaries",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaries_Status",
                schema: "orders",
                table: "OrderSummaries",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSummaries",
                schema: "orders");
        }
    }
}
