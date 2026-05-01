using System.Text.Json.Serialization;

namespace HoloRed.Domain
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EstadoNave
    {
        Patrulla,
        Hiperespacio,
        Combate
    }
}
