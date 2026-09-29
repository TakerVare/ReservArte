namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>
/// Colección única: todas las clases comparten un contenedor de PostgreSQL y una
/// API, y xUnit no ejecuta en paralelo las clases de una misma colección.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
