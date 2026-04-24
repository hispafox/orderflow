using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxState_BusName_Created",
                schema: "orders",
                table: "OutboxState");

            migrationBuilder.DropColumn(
                name: "BusName",
                schema: "orders",
                table: "OutboxState");

            migrationBuilder.CreateTable(
                name: "DemoEventLog",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DestinationAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SourceAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoEventLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoEventLog_CorrelationId",
                schema: "orders",
                table: "DemoEventLog",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoEventLog_OccurredAt",
                schema: "orders",
                table: "DemoEventLog",
                column: "OccurredAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoEventLog",
                schema: "orders");

            migrationBuilder.AddColumn<string>(
                name: "BusName",
                schema: "orders",
                table: "OutboxState",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_BusName_Created",
                schema: "orders",
                table: "OutboxState",
                columns: new[] { "BusName", "Created" });
        }
    }
}
