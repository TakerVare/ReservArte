using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservArte.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRolesToPascalCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Catálogo canónico de roles en PascalCase (RA-869f18116). Los
            // valores previos estaban en minúsculas, y [Authorize(Roles = …)]
            // distingue mayúsculas: sin esta normalización, el primer endpoint
            // protegido denegaría el acceso a TODOS los usuarios existentes.
            //
            // Se normaliza por comparación insensible a mayúsculas para que la
            // migración sea idempotente y tolere datos ya convertidos a mano.
            migrationBuilder.Sql(@"
                UPDATE AspNetUsers SET Rol = 'Admin'    WHERE LOWER(Rol) = 'admin';
                UPDATE AspNetUsers SET Rol = 'Manager'  WHERE LOWER(Rol) = 'manager';
                UPDATE AspNetUsers SET Rol = 'Employee' WHERE LOWER(Rol) = 'employee';
                UPDATE AspNetUsers SET Rol = 'Customer' WHERE LOWER(Rol) IN ('customer', 'client');

                UPDATE Employees SET Rol = 'Admin'    WHERE LOWER(Rol) = 'admin';
                UPDATE Employees SET Rol = 'Manager'  WHERE LOWER(Rol) = 'manager';
                UPDATE Employees SET Rol = 'Employee' WHERE LOWER(Rol) = 'employee';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vuelta a minúsculas. 'Customer' regresa a 'client', que era el
            // valor admitido por el CHECK del esquema anterior.
            migrationBuilder.Sql(@"
                UPDATE AspNetUsers SET Rol = 'admin'    WHERE LOWER(Rol) = 'admin';
                UPDATE AspNetUsers SET Rol = 'manager'  WHERE LOWER(Rol) = 'manager';
                UPDATE AspNetUsers SET Rol = 'employee' WHERE LOWER(Rol) = 'employee';
                UPDATE AspNetUsers SET Rol = 'client'   WHERE LOWER(Rol) IN ('customer', 'client');

                UPDATE Employees SET Rol = 'admin'    WHERE LOWER(Rol) = 'admin';
                UPDATE Employees SET Rol = 'manager'  WHERE LOWER(Rol) = 'manager';
                UPDATE Employees SET Rol = 'employee' WHERE LOWER(Rol) = 'employee';
            ");
        }
    }
}
