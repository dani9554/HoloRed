using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HoloRed.Infrastructure.Seeders
{
    /// <summary>
    /// Siembra impactos de ejemplo en impactos_combate.
    /// Idempotente: usa un UUID fijo por impacto + "INSERT ... IF NOT EXISTS".
    /// </summary>
    public class CassandraSeeder
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CassandraSeeder> _logger;

        public CassandraSeeder(IConfiguration config, ILogger<CassandraSeeder> logger)
        {
            _config = config;
            _logger = logger;
        }

        private static readonly (Guid Id, string Sector, DateTime Timestamp, string Atacante, string Objetivo, int Danio)[] Impactos = new[]
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "sector-001", new DateTime(2026, 4, 23, 10, 0, 0, DateTimeKind.Utc), "N-001", "N-900", 50),
            (Guid.Parse("22222222-2222-2222-2222-222222222222"), "sector-001", new DateTime(2026, 4, 23, 10, 5, 0, DateTimeKind.Utc), "N-001", "N-901", 75),
            (Guid.Parse("33333333-3333-3333-3333-333333333333"), "sector-002", new DateTime(2026, 4, 23, 11, 0, 0, DateTimeKind.Utc), "N-002", "N-902", 120),
        };

        public async Task SeedAsync()
        {
            var host = _config["Cassandra:Host"]!;
            var port = int.Parse(_config["Cassandra:Port"]!);
            var user = _config["Cassandra:User"]!;
            var pass = _config["Cassandra:Password"]!;
            var keyspace = _config["Cassandra:Keyspace"]!;

            var cluster = Cluster.Builder()
                .AddContactPoint(host)
                .WithPort(port)
                .WithCredentials(user, pass)
                .Build();

            var session = cluster.Connect(keyspace);
            try
            {
                var stmt = await session.PrepareAsync(@"
                    INSERT INTO impactos_combate
                    (sector_id, fecha, timestamp, id, nave_atacante, nave_objetivo, danio_escudos)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                    IF NOT EXISTS");

                foreach (var i in Impactos)
                {
                    var fecha = DateOnly.FromDateTime(i.Timestamp).ToDateTime(TimeOnly.MinValue);
                    await session.ExecuteAsync(stmt.Bind(
                        i.Sector, fecha, i.Timestamp, i.Id, i.Atacante, i.Objetivo, i.Danio));
                }

                _logger.LogInformation("Cassandra seeder ejecutado: {N} impactos garantizados.", Impactos.Length);
            }
            finally
            {
                await session.ShutdownAsync();
                await cluster.ShutdownAsync();
            }
        }
    }
}
