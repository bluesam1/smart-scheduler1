using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Location_Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Location_Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Location_Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Location_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location_State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location_PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Location_Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location_FormattedAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Location_PlaceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredSkills = table.Column<string>(type: "text", nullable: false),
                    ServiceWindow_Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceWindow_End = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AccessNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tools = table.Column<string>(type: "text", nullable: true),
                    DesiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAssignments", x => new { x.Id, x.JobId });
                    table.ForeignKey(
                        name: "FK_JobAssignments_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_JobId",
                table: "JobAssignments",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobAssignments");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
