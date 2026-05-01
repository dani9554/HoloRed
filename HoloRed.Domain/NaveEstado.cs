namespace HoloRed.Domain
{
    public class NaveEstado
    {
        public string Codigo { get; set; } = string.Empty;
        public EstadoNave Estado { get; set; } = EstadoNave.Patrulla;
    }
}
