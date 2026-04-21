using HoloRed.Domain;

namespace HoloRed.Application.Interfaces
{
    public interface IFlotaService
    {
        Task<bool> SolicitarAtraqueAsync(SolicitudAtraque solicitud);
    }
}