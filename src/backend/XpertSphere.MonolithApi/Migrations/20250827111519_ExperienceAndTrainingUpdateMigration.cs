using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class ExperienceAndTrainingUpdateMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeginPeriod",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "EndPeriod",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "BeginPeriod",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "EndPeriod",
                table: "Experiences");

            migrationBuilder.RenameColumn(
                name: "Institution",
                table: "Trainings",
                newName: "School");

            migrationBuilder.RenameColumn(
                name: "FieldOfStudy",
                table: "Trainings",
                newName: "Field");

            migrationBuilder.RenameColumn(
                name: "Degree",
                table: "Trainings",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "Experiences",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Experiences",
                newName: "Company");

            migrationBuilder.AddColumn<string>(
                name: "Period",
                table: "Trainings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "Experiences",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Period",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Experiences");

            migrationBuilder.RenameColumn(
                name: "School",
                table: "Trainings",
                newName: "Institution");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Trainings",
                newName: "Degree");

            migrationBuilder.RenameColumn(
                name: "Field",
                table: "Trainings",
                newName: "FieldOfStudy");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Experiences",
                newName: "CompanyName");

            migrationBuilder.RenameColumn(
                name: "Company",
                table: "Experiences",
                newName: "City");

            migrationBuilder.AddColumn<string>(
                name: "BeginPeriod",
                table: "Trainings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndPeriod",
                table: "Trainings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeginPeriod",
                table: "Experiences",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EndPeriod",
                table: "Experiences",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
