using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace SheinScraperApp.Servicios
{
    public interface IServicioAlmacenamientoImagenes
    {
        Task<string> DescargarYGuardarAsync(string urlImagen, string carpetaDestino, string sku, CancellationToken cancelacion = default);
        Image? CargarMiniatura(string rutaLocal, int ancho = 110, int alto = 110);
        string ResolverRutaLocal(string? rutaGuardada, string carpetaImagenes, string? sku = null);
    }
}

