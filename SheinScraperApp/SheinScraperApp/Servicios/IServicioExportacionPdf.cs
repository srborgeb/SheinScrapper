using System.Collections.Generic;
using System.Threading.Tasks;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Servicios
{
    public interface IServicioExportacionPdf
    {
        Task<string> ExportarEstimacionPdfAsync(
            IEnumerable<ProductoItem> productos,
            string nombreCliente,
            string carpetaDestino);
    }
}

