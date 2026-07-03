using Microsoft.EntityFrameworkCore;
using ReservArte.API.Extensions;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// ── Base de datos ─────────────────────────────────────────────────────────
builder.Services.AddDatabase(builder.Configuration);

// ── (resto de registros de servicios) ────────────────────────────────────
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Solo en Development: Swagger + migraciones + seed automáticos
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DevSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
