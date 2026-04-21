namespace HoloRed.Application.Interfaces
{
    public interface IInteligenciaService
    {
        Task<IEnumerable<string>> ObtenerTraidoresAsync(string faccion);
    }
}