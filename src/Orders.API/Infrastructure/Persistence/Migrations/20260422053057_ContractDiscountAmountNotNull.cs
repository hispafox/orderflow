using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContractDiscountAmountNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 3 (Contract) — solo después de que backfill haya completado en staging
            // Backfill previo: scripts/backfill_discount_amount.sql
            migrationBuilder.AlterColumn<decimal>(
                name:         "DiscountAmount",
                schema:       "orders",
                table:        "Orders",
                type:         "decimal(18,2)",
                nullable:     false,
                defaultValue: 0m,
                oldNullable:  true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name:        "DiscountAmount",
                schema:      "orders",
                table:       "Orders",
                type:        "decimal(18,2)",
                nullable:    true,
                oldNullable: false);
        }
    }
}
