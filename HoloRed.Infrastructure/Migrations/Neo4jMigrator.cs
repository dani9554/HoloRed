using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace HoloRed.Infrastructure.Migrations
{
    /// <summary>
    /// Crea constraints e índices en Neo4j. Cada versión aplicada se registra
    /// como nodo :_SchemaMigration para evitar reaplicarla.
    /// </summary>
    public class Neo4jMigrator
    {
        private readonly IConfiguration _config;
        private readonly ILogger<Neo4jMigrator> _logger;

        public Neo4jMigrator(IConfiguration config, ILogger<Neo4jMigrator> logger)
        {
            _config = config;
            _logger = logger;
        }

        private static readonly (string Version, string[] Statements)[] Migrations = new[]
        {
            ("001_constraints_faccion_espia", new[]
            {
                "CREATE CONSTRAINT faccion_nombre_unique IF NOT EXISTS FOR (f:Facción) REQUIRE f.nombre IS UNIQUE",
                "CREATE CONSTRAINT espia_nombre_unique IF NOT EXISTS FOR (e:Espía) REQUIRE e.nombre IS UNIQUE"
            })
        };

        public async Task MigrateAsync()
        {
            var uri = _config["Neo4j:Uri"] ?? throw new InvalidOperationException("Falta Neo4j:Uri");
            var user = _config["Neo4j:User"] ?? throw new InvalidOperationException("Falta Neo4j:User");
            var pass = _config["Neo4j:Password"] ?? throw new InvalidOperationException("Falta Neo4j:Password");

            await using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass),
                o => o.WithEncryptionLevel(EncryptionLevel.None));

            await using var session = driver.AsyncSession();

            var appliedResult = await session.RunAsync(
                "MATCH (m:_SchemaMigration) RETURN m.version AS version");
            var applied = new HashSet<string>();
            await appliedResult.ForEachAsync(r => applied.Add(r["version"].As<string>()));

            foreach (var (version, statements) in Migrations)
            {
                if (applied.Contains(version))
                {
                    _logger.LogDebug("Neo4j migration ya aplicada: {Version}", version);
                    continue;
                }

                foreach (var cypher in statements)
                    await session.RunAsync(cypher);

                await session.RunAsync(
                    "MERGE (m:_SchemaMigration {version: $version}) SET m.appliedAt = datetime()",
                    new { version });

                _logger.LogInformation("Neo4j migration aplicada: {Version}", version);
            }
        }
    }
}
