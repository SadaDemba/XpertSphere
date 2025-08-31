using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAssignationToApplicationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedManagerId",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedTechnicalEvaluatorId",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AssignedManagerId",
                table: "Applications",
                column: "AssignedManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AssignedTechnicalEvaluatorId",
                table: "Applications",
                column: "AssignedTechnicalEvaluatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Users_AssignedManagerId",
                table: "Applications",
                column: "AssignedManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Users_AssignedTechnicalEvaluatorId",
                table: "Applications",
                column: "AssignedTechnicalEvaluatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Users_AssignedManagerId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Users_AssignedTechnicalEvaluatorId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_AssignedManagerId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_AssignedTechnicalEvaluatorId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssignedManagerId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssignedTechnicalEvaluatorId",
                table: "Applications");
        }
    }
}
