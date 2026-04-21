using Cassandra;
using HoloRed.Application.Interfaces;
using HoloRed.Domain;
using Microsoft.Extensions.Configuration;

namespace HoloRed.Infrastructure.Services
{
    public class CassandraService : ITelemetriaService
    {
        private readonly ISession _session;

        public CassandraService(IConfiguration config)
        {
            var host = config["Cassandra:Host"]!;
            var port = int.Parse(config["Cassandra:Port"]!);
            var user = config["Cassandra:User"]!;
            var pass = config["Cassandra:Password"]!;
            var keyspace = config["Cassandra:Keyspace"]!;

            var cluster = Cluster.Builder()
                .AddContactPoint(host)
                .WithPort(port)
                .WithCredentials(user, pass)
                .Build();

            _session = cluster.Connect();
            InicializarKeyspace(keyspace);
        }

        private void InicializarKeyspace(string keyspace)
        {
            _session.Execute($@"
                CREATE KEYSPACE IF NOT EXISTS {keyspace}
                WITH replication = {{'class':'SimpleStrategy','replication_factor':1}}");

            _session.Execute($"USE {keyspace}");

            _session.Execute(@"
                CREATE TABLE IF NOT EXISTS impactos_combate (
                    sector_id text,
                    fecha date,
                    timestamp timestamp,
                    id uuid,
                    nave_atacante text,
                    nave_objetivo text,
                    daño_escudos int,
                    PRIMARY KEY ((sector_id, fecha), timestamp, id)
                ) WITH CLUSTERING ORDER BY (timestamp DESC)");
        }

        public async Task RegistrarImpactoAsync(ImpactoTelemetria impacto)
        {
            try
            {
                var stmt = await _session.PrepareAsync(@"
                    INSERT INTO impactos_combate 
                    (sector_id, fecha, timestamp, id, nave_atacante, nave_objetivo, daño_escudos)
                    VALUES (?, ?, ?, ?, ?, ?, ?)");

                var bound = stmt.Bind(
                    impacto.SectorId,
                    impacto.Fecha.ToDateTime(TimeOnly.MinValue),
                    impacto.Timestamp,
                    impacto.Id,
                    impacto.NaveAtacante,
                    impacto.NaveObjetivo,
                    impacto.DañoEscudos
                );

                await _session.ExecuteAsync(bound);
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

        public async Task<IEnumerable<ImpactoTelemetria>> ObtenerHistorialAsync(string sectorId, DateOnly fecha)
        {
            try
            {
                var stmt = await _session.PrepareAsync(@"
                    SELECT sector_id, fecha, timestamp, id, nave_atacante, nave_objetivo, daño_escudos
                    FROM impactos_combate
                    WHERE sector_id = ? AND fecha = ?");

                var bound = stmt.Bind(sectorId, fecha.ToDateTime(TimeOnly.MinValue));
                var rows = await _session.ExecuteAsync(bound);

                return rows.Select(row => new ImpactoTelemetria
                {
                    SectorId = row.GetValue<string>("sector_id"),
                    Fecha = DateOnly.FromDateTime(row.GetValue<DateTime>("fecha")),
                    Timestamp = row.GetValue<DateTime>("timestamp"),
                    Id = row.GetValue<Guid>("id"),
                    NaveAtacante = row.GetValue<string>("nave_atacante"),
                    NaveObjetivo = row.GetValue<string>("nave_objetivo"),
                    DañoEscudos = row.GetValue<int>("daño_escudos")
                });
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
    }
}