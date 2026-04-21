using HoloRed.Application.Interfaces;
using HoloRed.Domain;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HoloRed.Infrastructure.Services
{
    /// <summary>
    /// Servicio que gestiona el radar y los atraques usando Redis como base de datos en memoria.
    /// Implementa IRadarService e IFlotaService porque ambos usan Redis.
    /// </summary>
    public class RedisService : IRadarService, IFlotaService
    {
        private readonly IDatabase _db;

        // SemaphoreSlim para evitar condiciones de carrera en los atraques
        // Solo un hilo a la vez puede asignar una bahía
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public RedisService(IConfiguration config)
        {
            var connStr = config["Redis:ConnectionString"]!;
            var redis = ConnectionMultiplexer.Connect(connStr);
            _db = redis.GetDatabase();
        }

        /// <summary>
        /// Actualiza el estado de una nave en el radar con TTL de 10 minutos.
        /// Si la nave no emite señal en ese tiempo, desaparece automáticamente.
        /// </summary>
        public async Task ActualizarEstadoAsync(string codigoNave, string estado)
        {
            try
            {
                var key = $"nave:{codigoNave}:estado";
                // TTL de 10 minutos — si no hay señal, la nave desaparece del radar
                await _db.StringSetAsync(key, estado, TimeSpan.FromMinutes(10));
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis al actualizar estado: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene el estado actual de una nave. Devuelve null si la nave no está en el radar.
        /// </summary>
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

        /// <summary>
        /// Solicita una bahía de atraque en un crucero.
        /// Usa un SemaphoreSlim para evitar que dos naves ocupen la misma bahía a la vez.
        /// </summary>
        /// <returns>True si el atraque fue concedido, false si la bahía ya está ocupada.</returns>
        public async Task<bool> SolicitarAtraqueAsync(SolicitudAtraque solicitud)
        {
            // Bloqueamos el acceso para evitar race conditions
            await _lock.WaitAsync();
            try
            {
                var key = $"bahia:{solicitud.Crucero}:{solicitud.NumeroBahia}";
                var ocupada = await _db.StringGetAsync(key);

                if (ocupada.HasValue)
                    return false; // Bahía ocupada

                await _db.StringSetAsync(key, solicitud.CodigoNave, TimeSpan.FromHours(1));
                return true;
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Error Redis en atraque: {ex.Message}", ex);
            }
            finally
            {
                // Siempre liberamos el semáforo, pase lo que pase
                _lock.Release();
            }
        }
    }
}