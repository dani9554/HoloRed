using HoloRed.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver;

namespace HoloRed.Infrastructure.Services
{
    /// <summary>
    /// Servicio de inteligencia usando Neo4j como base de datos orientada a grafos.
    /// Permite descubrir relaciones entre espías, facciones y planetas.
    /// </summary>
    public class Neo4jService : IInteligenciaService
    {
        private readonly IDriver _driver;

        public Neo4jService(IConfiguration config)
        {
            var uri = config["Neo4j:Uri"]!;
            var user = config["Neo4j:User"]!;
            var pass = config["Neo4j:Password"]!;
            // EncryptionLevel.None porque estamos en local sin certificados
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass),
                o => o.WithEncryptionLevel(EncryptionLevel.None));
        }

        /// <summary>
        /// Busca espías infiltrados en una facción que además suministran armas a una facción rival.
        /// La query salta dos niveles de profundidad en el grafo: Espía → Facción → Facción rival.
        /// </summary>
        public async Task<IEnumerable<string>> ObtenerTraidoresAsync(string faccion)
        {
            try
            {
                await using var session = _driver.AsyncSession();

                // Buscamos espías que están infiltrados en la facción indicada
                // pero a la vez suministran armas a otra facción distinta
                var query = @"
                    MATCH (e:Espía)-[:INFILTRADO_EN]->(f:Facción {nombre: $faccion})
                    MATCH (e)-[:SUMINISTRA_ARMAS_A]->(rival:Facción)
                    WHERE rival.nombre <> $faccion
                    RETURN e.nombre AS traidor";

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
    }
}