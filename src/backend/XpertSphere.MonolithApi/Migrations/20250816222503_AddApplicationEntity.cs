using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Organizations_OrganizationId",
                table: "JobOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Users_CreatedBy",
                table: "JobOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Users_CreatedByUserId",
                table: "JobOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Users_UpdatedBy",
                table: "JobOffers");

            migrationBuilder.AlterColumn<string>(
                name: "WorkMode",
                table: "JobOffers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "JobOffers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ContractType",
                table: "JobOffers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverLetter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.CheckConstraint("CK_Application_AppliedAt", "[AppliedAt] <= GETUTCDATE()");
                    table.CheckConstraint("CK_Application_LastUpdatedAt", "[LastUpdatedAt] IS NULL OR [LastUpdatedAt] >= [AppliedAt]");
                    table.CheckConstraint("CK_Application_Rating", "[Rating] IS NULL OR ([Rating] >= 1 AND [Rating] <= 5)");
                    table.ForeignKey(
                        name: "FK_Applications_JobOffers_JobOfferId",
                        column: x => x.JobOfferId,
                        principalTable: "JobOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Users_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatusHistories", x => x.Id);
                    table.CheckConstraint("CK_ApplicationStatusHistory_Rating", "[Rating] IS NULL OR ([Rating] >= 1 AND [Rating] <= 5)");
                    table.CheckConstraint("CK_ApplicationStatusHistory_UpdatedAt", "[UpdatedAt] <= DATEADD(minute, 5, GETUTCDATE())");
                    table.ForeignKey(
                        name: "FK_ApplicationStatusHistories_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusHistories_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ContractType",
                table: "JobOffers",
                column: "ContractType");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_CreatedAt",
                table: "JobOffers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ExpiresAt",
                table: "JobOffers",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Location",
                table: "JobOffers",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_OrganizationId_Status",
                table: "JobOffers",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_PublishedAt",
                table: "JobOffers",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Status",
                table: "JobOffers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Status_PublishedAt",
                table: "JobOffers",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Title",
                table: "JobOffers",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_WorkMode",
                table: "JobOffers",
                column: "WorkMode");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JobOffer_ExpiresAt",
                table: "JobOffers",
                sql: "[ExpiresAt] IS NULL OR [ExpiresAt] > [CreatedAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JobOffer_SalaryRange",
                table: "JobOffers",
                sql: "[SalaryMin] IS NULL OR [SalaryMax] IS NULL OR [SalaryMin] <= [SalaryMax]");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AppliedAt",
                table: "Applications",
                column: "AppliedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateId",
                table: "Applications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateId_Status",
                table: "Applications",
                columns: new[] { "CandidateId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CreatedAt",
                table: "Applications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CreatedBy",
                table: "Applications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CurrentStatus",
                table: "Applications",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobOfferId",
                table: "Applications",
                column: "JobOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobOfferId_CandidateId",
                table: "Applications",
                columns: new[] { "JobOfferId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobOfferId_Status",
                table: "Applications",
                columns: new[] { "JobOfferId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_LastUpdatedAt",
                table: "Applications",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Rating",
                table: "Applications",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UpdatedBy",
                table: "Applications",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId",
                table: "ApplicationStatusHistories",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId_Status",
                table: "ApplicationStatusHistories",
                columns: new[] { "ApplicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId_UpdatedAt",
                table: "ApplicationStatusHistories",
                columns: new[] { "ApplicationId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_Rating",
                table: "ApplicationStatusHistories",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_Status",
                table: "ApplicationStatusHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_UpdatedAt",
                table: "ApplicationStatusHistories",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_UpdatedByUserId",
                table: "ApplicationStatusHistories",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Organizations_OrganizationId",
                table: "JobOffers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Users_CreatedBy",
                table: "JobOffers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Users_CreatedByUserId",
                table: "JobOffers",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Users_UpdatedBy",
                table: "JobOffers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Organizations_OrganizationId",
                table: "JobOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Users_CreatedBy",
                table: "JobOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Users_CreatedByUserId",
                table: "JobOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOffers_Users_UpdatedBy",
                table: "JobOffers");

            migrationBuilder.DropTable(
                name: "ApplicationStatusHistories");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_ContractType",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_CreatedAt",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_ExpiresAt",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Location",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_OrganizationId_Status",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_PublishedAt",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Status",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Status_PublishedAt",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Title",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_WorkMode",
                table: "JobOffers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JobOffer_ExpiresAt",
                table: "JobOffers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JobOffer_SalaryRange",
                table: "JobOffers");

            migrationBuilder.AlterColumn<int>(
                name: "WorkMode",
                table: "JobOffers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "JobOffers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "ContractType",
                table: "JobOffers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Organizations_OrganizationId",
                table: "JobOffers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Users_CreatedBy",
                table: "JobOffers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Users_CreatedByUserId",
                table: "JobOffers",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOffers_Users_UpdatedBy",
                table: "JobOffers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
