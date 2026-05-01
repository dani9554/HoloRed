using Cassandra;
using HoloRed.Application.Interfaces;
using HoloRed.Domain;
using Microsoft.Extensions.Configuration;

namespace HoloRed.Infrastructure.Services
{
    public class CassandraService : ITelemetriaService, IAsyncDisposable
    {
        private readonly Cluster _cluster;
        private readonly ISession _session;
        private readonly PreparedStatement _stmtInsert;
        private readonly PreparedStatement _stmtSelect;

        public CassandraService(IConfiguration config)
        {
            var host = config["Cassandra:Host"] ?? throw new InvalidOperationException("Falta Cassandra:Host");
            var port = int.Parse(config["Cassandra:Port"] ?? throw new InvalidOperationException("Falta Cassandra:Port"));
            var user = config["Cassandra:User"] ?? throw new InvalidOperationException("Falta Cassandra:User");
            var pass = config["Cassandra:Password"] ?? throw new InvalidOperationException("Falta Cassandra:Password");
            var keyspace = config["Cassandra:Keyspace"] ?? throw new InvalidOperationException("Falta Cassandra:Keyspace");

            _cluster = Cluster.Builder()
                .AddContactPoint(host)
                .WithPort(port)
                .WithCredentials(user, pass)
                .Build();

            // El keyspace y las tablas los crea CassandraMigrator al arrancar.
            _session = _cluster.Connect(keyspace);

            _stmtInsert = _session.Prepare(@"
                INSERT INTO impactos_combate
                (sector_id, fecha, timestamp, id, nave_atacante, nave_objetivo, danio_escudos)
                VALUES (?, ?, ?, ?, ?, ?, ?)");

            _stmtSelect = _session.Prepare(@"
                SELECT sector_id, fecha, timestamp, id, nave_atacante, nave_objetivo, danio_escudos
                FROM impactos_combate
                WHERE sector_id = ? AND fecha = ?
                LIMIT ?");
        }

        public async Task<ImpactoTelemetria> RegistrarImpactoAsync(RegistrarImpactoRequest request)
        {
            var impacto = new ImpactoTelemetria
            {
                SectorId = request.SectorId,
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                NaveAtacante = request.NaveAtacante,
                NaveObjetivo = request.NaveObjetivo,
                DanioEscudos = request.DanioEscudos
            };
            // Fecha derivada del timestamp del servidor para garantizar coherencia
            // con la clave de partición en Cassandra.
            impacto.Fecha = DateOnly.FromDateTime(impacto.Timestamp);

            try
            {
                var bound = _stmtInsert.Bind(
                    impacto.SectorId,
                    impacto.Fecha.ToDateTime(TimeOnly.MinValue),
                    impacto.Timestamp,
                    impacto.Id,
                    impacto.NaveAtacante,
                    impacto.NaveObjetivo,
                    impacto.DanioEscudos);

                await _session.ExecuteAsync(bound);
                return impacto;
            }
            catch (NoHostAvailableException ex)
            {
                throw new InvalidOperationException($"Cassandra no disponible: {ex.Message}", ex);
            }
            catch (QueryExecutionException ex)
            {
                throw new InvalidOperationException($"Error ejecutando query Cassandra: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ImpactoTelemetria>> ObtenerHistorialAsync(string sectorId, DateOnly fecha, int limite = 500)
        {
            if (limite <= 0 || limite > 5000) limite = 500;
            try
            {
                var bound = _stmtSelect.Bind(sectorId, fecha.ToDateTime(TimeOnly.MinValue), limite);
                var rows = await _session.ExecuteAsync(bound);

                return rows.Select(row => new ImpactoTelemetria
                {
                    SectorId = row.GetValue<string>("sector_id"),
                    Fecha = DateOnly.FromDateTime(row.GetValue<DateTime>("fecha")),
                    Timestamp = row.GetValue<DateTime>("timestamp"),
                    Id = row.GetValue<Guid>("id"),
                    NaveAtacante = row.GetValue<string>("nave_atacante"),
                    NaveObjetivo = row.GetValue<string>("nave_objetivo"),
                    DanioEscudos = row.GetValue<int>("danio_escudos")
                }).ToList();
            }
            catch (NoHostAvailableException ex)
            {
                throw new InvalidOperationException($"Cassandra no disponible: {ex.Message}", ex);
            }
            catch (QueryExecutionException ex)
            {
                throw new InvalidOperationException($"Error ejecutando query Cassandra: {ex.Message}", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _session.ShutdownAsync();
            await _cluster.ShutdownAsync();
        }
    }
}
