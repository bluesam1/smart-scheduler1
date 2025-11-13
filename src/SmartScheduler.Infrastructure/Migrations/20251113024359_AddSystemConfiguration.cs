using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestPayloadJson = table.Column<string>(type: "text", nullable: false),
                    CandidatesJson = table.Column<string>(type: "text", nullable: false),
                    SelectedContractorId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectionActor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ConfigVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Values = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    AuditId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JobId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractorId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_AuditRecommendations_AuditId",
                        column: x => x.AuditId,
                        principalTable: "AuditRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Assignments_AuditRecommendations_AuditId1",
                        column: x => x.AuditId1,
                        principalTable: "AuditRecommendations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assignments_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Contractors_ContractorId1",
                        column: x => x.ContractorId1,
                        principalTable: "Contractors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assignments_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Jobs_JobId1",
                        column: x => x.JobId1,
                        principalTable: "Jobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AuditId",
                table: "Assignments",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AuditId1",
                table: "Assignments",
                column: "AuditId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ContractorId",
                table: "Assignments",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ContractorId_StartUtc_EndUtc",
                table: "Assignments",
                columns: new[] { "ContractorId", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ContractorId1",
                table: "Assignments",
                column: "ContractorId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_EndUtc",
                table: "Assignments",
                column: "EndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_JobId",
                table: "Assignments",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_JobId1",
                table: "Assignments",
                column: "JobId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StartUtc",
                table: "Assignments",
                column: "StartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecommendations_CreatedAt",
                table: "AuditRecommendations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecommendations_JobId",
                table: "AuditRecommendations",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Type",
                table: "SystemConfigurations",
                column: "Type",
                unique: true);

            // Seed default job types
            var jobTypesId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var jobTypesValues = System.Text.Json.JsonSerializer.Serialize(new[] { "Hardwood Installation", "Tile Installation", "Carpet Installation", "Laminate Installation", "HVAC Repair", "Electrical Inspection", "Repair/Maintenance" });
            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "Type", "Values", "UpdatedAt", "UpdatedBy" },
                values: new object[] { jobTypesId, 1, jobTypesValues, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "System" });

            // Seed default skills
            var skillsId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var skillsValues = System.Text.Json.JsonSerializer.Serialize(new[] { "Hardwood Installation", "Tile", "Laminate", "Carpet", "Finishing", "HVAC", "Electrical", "Plumbing" });
            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "Type", "Values", "UpdatedAt", "UpdatedBy" },
                values: new object[] { skillsId, 2, skillsValues, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "System" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "AuditRecommendations");
        }
    }
}
