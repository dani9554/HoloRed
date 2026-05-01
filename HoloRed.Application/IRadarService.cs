using HoloRed.Domain;

namespace HoloRed.Application.Interfaces
{
    public interface IRadarService
    {
        Task ActualizarEstadoAsync(string codigoNave, EstadoNave estado);
        Task<EstadoNave?> ObtenerEstadoAsync(string codigoNave);
    }
}
