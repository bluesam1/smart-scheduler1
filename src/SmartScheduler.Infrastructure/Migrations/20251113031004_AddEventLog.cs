using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedToJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_CreatedAt",
                table: "EventLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_EventType",
                table: "EventLogs",
                column: "EventType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");
        }
    }
}
