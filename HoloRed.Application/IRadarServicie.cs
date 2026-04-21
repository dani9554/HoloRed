using HoloRed.Domain;

namespace HoloRed.Application.Interfaces
{
    public interface IRadarService
    {
        Task ActualizarEstadoAsync(string codigoNave, string estado);
        Task<string?> ObtenerEstadoAsync(string codigoNave);
    }
}