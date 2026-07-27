using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XpertSphere.MonolithApi.Migrations
{
    /// <inheritdoc />
    public partial class GrandfatherExistingUnconfirmedAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration, no schema change. Activation stricte
            // (RequireConfirmedEmail = true, voir candidate-account-activation-email.md) bloquerait
            // rétroactivement tout compte préexistant EmailConfirmed = false, alors qu'aucun email
            // d'activation réel n'a jamais été envoyé avant ce ticket. Grand-père ces comptes plutôt
            // que de les bloquer silencieusement.
            migrationBuilder.Sql("UPDATE Users SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionnellement vide : un rollback ne doit jamais re-verrouiller des comptes qui
            // ont été légitimement grand-pères - non réversible par conception.
        }
    }
}
