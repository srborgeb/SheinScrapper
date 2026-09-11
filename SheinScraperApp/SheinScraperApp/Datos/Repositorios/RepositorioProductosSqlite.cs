using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Datos.Repositorios
{
    public class RepositorioProductosSqlite : IRepositorioProductos
    {
        private readonly string? _rutaPersonalizada;

        public RepositorioProductosSqlite(string? rutaPersonalizada = null)
        {
            _rutaPersonalizada = rutaPersonalizada;
        }

        private ContextoBaseDatosApp CrearContexto() => new ContextoBaseDatosApp(_rutaPersonalizada);

        public async Task InicializarAsync()
        {
            using var contexto = CrearContexto();
            await contexto.Database.EnsureCreatedAsync();

            // Migración defensiva en caso de que la tabla previa no tenga Talla, Color o Cantidad
            try
            {
                await contexto.Database.ExecuteSqlRawAsync("ALTER TABLE Productos ADD COLUMN Talla TEXT DEFAULT '';");
            }
            catch { /* Ya existe */ }

            try
            {
                await contexto.Database.ExecuteSqlRawAsync("ALTER TABLE Productos ADD COLUMN Color TEXT DEFAULT '';");
            }
            catch { /* Ya existe */ }

            try
            {
                await contexto.Database.ExecuteSqlRawAsync("ALTER TABLE Productos ADD COLUMN Cantidad INTEGER DEFAULT 1;");
            }
            catch { /* Ya existe */ }

            try
            {
                await contexto.Database.ExecuteSqlRawAsync("ALTER TABLE Productos ADD COLUMN UrlProducto TEXT DEFAULT '';");
            }
            catch { /* Ya existe */ }

            try
            {
                await contexto.Database.ExecuteSqlRawAsync("ALTER TABLE Productos ADD COLUMN EstadoExitoso INTEGER DEFAULT 1;");
            }
            catch { /* Ya existe */ }

            try
            {
                await contexto.Database.ExecuteSqlRawAsync("ALTER TABLE Productos ADD COLUMN MensajeError TEXT DEFAULT '';");
            }
            catch { /* Ya existe */ }
        }

        public async Task<List<ProductoItem>> ObtenerTodosAsync()
        {
            using var contexto = CrearContexto();
            return await contexto.Productos
                .AsNoTracking()
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();
        }

        public async Task<ProductoItem?> ObtenerPorSkuAsync(string sku)
        {
            using var contexto = CrearContexto();
            return await contexto.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == sku);
        }

        public async Task GuardarOActualizarAsync(ProductoItem producto)
        {
            using var contexto = CrearContexto();
            var existente = await contexto.Productos.FirstOrDefaultAsync(p => p.Sku == producto.Sku);
            if (existente != null)
            {
                existente.Nombre = producto.Nombre;
                existente.Talla = producto.Talla;
                existente.Color = producto.Color;
                existente.Cantidad = producto.Cantidad;
                existente.PrecioOriginal = producto.PrecioOriginal;
                existente.Impuesto = producto.Impuesto;
                existente.Comision = producto.Comision;
                existente.Total = producto.Total;
                existente.NombreCliente = producto.NombreCliente;
                existente.UrlImagen = producto.UrlImagen;
                existente.RutaImagenLocal = producto.RutaImagenLocal;
                existente.FechaCreacion = DateTime.UtcNow;
            }
            else
            {
                await contexto.Productos.AddAsync(producto);
            }
            await contexto.SaveChangesAsync();
        }

        public async Task GuardarLoteAsync(IEnumerable<ProductoItem> productos)
        {
            using var contexto = CrearContexto();
            foreach (var producto in productos)
            {
                var existente = await contexto.Productos.FirstOrDefaultAsync(p => p.Sku == producto.Sku);
                if (existente != null)
                {
                    existente.Nombre = producto.Nombre;
                    existente.Talla = producto.Talla;
                    existente.Color = producto.Color;
                    existente.Cantidad = producto.Cantidad;
                    existente.PrecioOriginal = producto.PrecioOriginal;
                    existente.Impuesto = producto.Impuesto;
                    existente.Comision = producto.Comision;
                    existente.Total = producto.Total;
                    existente.NombreCliente = producto.NombreCliente;
                    existente.UrlImagen = producto.UrlImagen;
                    existente.RutaImagenLocal = producto.RutaImagenLocal;
                    existente.FechaCreacion = DateTime.UtcNow;
                }
                else
                {
                    await contexto.Productos.AddAsync(producto);
                }
            }
            await contexto.SaveChangesAsync();
        }

        public async Task<bool> EliminarPorSkuAsync(string sku)
        {
            using var contexto = CrearContexto();
            var registro = await contexto.Productos.FirstOrDefaultAsync(p => p.Sku == sku);
            if (registro != null)
            {
                contexto.Productos.Remove(registro);
                await contexto.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> EliminarPorIdAsync(int id)
        {
            using var contexto = CrearContexto();
            var registro = await contexto.Productos.FindAsync(id);
            if (registro != null)
            {
                contexto.Productos.Remove(registro);
                await contexto.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

