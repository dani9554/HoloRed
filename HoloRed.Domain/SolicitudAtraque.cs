using System.ComponentModel.DataAnnotations;

namespace HoloRed.Domain
{
    public class SolicitudAtraque
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string CodigoNave { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 1)]
        public string Crucero { get; set; } = string.Empty;

        [Range(1, 9999)]
        public int NumeroBahia { get; set; }
    }
}
