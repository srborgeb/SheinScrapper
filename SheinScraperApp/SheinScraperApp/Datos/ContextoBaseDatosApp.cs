using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Datos
{
    public class ContextoBaseDatosApp : DbContext
    {
        public DbSet<ProductoItem> Productos => Set<ProductoItem>();

        private readonly string _rutaBaseDatos;

        public ContextoBaseDatosApp(string? rutaPersonalizada = null)
        {
            if (!string.IsNullOrWhiteSpace(rutaPersonalizada))
            {
                _rutaBaseDatos = rutaPersonalizada;
            }
            else
            {
                string carpetaBase = AppDomain.CurrentDomain.BaseDirectory;
                _rutaBaseDatos = Path.Combine(carpetaBase, "shein_scraper.db");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder opciones)
        {
            opciones.UseSqlite($"Data Source={_rutaBaseDatos}");
        }

        protected override void OnModelCreating(ModelBuilder constructorModelos)
        {
            base.OnModelCreating(constructorModelos);

            constructorModelos.Entity<ProductoItem>(entidad =>
            {
                entidad.HasKey(e => e.Id);
                entidad.HasIndex(e => e.Sku);
                entidad.Property(e => e.Talla).HasMaxLength(50).HasDefaultValue(string.Empty);
                entidad.Property(e => e.Color).HasMaxLength(50).HasDefaultValue(string.Empty);
                entidad.Property(e => e.Cantidad).HasDefaultValue(1);
                entidad.Property(e => e.PrecioOriginal).HasPrecision(18, 2);
                entidad.Property(e => e.Impuesto).HasPrecision(18, 2);
                entidad.Property(e => e.Comision).HasPrecision(18, 2);
                entidad.Property(e => e.Total).HasPrecision(18, 2);
            });
        }
    }
}

