using System.Collections.Generic;
using System.Threading.Tasks;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Servicios
{
    public interface IServicioExportacionExcel
    {
        Task<string> ExportarPresupuestoAsync(
            IEnumerable<ProductoItem> productos,
            string nombreCliente,
            string carpetaDestino);
    }
}

