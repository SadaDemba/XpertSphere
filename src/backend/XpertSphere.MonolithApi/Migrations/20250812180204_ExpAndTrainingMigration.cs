using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class ExpAndTrainingMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Trainings_CreatedBy",
                table: "Trainings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Trainings_UpdatedBy",
                table: "Trainings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_CreatedBy",
                table: "Experiences",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UpdatedBy",
                table: "Experiences",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Experiences_Users_CreatedBy",
                table: "Experiences",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Experiences_Users_UpdatedBy",
                table: "Experiences",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainings_Users_CreatedBy",
                table: "Trainings",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainings_Users_UpdatedBy",
                table: "Trainings",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Experiences_Users_CreatedBy",
                table: "Experiences");

            migrationBuilder.DropForeignKey(
                name: "FK_Experiences_Users_UpdatedBy",
                table: "Experiences");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainings_Users_CreatedBy",
                table: "Trainings");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainings_Users_UpdatedBy",
                table: "Trainings");

            migrationBuilder.DropIndex(
                name: "IX_Trainings_CreatedBy",
                table: "Trainings");

            migrationBuilder.DropIndex(
                name: "IX_Trainings_UpdatedBy",
                table: "Trainings");

            migrationBuilder.DropIndex(
                name: "IX_Experiences_CreatedBy",
                table: "Experiences");

            migrationBuilder.DropIndex(
                name: "IX_Experiences_UpdatedBy",
                table: "Experiences");
        }
    }
}
