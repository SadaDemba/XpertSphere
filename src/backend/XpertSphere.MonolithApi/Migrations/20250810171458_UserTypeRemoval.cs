using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class UserTypeRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_UserType",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Internal_OrganizationRequired",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserType",
                table: "Users",
                column: "UserType");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Internal_OrganizationRequired",
                table: "Users",
                sql: "([UserType] = 'RECRUITER' AND [OrganizationId] IS NOT NULL) OR([UserType] = 'MANAGER' AND [OrganizationId] IS NOT NULL) OR([UserType] = 'COLLABORATOR' AND [OrganizationId] IS NOT NULL)OR ([UserType] = 'CANDIDATE' AND [OrganizationId] IS NULL)");
        }
    }
}
