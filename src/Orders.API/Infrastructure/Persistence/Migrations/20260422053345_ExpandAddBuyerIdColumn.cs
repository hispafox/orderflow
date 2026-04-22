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
            //   1. Añadir BuyerId como nullable (ADITIVA — código viejo sigue funcionando)
            //   2. Copiar datos existentes (BuyerId = CustomerId)
            //
            // NO se hace NOT NULL en esta migration: los INSERTs del código v1 no
            // conocen BuyerId y SQL pondría NULL. El NOT NULL es la fase Contract
            // que se aplica DESPUÉS de que el código (Deploy 2) escriba BuyerId
            // explícitamente.
            //
            // CustomerId se mantiene — el código viejo la sigue usando.

            migrationBuilder.AddColumn<System.Guid>(
                name:     "BuyerId",
                schema:   "orders",
                table:    "Orders",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE orders.Orders SET BuyerId = CustomerId WHERE BuyerId IS NULL");
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
