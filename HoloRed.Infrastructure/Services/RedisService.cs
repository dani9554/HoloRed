using HoloRed.Application.Interfaces;
using HoloRed.Domain;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HoloRed.Infrastructure.Services
{
    public class RedisService : IRadarService, IFlotaService, IAsyncDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly TimeSpan _ttlRadar = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _ttlBahia = TimeSpan.FromHours(1);

        public RedisService(IConfiguration config)
        {
            var connStr = config["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Falta Redis:ConnectionString");
            _redis = ConnectionMultiplexer.Connect(connStr);
            _db = _redis.GetDatabase();
        }

        public async Task ActualizarEstadoAsync(string codigoNave, EstadoNave estado)
        {
            try
            {
                await _db.StringSetAsync(KeyRadar(codigoNave), estado.ToString(), _ttlRadar);
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis al actualizar estado: {ex.Message}", ex);
            }
        }

        public async Task<EstadoNave?> ObtenerEstadoAsync(string codigoNave)
        {
            try
            {
                var valor = await _db.StringGetAsync(KeyRadar(codigoNave));
                if (!valor.HasValue) return null;
                return Enum.TryParse<EstadoNave>(valor.ToString(), ignoreCase: true, out var estado)
                    ? estado
                    : null;
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis al obtener estado: {ex.Message}", ex);
            }
        }

        // Reserva atómica con SET NX EX — sin race conditions entre procesos.
        public async Task<bool> SolicitarAtraqueAsync(SolicitudAtraque solicitud)
        {
            try
            {
                var key = KeyBahia(solicitud.Crucero, solicitud.NumeroBahia);
                return await _db.StringSetAsync(key, solicitud.CodigoNave, _ttlBahia, When.NotExists);
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis en atraque: {ex.Message}", ex);
            }
        }

        public async Task<bool> LiberarAtraqueAsync(string crucero, int numeroBahia)
        {
            try
            {
                return await _db.KeyDeleteAsync(KeyBahia(crucero, numeroBahia));
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis al liberar bahía: {ex.Message}", ex);
            }
        }

        private static string KeyRadar(string codigoNave) => $"nave:{codigoNave}:estado";
        private static string KeyBahia(string crucero, int bahia) => $"bahia:{crucero}:{bahia}";

        public async ValueTask DisposeAsync()
        {
            await _redis.CloseAsync();
            _redis.Dispose();
        }
    }
}
