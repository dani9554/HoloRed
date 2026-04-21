namespace HoloRed.Domain
{
    public class NaveEstado
    {
        public string Codigo { get; set; } = string.Empty;
        public string Estado { get; set; } = "patrulla"; // patrulla, hiperespacio, combate
    }
}
