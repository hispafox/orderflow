using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAddBuyerIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1 (Expand) del rename CustomerId → BuyerId:
            //   1. Añadir BuyerId como nullable
            //   2. Copiar datos atómicamente (UPDATE SET BuyerId = CustomerId)
            //   3. Hacer NOT NULL tras el copy
            // CustomerId se mantiene — el código viejo la sigue usando.
            // La fase Contract (DropColumn CustomerId) llega en una release posterior,
            // tras ≥1 semana de verificación en producción.

            migrationBuilder.AddColumn<System.Guid>(
                name:     "BuyerId",
                schema:   "orders",
                table:    "Orders",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE orders.Orders SET BuyerId = CustomerId WHERE BuyerId IS NULL");

            migrationBuilder.AlterColumn<System.Guid>(
                name:        "BuyerId",
                schema:      "orders",
                table:       "Orders",
                nullable:    false,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name:   "BuyerId",
                schema: "orders",
                table:  "Orders");
        }
    }
}
