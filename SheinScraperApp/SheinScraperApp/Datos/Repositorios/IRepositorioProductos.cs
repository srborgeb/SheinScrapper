using System.Collections.Generic;
using System.Threading.Tasks;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Datos.Repositorios
{
    public interface IRepositorioProductos
    {
        Task InicializarAsync();
        Task<List<ProductoItem>> ObtenerTodosAsync();
        Task<ProductoItem?> ObtenerPorSkuAsync(string sku);
        Task GuardarOActualizarAsync(ProductoItem producto);
        Task GuardarLoteAsync(IEnumerable<ProductoItem> productos);
        Task<bool> EliminarPorSkuAsync(string sku);
        Task<bool> EliminarPorIdAsync(int id);
    }
}

