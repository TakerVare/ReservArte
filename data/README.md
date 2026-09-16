# data/ — scripts SQL de la base de datos

Hay **dos tipos de fichero**, separados a propósito en carpetas distintas:

| Carpeta | Qué contiene | Fuente de verdad | Cuándo se actualiza |
|---|---|---|---|
| [`schema/`](schema/) | **Creación** de la base de datos: esquema (DDL), **sin datos**. `create_ReservArteDB.sql` (generado), `drop_ReservArteDB.sql` y `regenerate-create.sh`. | Las **migraciones de EF Core** | Tras **cada** migración, regenerando con `bash data/schema/regenerate-create.sh`, en el mismo PR. |
| [`demo/`](demo/) | **Inyección de datos demo** para desarrollo (DML). `seed_demo_ReservArteDB.sql`. | El propio fichero, alineado con `DevSeeder` | Cuando una migración cambia una tabla que el script siembra, o cuando cambian los datos demo o el `DevSeeder`. |

> Los ficheros de `schema/` se pueden ejecutar en cualquier entorno. Los de `demo/` son **solo para desarrollo**: llevan contraseñas conocidas.

## Orden de ejecución

1. `schema/drop_ReservArteDB.sql`: opcional. **Destruye** la base de datos `ReservArteDB`.
2. `schema/create_ReservArteDB.sql`: crea la base de datos (si no existe) y el esquema. Es idempotente.
3. `demo/seed_demo_ReservArteDB.sql`: solo en desarrollo. No inserta nada si ya hay alguna organización.

Con el contenedor de desarrollo (macOS/Linux; en Windows, desde Git Bash anteponiendo `MSYS_NO_PATHCONV=1`):

```bash
SQLCMD="docker exec -i reservarte-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P <pwd-dev> -C -b"
$SQLCMD < data/schema/drop_ReservArteDB.sql       # opcional: DESTRUYE ReservArteDB
$SQLCMD < data/schema/create_ReservArteDB.sql
$SQLCMD < data/demo/seed_demo_ReservArteDB.sql
```

`-b` hace que `sqlcmd` pare al primer error.

## Relación con la API

- En `Development`, la API **aplica las migraciones y ejecuta `DevSeeder` al arrancar** (`Program.cs`). Para el día a día no hace falta ejecutar estos scripts: sirven para crear o reiniciar la base de datos sin arrancar la API, preparar otros entornos y revisar el esquema en SQL.
- Una base de datos creada con `create_ReservArteDB.sql` **la reconoce la API como migrada**: el script rellena `__EFMigrationsHistory`, así que al arrancar no se reaplica nada, y `DevSeeder` no siembra porque ya hay organización.

## Datos demo

| Dato | Valor |
|---|---|
| Organización | More Than Brows · `AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE` · subdominio `morethanbrows` |
| Admin (sin ficha de empleado) | `guille@svalero.com` / `Admin1234!` |
| Empleada | `maria.garcia@reservarte.com` / `Maria123!` |
| Empleada | `lucia.martinez@reservarte.com` / `Lucia123!` |
| Clienta (VIP) | `carmen.lopez@example.com` / `Cliente123!`: consentimientos de tratamiento de datos y marketing, alergia al látex (alta) y una nota de María |
| Clienta | `sofia.ruiz@example.com` / `Cliente123!`: consentimiento de tratamiento de datos |
| Horario semanal | María: lunes a jueves 09:00–18:00 y viernes 09:00–14:00. Lucía: lunes a jueves 10:00–19:00 y viernes 10:00–15:00. Convención **0 = lunes … 6 = domingo**. |

- Organización, cuentas y fichas (de empleada y de clienta) son **las mismas que crea `DevSeeder`**, con las mismas contraseñas. Los hashes son del `PasswordHasher` de ASP.NET Core Identity (PBKDF2), nunca texto plano.
- **Diferencia con `DevSeeder`:** el script añade el horario semanal demo; `DevSeeder` no lo crea.
- El **catálogo de servicios** sí se siembra (RA-869d7f3z0): dos categorías, tres servicios, una variación y tres tarifas por nivel, más quién sabe hacer qué. Los paquetes no llevan datos demo.
- **Citas, líneas de cita y lista de espera** ya están en el esquema (RA-869d7f4j8) pero **sin datos demo**: los añadirá el módulo que las cree (RA-869d7f519). Las tablas del antiguo script v2 que aún no existen en las migraciones (tarjetas guardadas, pagos, recordatorios…) **no están aquí**. Su diseño está en el vol. 1 §5; sus datos demo se añadirán cuando llegue su migración.

## Reglas de mantenimiento

1. **Nunca editar `create_ReservArteDB.sql` a mano.** Un cambio de base de datos es una migración y, después, `bash data/schema/regenerate-create.sh`.
2. **Todo PR que añada una migración incluye el `create` regenerado** y, si la migración toca una tabla que siembra el demo, el `seed_demo` actualizado.
3. **El demo debe poder ejecutarse sobre una base creada con el `create`.** Verificación mínima tras tocarlos: crear una base de prueba con ambos scripts y arrancar la API contra ella (login de las cuentas demo).
