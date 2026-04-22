using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAddDiscountAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1 (Expand) — columna nullable, el código viejo sigue funcionando
            migrationBuilder.AddColumn<decimal>(
                name:         "DiscountAmount",
                schema:       "orders",
                table:        "Orders",
                type:         "decimal(18,2)",
                nullable:     true,
                defaultValue: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name:   "DiscountAmount",
                schema: "orders",
                table:  "Orders");
        }
    }
}
