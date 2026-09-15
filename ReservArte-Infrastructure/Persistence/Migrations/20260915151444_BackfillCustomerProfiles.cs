using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservArte.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCustomerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Relleno (RA-869f1xc2n): hasta este cambio, el registro y el alta
            // social creaban cuentas Customer sin ficha. Cada una recibe la suya
            // con los datos de la cuenta y los valores por defecto del catálogo.
            //
            // - Solo datos de la cuenta: no se crea ningún consentimiento, porque
            //   esas personas no marcaron el de tratamiento de datos.
            // - Por organización: el mismo email puede tener cuenta en varios
            //   centros (RA-869f1xc0u), y el índice (OrganizationId, Email) de
            //   Customers se respeta saltando el email que ya use otra ficha.
            // - Idempotente: una cuenta que ya tiene ficha no se toca.
            migrationBuilder.Sql(
                """
                INSERT INTO Customers (Id, OrganizationId, FirstName, LastName, Email, Phone, Category,
                                       LoyaltyPoints, IsBlocked, PreferredContactMethod, IsActive, CreatedAt)
                SELECT u.Id, u.OrganizationId, u.FirstName, u.LastName, u.Email, LEFT(u.PhoneNumber, 20),
                       N'regular', 0, 0, N'email', 1, SYSUTCDATETIME()
                FROM AspNetUsers u
                WHERE u.Rol = N'Customer'
                  AND u.Email IS NOT NULL
                  AND LEN(u.Email) <= 255
                  AND NOT EXISTS (SELECT 1 FROM Customers c WHERE c.Id = u.Id)
                  AND NOT EXISTS (SELECT 1 FROM Customers c
                                  WHERE c.OrganizationId = u.OrganizationId AND c.Email = u.Email);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin reversión: después del relleno ya no se distingue una ficha
            // creada aquí de una creada por el alta pública, y borrar fichas de
            // clientes perdería datos.
        }
    }
}
