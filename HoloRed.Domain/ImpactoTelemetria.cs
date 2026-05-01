namespace HoloRed.Domain
{
    public class ImpactoTelemetria
    {
        public string SectorId { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid Id { get; set; }
        public string NaveAtacante { get; set; } = string.Empty;
        public string NaveObjetivo { get; set; } = string.Empty;
        public int DanioEscudos { get; set; }
    }
}
