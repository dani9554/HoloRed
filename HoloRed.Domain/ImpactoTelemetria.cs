namespace HoloRed.Domain
{
    public class ImpactoTelemetria
    {
        public string SectorId { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NaveAtacante { get; set; } = string.Empty;
        public string NaveObjetivo { get; set; } = string.Empty;
        public int DañoEscudos { get; set; }
    }
}