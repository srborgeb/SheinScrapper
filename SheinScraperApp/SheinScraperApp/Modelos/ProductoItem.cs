using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace SheinScraperApp.Modelos
{
    [Table("Productos")]
    public class ProductoItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Talla { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        public int Cantidad { get; set; } = 1;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecioOriginal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Impuesto { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Comision { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total { get; set; }

        [MaxLength(200)]
        public string NombreCliente { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string UrlImagen { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string RutaImagenLocal { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string UrlProducto { get; set; } = string.Empty;

        public bool EstadoExitoso { get; set; } = true;

        [MaxLength(500)]
        public string? MensajeError { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public Image? ImagenMiniatura { get; set; }

        public void Recalcular(decimal tasaImpuesto = 0.07m)
        {
            if (Cantidad < 1) Cantidad = 1;

            decimal subtotalMercaderia = Math.Round(PrecioOriginal * Cantidad, 2, MidpointRounding.AwayFromZero);
            Impuesto = Math.Round(subtotalMercaderia * tasaImpuesto, 2, MidpointRounding.AwayFromZero);
            decimal comisionTotal = Math.Round(Comision * Cantidad, 2, MidpointRounding.AwayFromZero);

            Total = Math.Round(subtotalMercaderia + Impuesto + comisionTotal, 2, MidpointRounding.AwayFromZero);
        }
    }
}

