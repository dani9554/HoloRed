using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace HoloRed.Infrastructure.Seeders
{
    /// <summary>
    /// Siembra facciones, espías y relaciones de infiltración/suministro de armas.
    /// Todo MERGE → idempotente.
    /// </summary>
    public class Neo4jSeeder
    {
        private readonly IConfiguration _config;
        private readonly ILogger<Neo4jSeeder> _logger;

        public Neo4jSeeder(IConfiguration config, ILogger<Neo4jSeeder> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var uri = _config["Neo4j:Uri"]!;
            var user = _config["Neo4j:User"]!;
            var pass = _config["Neo4j:Password"]!;

            await using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass),
                o => o.WithEncryptionLevel(EncryptionLevel.None));
            await using var session = driver.AsyncSession();

            // Facciones
            await session.RunAsync(@"
                UNWIND $facciones AS f
                MERGE (:Facción {nombre: f})",
                new { facciones = new[] { "Alianza", "Imperio", "Piratas" } });

            // Espías y sus relaciones (algunos son traidores)
            await session.RunAsync(@"
                UNWIND $data AS row
                MERGE (e:Espía {nombre: row.espia})
                WITH e, row
                MATCH (f:Facción {nombre: row.infiltrado})
                MERGE (e)-[:INFILTRADO_EN]->(f)",
                new
                {
                    data = new object[]
                    {
                        new { espia = "Kira",  infiltrado = "Alianza" },
                        new { espia = "Dax",   infiltrado = "Alianza" },
                        new { espia = "Sol",   infiltrado = "Imperio" },
                        new { espia = "Vex",   infiltrado = "Piratas" }
                    }
                });

            // Traidores: suministran armas a una facción distinta
            await session.RunAsync(@"
                UNWIND $data AS row
                MATCH (e:Espía {nombre: row.espia})
                MATCH (rival:Facción {nombre: row.suministra})
                MERGE (e)-[:SUMINISTRA_ARMAS_A]->(rival)",
                new
                {
                    data = new object[]
                    {
                        new { espia = "Kira", suministra = "Imperio" },
                        new { espia = "Sol",  suministra = "Piratas" }
                    }
                });

            _logger.LogInformation("Neo4j seeder ejecutado: facciones, espías y relaciones garantizadas.");
        }
    }
}
