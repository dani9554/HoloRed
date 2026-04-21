using HoloRed.Application.Interfaces;
using HoloRed.Domain;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HoloRed.Infrastructure.Services
{
    public class RedisService : IRadarService, IFlotaService
    {
        private readonly IDatabase _db;
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public RedisService(IConfiguration config)
        {
            var connStr = config["Redis:ConnectionString"]!;
            var redis = ConnectionMultiplexer.Connect(connStr);
            _db = redis.GetDatabase();
        }

        public async Task ActualizarEstadoAsync(string codigoNave, string estado)
        {
            try
            {
                var key = $"nave:{codigoNave}:estado";
                await _db.StringSetAsync(key, estado, TimeSpan.FromMinutes(10));
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis al actualizar estado: {ex.Message}", ex);
            }
        }

        public async Task<string?> ObtenerEstadoAsync(string codigoNave)
        {
            try
            {
                var key = $"nave:{codigoNave}:estado";
                var valor = await _db.StringGetAsync(key);
                return valor.HasValue ? valor.ToString() : null;
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis al obtener estado: {ex.Message}", ex);
            }
        }

        public async Task<bool> SolicitarAtraqueAsync(SolicitudAtraque solicitud)
        {
            await _lock.WaitAsync();
            try
            {
                var key = $"bahia:{solicitud.Crucero}:{solicitud.NumeroBahia}";
                var ocupada = await _db.StringGetAsync(key);
                if (ocupada.HasValue)
                    return false;

                await _db.StringSetAsync(key, solicitud.CodigoNave, TimeSpan.FromHours(1));
                return true;
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis en atraque: {ex.Message}", ex);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}