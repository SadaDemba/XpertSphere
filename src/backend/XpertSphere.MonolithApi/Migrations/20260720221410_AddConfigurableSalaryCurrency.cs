using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableSalaryCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: new nullable columns - no existing data to transform, NULL for every row.
            migrationBuilder.AddColumn<string>(
                name: "DesiredSalaryCurrency",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Organizations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            // Step 2: normalize any historical JobOffers.SalaryCurrency value that would not survive
            // the strict Currency converter (Extensions/EnumExtensions.ToCurrency throws on anything
            // outside {EUR, XOF}), *before* the column becomes NOT NULL below. On the current state
            // of the code (no data known to hold anything other than "EUR"), this is a safety net,
            // not a correction of known-bad data - see configurable-salary-currency.md, §Migrations.
            migrationBuilder.Sql("UPDATE JobOffers SET SalaryCurrency = 'EUR' WHERE SalaryCurrency IS NULL;");
            migrationBuilder.Sql("UPDATE JobOffers SET SalaryCurrency = 'EUR' WHERE SalaryCurrency NOT IN ('EUR', 'XOF');");

            // Step 3: only now that every row is guaranteed to hold 'EUR' or 'XOF' can the column
            // become NOT NULL.
            migrationBuilder.AlterColumn<string>(
                name: "SalaryCurrency",
                table: "JobOffers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            // Step 4: backfill Users.DesiredSalaryCurrency - candidate accounts (OrganizationId IS
            // NULL) get the currency convention already assumed everywhere else in the product
            // (Currency.XOF); organization accounts keep the NULL posed by step 1 (this field has no
            // meaning for them).
            migrationBuilder.Sql("UPDATE Users SET DesiredSalaryCurrency = 'XOF' WHERE OrganizationId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesiredSalaryCurrency",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Organizations");

            migrationBuilder.AlterColumn<string>(
                name: "SalaryCurrency",
                table: "JobOffers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }
    }
}
