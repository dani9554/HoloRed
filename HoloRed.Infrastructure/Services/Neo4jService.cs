using HoloRed.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver;

namespace HoloRed.Infrastructure.Services
{
    public class Neo4jService : IInteligenciaService, IAsyncDisposable
    {
        private readonly IDriver _driver;

        public Neo4jService(IConfiguration config)
        {
            var uri = config["Neo4j:Uri"] ?? throw new InvalidOperationException("Falta Neo4j:Uri");
            var user = config["Neo4j:User"] ?? throw new InvalidOperationException("Falta Neo4j:User");
            var pass = config["Neo4j:Password"] ?? throw new InvalidOperationException("Falta Neo4j:Password");
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass),
                o => o.WithEncryptionLevel(EncryptionLevel.None));
        }

        public async Task<IEnumerable<string>> ObtenerTraidoresAsync(string faccion)
        {
            if (string.IsNullOrWhiteSpace(faccion))
                throw new ArgumentException("La facción no puede estar vacía.", nameof(faccion));

            try
            {
                await using var session = _driver.AsyncSession();

                var query = @"
                    MATCH (e:Espía)-[:INFILTRADO_EN]->(f:Facción {nombre: $faccion})
                    MATCH (e)-[:SUMINISTRA_ARMAS_A]->(rival:Facción)
                    WHERE rival.nombre <> $faccion
                    RETURN DISTINCT e.nombre AS traidor";

                var result = await session.RunAsync(query, new { faccion });
                var traidores = new List<string>();

                await result.ForEachAsync(record =>
                    traidores.Add(record["traidor"].As<string>()));

                return traidores;
            }
            catch (ServiceUnavailableException ex)
            {
                throw new InvalidOperationException($"Neo4j no disponible: {ex.Message}", ex);
            }
            catch (AuthorizationException ex)
            {
                throw new InvalidOperationException($"Error de autenticación Neo4j: {ex.Message}", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _driver.DisposeAsync();
        }
    }
}
