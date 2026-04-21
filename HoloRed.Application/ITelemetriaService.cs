using HoloRed.Domain;

namespace HoloRed.Application.Interfaces
{
    public interface ITelemetriaService
    {
        Task RegistrarImpactoAsync(ImpactoTelemetria impacto);
        Task<IEnumerable<ImpactoTelemetria>> ObtenerHistorialAsync(string sectorId, DateOnly fecha);
    }
}