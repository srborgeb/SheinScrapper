using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Servicios
{
    public class ReporteProgresoScraping
    {
        public int IndiceActual { get; set; }
        public int TotalRegistros { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool EsExitoso { get; set; }
        public ProductoItem? Producto { get; set; }
        public int Porcentaje => TotalRegistros > 0 ? (int)Math.Round((double)IndiceActual / TotalRegistros * 100) : 0;
    }

    public interface IServicioScraperShein
    {
        Task<List<ProductoItem>> ExtraerLoteAsync(
            List<string> urls,
            string carpetaImagenes,
            decimal comisionPorDefecto,
            string clientePorDefecto,
            string tallaPorDefecto = "",
            string colorPorDefecto = "",
            int cantidadPorDefecto = 1,
            IProgress<ReporteProgresoScraping>? progreso = null,
            CancellationToken cancelacion = default);

        Task<ProductoItem> ExtraerIndividualAsync(
            string url,
            string carpetaImagenes,
            decimal comisionPorDefecto,
            string clientePorDefecto,
            string tallaPorDefecto = "",
            string colorPorDefecto = "",
            int cantidadPorDefecto = 1,
            CancellationToken cancelacion = default);
    }
}

