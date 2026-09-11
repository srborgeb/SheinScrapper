using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace SheinScraperApp.Modelos
{
    [Table("Productos")]
    public class ProductoItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _id;
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private string _sku = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Sku
        {
            get => _sku;
            set { if (_sku != value) { _sku = value; OnPropertyChanged(); } }
        }

        private string _nombre = string.Empty;
        [Required]
        [MaxLength(500)]
        public string Nombre
        {
            get => _nombre;
            set { if (_nombre != value) { _nombre = value; OnPropertyChanged(); } }
        }

        private string _talla = string.Empty;
        [MaxLength(50)]
        public string Talla
        {
            get => _talla;
            set { if (_talla != value) { _talla = value; OnPropertyChanged(); } }
        }

        private string _color = string.Empty;
        [MaxLength(50)]
        public string Color
        {
            get => _color;
            set { if (_color != value) { _color = value; OnPropertyChanged(); } }
        }

        private int _cantidad = 1;
        public int Cantidad
        {
            get => _cantidad;
            set
            {
                int val = value < 1 ? 1 : value;
                if (_cantidad != val)
                {
                    _cantidad = val;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _precioOriginal;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecioOriginal
        {
            get => _precioOriginal;
            set
            {
                if (_precioOriginal != value)
                {
                    _precioOriginal = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _impuesto;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Impuesto
        {
            get => _impuesto;
            set
            {
                if (_impuesto != value)
                {
                    _impuesto = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _comision;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Comision
        {
            get => _comision;
            set
            {
                if (_comision != value)
                {
                    _comision = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _total;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total
        {
            get => _total;
            set
            {
                if (_total != value)
                {
                    _total = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _nombreCliente = string.Empty;
        [MaxLength(200)]
        public string NombreCliente
        {
            get => _nombreCliente;
            set { if (_nombreCliente != value) { _nombreCliente = value; OnPropertyChanged(); } }
        }

        private string _urlImagen = string.Empty;
        [MaxLength(1000)]
        public string UrlImagen
        {
            get => _urlImagen;
            set { if (_urlImagen != value) { _urlImagen = value; OnPropertyChanged(); } }
        }

        private string _rutaImagenLocal = string.Empty;
        [MaxLength(1000)]
        public string RutaImagenLocal
        {
            get => _rutaImagenLocal;
            set { if (_rutaImagenLocal != value) { _rutaImagenLocal = value; OnPropertyChanged(); } }
        }

        private string _urlProducto = string.Empty;
        [MaxLength(2000)]
        public string UrlProducto
        {
            get => _urlProducto;
            set { if (_urlProducto != value) { _urlProducto = value; OnPropertyChanged(); } }
        }

        private bool _estadoExitoso = true;
        public bool EstadoExitoso
        {
            get => _estadoExitoso;
            set { if (_estadoExitoso != value) { _estadoExitoso = value; OnPropertyChanged(); } }
        }

        private string? _mensajeError;
        [MaxLength(500)]
        public string? MensajeError
        {
            get => _mensajeError;
            set { if (_mensajeError != value) { _mensajeError = value; OnPropertyChanged(); } }
        }

        private DateTime _fechaCreacion = DateTime.UtcNow;
        public DateTime FechaCreacion
        {
            get => _fechaCreacion;
            set { if (_fechaCreacion != value) { _fechaCreacion = value; OnPropertyChanged(); } }
        }

        private Image? _imagenMiniatura;
        [NotMapped]
        public Image? ImagenMiniatura
        {
            get => _imagenMiniatura;
            set { if (_imagenMiniatura != value) { _imagenMiniatura = value; OnPropertyChanged(); } }
        }

        public void Recalcular(decimal tasaImpuesto = 0.07m)
        {
            if (_cantidad < 1) _cantidad = 1;

            decimal subtotalMercaderia = Math.Round(_precioOriginal * _cantidad, 2, MidpointRounding.AwayFromZero);
            Impuesto = Math.Round(subtotalMercaderia * tasaImpuesto, 2, MidpointRounding.AwayFromZero);
            decimal comisionTotal = Math.Round(_comision * _cantidad, 2, MidpointRounding.AwayFromZero);

            Total = Math.Round(subtotalMercaderia + Impuesto + comisionTotal, 2, MidpointRounding.AwayFromZero);
        }
    }
}

