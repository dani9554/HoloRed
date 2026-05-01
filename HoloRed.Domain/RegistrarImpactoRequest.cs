using System.ComponentModel.DataAnnotations;

namespace HoloRed.Domain
{
    public class RegistrarImpactoRequest
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string SectorId { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 1)]
        public string NaveAtacante { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 1)]
        public string NaveObjetivo { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int DanioEscudos { get; set; }
    }
}
