using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOfferEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WorkMode = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    SalaryMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalaryMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalaryCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobOffers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOffers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOffers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOffers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_CreatedBy",
                table: "JobOffers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_CreatedByUserId",
                table: "JobOffers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_OrganizationId",
                table: "JobOffers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_UpdatedBy",
                table: "JobOffers",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobOffers");
        }
    }
}
