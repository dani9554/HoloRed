using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HoloRed.Infrastructure.Seeders
{
    /// <summary>
    /// Siembra naves iniciales en el radar y una bahía ocupada de ejemplo.
    /// Usa When.NotExists → idempotente, no pisa datos reales.
    /// </summary>
    public class RedisSeeder
    {
        private readonly IConfiguration _config;
        private readonly ILogger<RedisSeeder> _logger;

        public RedisSeeder(IConfiguration config, ILogger<RedisSeeder> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var connStr = _config["Redis:ConnectionString"]!;
            var redis = await ConnectionMultiplexer.ConnectAsync(connStr);
            try
            {
                var db = redis.GetDatabase();

                var naves = new (string Codigo, string Estado)[]
                {
                    ("N-001", "Patrulla"),
                    ("N-002", "Combate"),
                    ("N-003", "Hiperespacio"),
                };

                foreach (var (codigo, estado) in naves)
                {
                    await db.StringSetAsync(
                        $"nave:{codigo}:estado", estado,
                        TimeSpan.FromMinutes(10), When.NotExists);
                }

                await db.StringSetAsync(
                    "bahia:Crucero-Alfa:1", "N-001",
                    TimeSpan.FromHours(1), When.NotExists);

                _logger.LogInformation("Redis seeder ejecutado: {N} naves + 1 bahía.", naves.Length);
            }
            finally
            {
                await redis.CloseAsync();
                redis.Dispose();
            }
        }
    }
}
