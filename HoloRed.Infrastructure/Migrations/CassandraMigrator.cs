using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HoloRed.Infrastructure.Migrations
{
    /// <summary>
    /// Corre migraciones idempotentes contra Cassandra. Registra cada versión
    /// aplicada en la tabla schema_migrations del keyspace de la aplicación.
    /// </summary>
    public class CassandraMigrator
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CassandraMigrator> _logger;

        public CassandraMigrator(IConfiguration config, ILogger<CassandraMigrator> logger)
        {
            _config = config;
            _logger = logger;
        }

        private static readonly (string Version, string[] Statements)[] Migrations = new[]
        {
            ("001_create_impactos_combate", new[]
            {
                @"CREATE TABLE IF NOT EXISTS impactos_combate (
                    sector_id text,
                    fecha date,
                    timestamp timestamp,
                    id uuid,
                    nave_atacante text,
                    nave_objetivo text,
                    danio_escudos int,
                    PRIMARY KEY ((sector_id, fecha), timestamp, id)
                ) WITH CLUSTERING ORDER BY (timestamp DESC)"
            })
        };

        public async Task MigrateAsync()
        {
            var host = _config["Cassandra:Host"] ?? throw new InvalidOperationException("Falta Cassandra:Host");
            var port = int.Parse(_config["Cassandra:Port"] ?? throw new InvalidOperationException("Falta Cassandra:Port"));
            var user = _config["Cassandra:User"] ?? throw new InvalidOperationException("Falta Cassandra:User");
            var pass = _config["Cassandra:Password"] ?? throw new InvalidOperationException("Falta Cassandra:Password");
            var keyspace = _config["Cassandra:Keyspace"] ?? throw new InvalidOperationException("Falta Cassandra:Keyspace");

            var cluster = Cluster.Builder()
                .AddContactPoint(host)
                .WithPort(port)
                .WithCredentials(user, pass)
                .Build();

            var session = cluster.Connect();
            try
            {
                session.Execute($@"
                    CREATE KEYSPACE IF NOT EXISTS {keyspace}
                    WITH replication = {{'class':'SimpleStrategy','replication_factor':1}}");

                session.Execute($"USE {keyspace}");

                session.Execute(@"
                    CREATE TABLE IF NOT EXISTS schema_migrations (
                        version text PRIMARY KEY,
                        applied_at timestamp
                    )");

                var applied = new HashSet<string>();
                var rs = await session.ExecuteAsync(new SimpleStatement("SELECT version FROM schema_migrations"));
                foreach (var row in rs) applied.Add(row.GetValue<string>("version"));

                var insertMigration = await session.PrepareAsync(
                    "INSERT INTO schema_migrations (version, applied_at) VALUES (?, ?)");

                foreach (var (version, statements) in Migrations)
                {
                    if (applied.Contains(version))
                    {
                        _logger.LogDebug("Cassandra migration ya aplicada: {Version}", version);
                        continue;
                    }

                    foreach (var cql in statements)
                        await session.ExecuteAsync(new SimpleStatement(cql));

                    await session.ExecuteAsync(insertMigration.Bind(version, DateTime.UtcNow));
                    _logger.LogInformation("Cassandra migration aplicada: {Version}", version);
                }
            }
            finally
            {
                await session.ShutdownAsync();
                await cluster.ShutdownAsync();
            }
        }
    }
}
