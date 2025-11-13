using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightsConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeightsConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChangeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightsConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightsConfigs_IsActive",
                table: "WeightsConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WeightsConfigs_Version",
                table: "WeightsConfigs",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeightsConfigs");
        }
    }
}
