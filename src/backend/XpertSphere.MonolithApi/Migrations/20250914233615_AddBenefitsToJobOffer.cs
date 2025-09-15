using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBenefitsToJobOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Benefits",
                table: "JobOffers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Benefits",
                table: "JobOffers");
        }
    }
}
