using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastRecommendationAuditIdToJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastRecommendationAuditId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_LastRecommendationAuditId",
                table: "Jobs",
                column: "LastRecommendationAuditId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_LastRecommendationAuditId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LastRecommendationAuditId",
                table: "Jobs");
        }
    }
}
