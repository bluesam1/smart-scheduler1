using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseLocation_Latitude = table.Column<double>(type: "double precision", nullable: false),
                    BaseLocation_Longitude = table.Column<double>(type: "double precision", nullable: false),
                    BaseLocation_Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BaseLocation_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BaseLocation_State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BaseLocation_PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BaseLocation_Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BaseLocation_FormattedAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BaseLocation_PlaceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    Calendar_Holidays = table.Column<string>(type: "text", nullable: true),
                    Calendar_Exceptions = table.Column<string>(type: "text", nullable: true),
                    Calendar_DailyBreakMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxJobsPerDay = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractorWorkingHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractorWorkingHours", x => new { x.Id, x.ContractorId });
                    table.ForeignKey(
                        name: "FK_ContractorWorkingHours_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractorWorkingHours_ContractorId",
                table: "ContractorWorkingHours",
                column: "ContractorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractorWorkingHours");

            migrationBuilder.DropTable(
                name: "Contractors");
        }
    }
}
