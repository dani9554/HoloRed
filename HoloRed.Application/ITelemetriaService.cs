using HoloRed.Domain;

namespace HoloRed.Application.Interfaces
{
    public interface ITelemetriaService
    {
        Task<ImpactoTelemetria> RegistrarImpactoAsync(RegistrarImpactoRequest request);
        Task<IEnumerable<ImpactoTelemetria>> ObtenerHistorialAsync(string sectorId, DateOnly fecha, int limite = 500);
    }
}
