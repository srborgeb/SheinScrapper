using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Telerik.WinControls;
using Telerik.WinControls.Themes;
using Telerik.WinControls.UI;
using SheinScraperApp.Datos.Repositorios;
using SheinScraperApp.Modelos;
using SheinScraperApp.Servicios;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SheinScraperApp.Formularios
{
    public partial class FormularioPrincipalRad : RadForm
    {
        private readonly IRepositorioProductos _repositorioProductos;
        private readonly IServicioScraperShein _servicioScraper;
        private readonly IServicioAlmacenamientoImagenes _almacenamientoImagenes;
        private readonly IServicioExportacionPdf _servicioPdf;
        private readonly IServicioCalculo _servicioCalculo;

        private readonly BindingList<ProductoItem> _listaProductos = new BindingList<ProductoItem>();
        private CancellationTokenSource? _origenCancelacion;
        private readonly string _carpetaImagenes;

        public FormularioPrincipalRad()
        {
            InitializeComponent();

            _carpetaImagenes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes");
            if (!Directory.Exists(_carpetaImagenes))
            {
                Directory.CreateDirectory(_carpetaImagenes);
            }

            _servicioCalculo = new ServicioCalculo();
            _almacenamientoImagenes = new ServicioAlmacenamientoImagenesLocal();
            _servicioScraper = new ServicioScraperShein(_almacenamientoImagenes, _servicioCalculo);
            _repositorioProductos = new RepositorioProductosSqlite();
            _servicioPdf = new ServicioExportacionPdf();

            ConfigurarVentana();
            ConfigurarIconosFontAwesome();
            ConfigurarGrilla();
        }

        private async void FormularioPrincipalRad_Load(object sender, EventArgs e)
        {
            await _repositorioProductos.InicializarAsync();
            await CargarProductosDesdeBaseDatosAsync();
        }

        private void ConfigurarVentana()
        {
            ThemeResolutionService.ApplicationThemeName = "Fluent";
            this.ThemeName = "Fluent";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Extractor de Productos Shein Pro - Telerik & FontAwesome";

            botonDetener.Enabled = false;
            barraProgresoExtraccion.Visible = false;
        }

        private void ConfigurarIconosFontAwesome()
        {
            botonIniciarExtraccion.Image = IconChar.Play.ToBitmap(Color.White, 16);
            botonIniciarExtraccion.TextImageRelation = TextImageRelation.ImageBeforeText;

            botonDetener.Image = IconChar.Stop.ToBitmap(Color.White, 16);
            botonDetener.TextImageRelation = TextImageRelation.ImageBeforeText;

            botonImportarTxt.Image = IconChar.FileImport.ToBitmap(Color.White, 16);
            botonImportarTxt.TextImageRelation = TextImageRelation.ImageBeforeText;

            botonExportarPdf.Image = IconChar.FilePdf.ToBitmap(Color.White, 16);
            botonExportarPdf.TextImageRelation = TextImageRelation.ImageBeforeText;

            botonRecargarBaseDatos.Image = IconChar.Rotate.ToBitmap(Color.White, 16);
            botonRecargarBaseDatos.TextImageRelation = TextImageRelation.ImageBeforeText;

            botonLimpiarTabla.Image = IconChar.Broom.ToBitmap(Color.White, 16);
            botonLimpiarTabla.TextImageRelation = TextImageRelation.ImageBeforeText;
        }

        private void ConfigurarGrilla()
        {
            grillaProductos.ThemeName = "Fluent";
            grillaProductos.AutoGenerateColumns = false;
            grillaProductos.AllowAddNewRow = false;
            grillaProductos.AllowDeleteRow = false;
            grillaProductos.AllowEditRow = true;
            grillaProductos.EnableFiltering = true;
            grillaProductos.EnableSorting = true;
            grillaProductos.ShowFilteringRow = true;
            grillaProductos.AutoSizeRows = false;
            grillaProductos.TableElement.RowHeight = 110;
            grillaProductos.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;

            grillaProductos.Columns.Clear();

            // 1. Imagen (Miniatura fija para mantener proporción)
            var columnaImagen = new GridViewImageColumn("ImagenMiniatura")
            {
                HeaderText = "Imagen",
                Width = 110,
                MinWidth = 110,
                MaxWidth = 115,
                ImageLayout = ImageLayout.Zoom,
                ReadOnly = true,
                AllowResize = false,
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaImagen);

            // 2. Nombre del Producto (Elástica principal)
            var columnaNombre = new GridViewTextBoxColumn("Nombre")
            {
                HeaderText = "Nombre del Producto",
                Width = 260,
                MinWidth = 180,
                WrapText = true,
                ReadOnly = false
            };
            grillaProductos.Columns.Add(columnaNombre);

            // 3. SKU
            var columnaSku = new GridViewTextBoxColumn("Sku")
            {
                HeaderText = "SKU",
                Width = 110,
                MinWidth = 95,
                ReadOnly = true,
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaSku);

            // 4. Talla
            var columnaTalla = new GridViewTextBoxColumn("Talla")
            {
                HeaderText = "Talla",
                Width = 70,
                MinWidth = 60,
                MaxWidth = 85,
                ReadOnly = false,
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaTalla);

            // 5. Color
            var columnaColor = new GridViewTextBoxColumn("Color")
            {
                HeaderText = "Color",
                Width = 85,
                MinWidth = 75,
                MaxWidth = 110,
                ReadOnly = false,
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaColor);

            // 6. Cantidad (Unidades)
            var columnaCantidad = new GridViewDecimalColumn("Cantidad")
            {
                HeaderText = "Unidades",
                Width = 75,
                MinWidth = 70,
                MaxWidth = 90,
                DecimalPlaces = 0,
                Minimum = 1,
                Maximum = 9999,
                ReadOnly = false,
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaCantidad);

            // 7. Precio Unitario (Siempre en $)
            var columnaPrecio = new GridViewDecimalColumn("PrecioOriginal")
            {
                HeaderText = "Precio Unit.",
                Width = 95,
                MinWidth = 85,
                MaxWidth = 120,
                FormatString = "${0:N2}",
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 999999,
                ReadOnly = false,
                TextAlignment = ContentAlignment.MiddleRight
            };
            grillaProductos.Columns.Add(columnaPrecio);

            // 8. Impuesto (7%) (Siempre en $)
            var columnaImpuesto = new GridViewDecimalColumn("Impuesto")
            {
                HeaderText = "Impuesto (7%)",
                Width = 95,
                MinWidth = 85,
                MaxWidth = 120,
                FormatString = "${0:N2}",
                DecimalPlaces = 2,
                ReadOnly = true,
                TextAlignment = ContentAlignment.MiddleRight
            };
            grillaProductos.Columns.Add(columnaImpuesto);

            // 9. Comisión (Siempre en $)
            var columnaComision = new GridViewDecimalColumn("Comision")
            {
                HeaderText = "Comisión",
                Width = 95,
                MinWidth = 85,
                MaxWidth = 120,
                FormatString = "${0:N2}",
                DecimalPlaces = 2,
                ReadOnly = false,
                TextAlignment = ContentAlignment.MiddleRight
            };
            grillaProductos.Columns.Add(columnaComision);

            // 10. Sumatoria (Siempre en $)
            var columnaTotal = new GridViewDecimalColumn("Total")
            {
                HeaderText = "Sumatoria",
                Width = 105,
                MinWidth = 95,
                MaxWidth = 135,
                FormatString = "${0:N2}",
                DecimalPlaces = 2,
                ReadOnly = true,
                TextAlignment = ContentAlignment.MiddleRight
            };
            grillaProductos.Columns.Add(columnaTotal);

            // 11. Nombre Cliente (Elástica secundaria)
            var columnaCliente = new GridViewTextBoxColumn("NombreCliente")
            {
                HeaderText = "Nombre Cliente",
                Width = 135,
                MinWidth = 110,
                ReadOnly = false
            };
            grillaProductos.Columns.Add(columnaCliente);

            // 12. Botón Reintentar
            var columnaReintentar = new GridViewCommandColumn("ColumnaReintentar")
            {
                HeaderText = "Reintentar",
                Width = 90,
                MinWidth = 85,
                MaxWidth = 95,
                UseDefaultText = true,
                DefaultText = "Reintentar",
                Image = IconChar.RotateRight.ToBitmap(Color.FromArgb(13, 110, 253), 14),
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaReintentar);

            // 13. Botón Abrir URL
            var columnaAbrirUrl = new GridViewCommandColumn("ColumnaAbrirUrl")
            {
                HeaderText = "Enlace",
                Width = 75,
                MinWidth = 70,
                MaxWidth = 80,
                UseDefaultText = true,
                DefaultText = "Abrir",
                Image = IconChar.ArrowUpRightFromSquare.ToBitmap(Color.FromArgb(108, 117, 125), 14),
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaAbrirUrl);

            // 14. Botón Eliminar
            var columnaEliminar = new GridViewCommandColumn("ColumnaEliminar")
            {
                HeaderText = "Acción",
                Width = 85,
                MinWidth = 80,
                MaxWidth = 90,
                UseDefaultText = true,
                DefaultText = "Eliminar",
                Image = IconChar.TrashCan.ToBitmap(Color.FromArgb(220, 53, 69), 15),
                TextAlignment = ContentAlignment.MiddleCenter
            };
            grillaProductos.Columns.Add(columnaEliminar);

            // Enlazar datos
            grillaProductos.DataSource = _listaProductos;

            // Eventos
            grillaProductos.CommandCellClick += GrillaProductos_CommandCellClick;
            grillaProductos.CellValueChanged += GrillaProductos_CellValueChanged;
            grillaProductos.CellEndEdit += GrillaProductos_CellEndEdit;
            grillaProductos.RowFormatting += GrillaProductos_RowFormatting;
        }

        private void GrillaProductos_RowFormatting(object sender, RowFormattingEventArgs e)
        {
            if (e.RowElement.RowInfo?.DataBoundItem is ProductoItem producto)
            {
                if (!producto.EstadoExitoso)
                {
                    e.RowElement.DrawFill = true;
                    e.RowElement.GradientStyle = GradientStyles.Solid;
                    e.RowElement.BackColor = Color.FromArgb(254, 242, 242);
                }
                else
                {
                    e.RowElement.ResetValue(LightVisualElement.BackColorProperty, ValueResetFlags.Local);
                    e.RowElement.ResetValue(LightVisualElement.GradientStyleProperty, ValueResetFlags.Local);
                    e.RowElement.ResetValue(LightVisualElement.DrawFillProperty, ValueResetFlags.Local);
                }
            }
        }

        private async void GrillaProductos_CommandCellClick(object sender, GridViewCellEventArgs e)
        {
            if (e.Row?.DataBoundItem is not ProductoItem producto) return;

            if (e.Column.Name == "ColumnaEliminar")
            {
                var respuesta = RadMessageBox.Show(
                    this,
                    $"¿Deseas eliminar el registro '{producto.Nombre}' (SKU: {producto.Sku})?\nSe borrará de la tabla y de SQLite.",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    RadMessageIcon.Question);

                if (respuesta == DialogResult.Yes)
                {
                    await _repositorioProductos.EliminarPorSkuAsync(producto.Sku);
                    _listaProductos.Remove(producto);
                    ActualizarBarraEstadoResumen();
                }
            }
            else if (e.Column.Name == "ColumnaAbrirUrl")
            {
                if (!string.IsNullOrWhiteSpace(producto.UrlProducto))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(producto.UrlProducto) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        RadMessageBox.Show(this, $"No se pudo abrir el enlace: {ex.Message}", "Error al Abrir", MessageBoxButtons.OK, RadMessageIcon.Error);
                    }
                }
                else
                {
                    RadMessageBox.Show(this, "Este registro no posee una URL de producto asociada.", "URL No Disponible", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                }
            }
            else if (e.Column.Name == "ColumnaReintentar")
            {
                if (string.IsNullOrWhiteSpace(producto.UrlProducto))
                {
                    RadMessageBox.Show(this, "Este registro no posee una URL para reintentar la extracción.", "URL Requerida", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                etiquetaEstadoProgreso.Text = $"Reintentando extracción de: {producto.UrlProducto}...";
                barraProgresoExtraccion.Value1 = 30;

                try
                {
                    var actualizado = await _servicioScraper.ExtraerIndividualAsync(
                        producto.UrlProducto,
                        _carpetaImagenes,
                        producto.Comision,
                        producto.NombreCliente,
                        producto.Talla,
                        producto.Color,
                        producto.Cantidad);

                    if (actualizado.EstadoExitoso)
                    {
                        producto.Sku = actualizado.Sku;
                        producto.Nombre = actualizado.Nombre;
                        producto.PrecioOriginal = actualizado.PrecioOriginal;
                        producto.Impuesto = actualizado.Impuesto;
                        producto.Total = actualizado.Total;
                        producto.UrlImagen = actualizado.UrlImagen;
                        producto.RutaImagenLocal = actualizado.RutaImagenLocal;
                        producto.ImagenMiniatura = actualizado.ImagenMiniatura;
                        producto.EstadoExitoso = true;
                        producto.MensajeError = null;

                        await _repositorioProductos.GuardarOActualizarAsync(producto);
                        grillaProductos.TableElement.Update(GridUINotifyAction.DataChanged);
                        ActualizarBarraEstadoResumen();

                        barraProgresoExtraccion.Value1 = 100;
                        etiquetaEstadoProgreso.Text = $"✓ Producto reintentado con éxito: {producto.Sku}";
                        RadMessageBox.Show(this, $"Producto resuelto y actualizado exitosamente:\n{producto.Nombre}\nPrecio: ${producto.PrecioOriginal:N2}", "Reintento Exitoso", MessageBoxButtons.OK, RadMessageIcon.Info);
                    }
                    else
                    {
                        barraProgresoExtraccion.Value1 = 0;
                        etiquetaEstadoProgreso.Text = "⚠ El reintento no pudo resolver el producto.";
                        RadMessageBox.Show(this, $"El enlace continúa sin poder resolverse:\n{actualizado.MensajeError}", "Reintento Fallido", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    barraProgresoExtraccion.Value1 = 0;
                    etiquetaEstadoProgreso.Text = $"Error en reintento: {ex.Message}";
                    RadMessageBox.Show(this, $"Error al reintentar: {ex.Message}", "Error de Conexión", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            }
        }

        private async void GrillaProductos_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            if (e.Row?.DataBoundItem is ProductoItem producto)
            {
                string colName = e.Column?.Name ?? string.Empty;
                string fieldName = e.Column?.FieldName ?? string.Empty;

                if (colName == "Cantidad" || fieldName == "Cantidad" ||
                    colName == "PrecioOriginal" || fieldName == "PrecioOriginal" ||
                    colName == "Comision" || fieldName == "Comision")
                {
                    producto.Recalcular();
                    grillaProductos.TableElement.Update(GridUINotifyAction.DataChanged);
                }

                await _repositorioProductos.GuardarOActualizarAsync(producto);
                ActualizarBarraEstadoResumen();
            }
        }

        private void GrillaProductos_CellEndEdit(object sender, GridViewCellEventArgs e)
        {
            if (e.Row?.DataBoundItem is ProductoItem producto)
            {
                producto.Recalcular();
                grillaProductos.TableElement.Update(GridUINotifyAction.DataChanged);
            }
            ActualizarBarraEstadoResumen();
        }

        private async Task CargarProductosDesdeBaseDatosAsync()
        {
            var registros = await _repositorioProductos.ObtenerTodosAsync();
            _listaProductos.Clear();

            var productosParaActualizarRuta = new List<ProductoItem>();

            foreach (var producto in registros)
            {
                // Resolver ruta de imagen dinámicamente si la carpeta del aplicativo fue movida a otro equipo/directorio
                string rutaResuelta = _almacenamientoImagenes.ResolverRutaLocal(producto.RutaImagenLocal, _carpetaImagenes, producto.Sku);
                if (!string.IsNullOrEmpty(rutaResuelta))
                {
                    if (!rutaResuelta.Equals(producto.RutaImagenLocal, StringComparison.OrdinalIgnoreCase))
                    {
                        producto.RutaImagenLocal = rutaResuelta;
                        productosParaActualizarRuta.Add(producto);
                    }
                    producto.ImagenMiniatura = _almacenamientoImagenes.CargarMiniatura(rutaResuelta, 110, 110);
                }

                _listaProductos.Add(producto);
            }

            // Sincronizar silenciosamente las nuevas rutas locales en la base de datos si cambiaron
            if (productosParaActualizarRuta.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        foreach (var p in productosParaActualizarRuta)
                        {
                            await _repositorioProductos.GuardarOActualizarAsync(p);
                        }
                    }
                    catch { }
                });
            }

            ActualizarBarraEstadoResumen();
        }

        private void ActualizarBarraEstadoResumen()
        {
            int totalUnidades = _listaProductos.Sum(p => p.Cantidad);
            decimal totalVenta = _listaProductos.Sum(p => p.Total);
            decimal totalComision = _listaProductos.Sum(p => p.Comision * p.Cantidad);
            etiquetaEstadoResumen.Text = $"Total Artículos: {_listaProductos.Count} ({totalUnidades} Unidades) | TOTAL GENERAL: ${totalVenta:N2} | Total Comisión: ${totalComision:N2}";
        }

        private void BotonImportarTxt_Click(object sender, EventArgs e)
        {
            using var selectorArchivo = new OpenFileDialog();
            selectorArchivo.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            selectorArchivo.Title = "Seleccionar archivo TXT con enlaces de Shein";

            if (selectorArchivo.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var lineas = File.ReadAllLines(selectorArchivo.FileName)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l) && Uri.IsWellFormedUriString(l, UriKind.Absolute))
                        .Distinct()
                        .ToList();

                    if (lineas.Count > 0)
                    {
                        if (string.IsNullOrWhiteSpace(cuadroTextoUrls.Text))
                        {
                            cuadroTextoUrls.Text = string.Join(Environment.NewLine, lineas);
                        }
                        else
                        {
                            cuadroTextoUrls.Text += Environment.NewLine + string.Join(Environment.NewLine, lineas);
                        }

                        RadMessageBox.Show(this, $"Se cargaron {lineas.Count} enlaces desde el archivo TXT al cuadro de texto.", "Importación Exitosa", MessageBoxButtons.OK, RadMessageIcon.Info);
                    }
                    else
                    {
                        RadMessageBox.Show(this, "No se encontraron URLs válidas en el archivo seleccionado.", "Sin URLs", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    RadMessageBox.Show(this, $"Error al leer el archivo TXT: {ex.Message}", "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            }
        }

        private async void BotonIniciarExtraccion_Click(object sender, EventArgs e)
        {
            var enlaces = cuadroTextoUrls.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .Where(u => !string.IsNullOrEmpty(u) && Uri.IsWellFormedUriString(u, UriKind.Absolute))
                .Distinct()
                .ToList();

            if (enlaces.Count == 0)
            {
                RadMessageBox.Show(this, "Por favor ingresa al menos una URL de Shein (puedes pegarla directamente o usar 'Importar TXT').", "URLs Requeridas", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            decimal comision = (decimal)controlComision.Value;
            string cliente = cuadroTextoCliente.Text.Trim();
            string talla = cuadroTextoTalla.Text.Trim();
            string color = cuadroTextoColor.Text.Trim();
            int cantidad = (int)controlCantidad.Value;

            botonIniciarExtraccion.Enabled = false;
            botonDetener.Enabled = true;
            botonImportarTxt.Enabled = false;
            botonExportarPdf.Enabled = false;
            barraProgresoExtraccion.Visible = true;
            barraProgresoExtraccion.Value1 = 0;
            barraProgresoExtraccion.Maximum = 100;
            etiquetaEstadoProgreso.Text = $"Iniciando extracción de {enlaces.Count} productos...";

            _origenCancelacion = new CancellationTokenSource();

            var reporteProgreso = new Progress<ReporteProgresoScraping>(reporte =>
            {
                barraProgresoExtraccion.Value1 = reporte.Porcentaje;
                etiquetaEstadoProgreso.Text = $"({reporte.IndiceActual}/{reporte.TotalRegistros}) {reporte.Mensaje}";

                if (reporte.Producto != null)
                {
                    _listaProductos.Insert(0, reporte.Producto);
                    _ = _repositorioProductos.GuardarOActualizarAsync(reporte.Producto);
                    ActualizarBarraEstadoResumen();
                }
            });

            try
            {
                var extraidos = await _servicioScraper.ExtraerLoteAsync(
                    enlaces,
                    _carpetaImagenes,
                    comision,
                    cliente,
                    talla,
                    color,
                    cantidad,
                    reporteProgreso,
                    _origenCancelacion.Token);

                etiquetaEstadoProgreso.Text = $"✓ Extracción finalizada. {extraidos.Count} productos procesados correctamente.";
                RadMessageBox.Show(this, $"Extracción completada.\nSe procesaron {extraidos.Count} productos y se guardaron en SQLite.", "Éxito", MessageBoxButtons.OK, RadMessageIcon.Info);
            }
            catch (OperationCanceledException)
            {
                etiquetaEstadoProgreso.Text = "⚠ Extracción cancelada por el usuario.";
                RadMessageBox.Show(this, "La extracción fue detenida correctamente.", "Operación Cancelada", MessageBoxButtons.OK, RadMessageIcon.Info);
            }
            catch (ExcepcionCaptchaShein exCaptcha)
            {
                etiquetaEstadoProgreso.Text = "⚠ Shein requiere verificación de Captcha.";
                barraProgresoExtraccion.Value1 = 0;

                var respuesta = RadMessageBox.Show(
                    this,
                    $"Shein ha solicitado resolver un Captcha de seguridad y los métodos automáticos no pudieron superarlo.\n\n" +
                    $"Quedan {exCaptcha.UrlsPendientes.Count} enlaces pendientes por procesar.\n\n" +
                    $"¿Deseas abrir una ventana de navegación para resolver el Captcha manualmente y continuar con el lote?",
                    "Captcha Detectado",
                    MessageBoxButtons.YesNo,
                    RadMessageIcon.Question);

                if (respuesta == DialogResult.Yes)
                {
                    await ManejarResolucionManualCaptchaAsync(exCaptcha, comision, cliente, talla, color, cantidad, reporteProgreso);
                }
                else
                {
                    etiquetaEstadoProgreso.Text = $"Extracción detenida por Captcha. {exCaptcha.UrlsPendientes.Count} enlaces quedaron pendientes.";
                    cuadroTextoUrls.Text = string.Join(Environment.NewLine, exCaptcha.UrlsPendientes);
                }
            }
            catch (Exception ex)
            {
                etiquetaEstadoProgreso.Text = $"Error: {ex.Message}";
                RadMessageBox.Show(this, $"Ocurrió un problema: {ex.Message}", "Error en Extracción", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
            finally
            {
                botonIniciarExtraccion.Enabled = true;
                botonDetener.Enabled = false;
                botonImportarTxt.Enabled = true;
                botonExportarPdf.Enabled = true;
                _origenCancelacion?.Dispose();
                _origenCancelacion = null;
            }
        }

        private void BotonDetener_Click(object sender, EventArgs e)
        {
            if (_origenCancelacion != null && !_origenCancelacion.IsCancellationRequested)
            {
                botonDetener.Enabled = false;
                etiquetaEstadoProgreso.Text = "Deteniendo proceso... Por favor espera.";
                _origenCancelacion.Cancel();
            }
        }

        private async Task ManejarResolucionManualCaptchaAsync(
            ExcepcionCaptchaShein exCaptcha,
            decimal comision,
            string cliente,
            string talla,
            string color,
            int cantidad,
            IProgress<ReporteProgresoScraping> reporteProgreso)
        {
            IWebDriver? navegadorVisible = null;
            try
            {
                etiquetaEstadoProgreso.Text = "Abriendo ventana para resolución manual de Captcha...";

                navegadorVisible = GestorNavegadorChrome.CrearNavegador(modoSinCabeza: false);
                navegadorVisible.Navigate().GoToUrl(exCaptcha.UrlDondeOcurrio);

                RadMessageBox.Show(
                    this,
                    "Se ha abierto la ventana del navegador.\n\n" +
                    "Por favor resuelve el rompecabezas o Captcha en la ventana de Shein.\n\n" +
                    "Una vez que hayas completado la verificación y veas el producto, haz clic en 'Aceptar' en este mensaje para continuar en modo oculto.",
                    "Resolver Captcha Manualmente",
                    MessageBoxButtons.OK,
                    RadMessageIcon.Info);
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(this, $"No se pudo abrir la ventana del navegador: {ex.Message}", "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                return;
            }
            finally
            {
                if (navegadorVisible != null)
                {
                    try { navegadorVisible.Quit(); } catch { }
                    try { navegadorVisible.Dispose(); } catch { }
                }
            }

            // Continuar en modo 100% oculto con los enlaces pendientes
            if (exCaptcha.UrlsPendientes.Count > 0)
            {
                etiquetaEstadoProgreso.Text = $"Reanudando extracción silenciosa de {exCaptcha.UrlsPendientes.Count} enlaces restantes...";
                _origenCancelacion = new CancellationTokenSource();

                try
                {
                    var extraidosRestantes = await _servicioScraper.ExtraerLoteAsync(
                        exCaptcha.UrlsPendientes,
                        _carpetaImagenes,
                        comision,
                        cliente,
                        talla,
                        color,
                        cantidad,
                        reporteProgreso,
                        _origenCancelacion.Token);

                    etiquetaEstadoProgreso.Text = $"✓ Lote finalizado. {extraidosRestantes.Count} productos procesados tras resolver el Captcha.";
                    RadMessageBox.Show(this, $"Extracción completada.\nSe procesaron los {extraidosRestantes.Count} productos restantes con éxito.", "Lote Completado", MessageBoxButtons.OK, RadMessageIcon.Info);
                }
                catch (Exception ex)
                {
                    etiquetaEstadoProgreso.Text = $"Error al reanudar lote: {ex.Message}";
                    RadMessageBox.Show(this, $"Error al reanudar el lote: {ex.Message}", "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            }
        }

        private async void BotonExportarPdf_Click(object sender, EventArgs e)
        {
            grillaProductos.EndEdit();

            if (_listaProductos.Count == 0)
            {
                RadMessageBox.Show(this, "No hay productos en la tabla para exportar la estimación en PDF.", "Tabla Vacía", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            // Clientes únicos presentes en la tabla
            var clientesEnGrid = _listaProductos
                .Select(p => p.NombreCliente)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Si hay un cliente escrito en el cuadro de texto que aún no esté en la lista, lo agregamos
            string clienteActual = cuadroTextoCliente.Text.Trim();
            if (!string.IsNullOrWhiteSpace(clienteActual) && !clientesEnGrid.Contains(clienteActual, StringComparer.OrdinalIgnoreCase))
            {
                clientesEnGrid.Add(clienteActual);
            }

            // Consultar el cliente por medio del formulario con ListBox
            using var dialogoSeleccion = new FormularioSeleccionCliente(clientesEnGrid);
            if (dialogoSeleccion.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string clienteSeleccionado = dialogoSeleccion.ClienteSeleccionado;

            // Directorio de exportación: [DirectorioDelAplicativo]\PDF\*.pdf
            string carpetaPdf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PDF");
            if (!Directory.Exists(carpetaPdf))
            {
                Directory.CreateDirectory(carpetaPdf);
            }

            try
            {
                etiquetaEstadoProgreso.Text = "Generando estimación en PDF...";
                barraProgresoExtraccion.Value1 = 50;

                string rutaGenerada = await _servicioPdf.ExportarEstimacionPdfAsync(_listaProductos, clienteSeleccionado, carpetaPdf);

                barraProgresoExtraccion.Value1 = 100;
                etiquetaEstadoProgreso.Text = $"PDF guardado: {Path.GetFileName(rutaGenerada)}";

                var abrir = RadMessageBox.Show(
                    this,
                    $"Estimación PDF generada exitosamente en el directorio PDF:\n\n{rutaGenerada}\n\n¿Deseas abrir el archivo PDF ahora?",
                    "Estimación Generada",
                    MessageBoxButtons.YesNo,
                    RadMessageIcon.Info);

                if (abrir == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(rutaGenerada) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                etiquetaEstadoProgreso.Text = "Error al exportar PDF.";
                RadMessageBox.Show(this, $"Error al generar el archivo PDF: {ex.Message}", "Error de Exportación", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private async void BotonRecargarBaseDatos_Click(object sender, EventArgs e)
        {
            await CargarProductosDesdeBaseDatosAsync();
        }

        private void BotonLimpiarTabla_Click(object sender, EventArgs e)
        {
            var confirmacion = RadMessageBox.Show(
                this,
                "¿Deseas vaciar la vista actual de la tabla? (Los registros continuarán en la base de datos SQLite).",
                "Limpiar Vista",
                MessageBoxButtons.YesNo,
                RadMessageIcon.Question);

            if (confirmacion == DialogResult.Yes)
            {
                _listaProductos.Clear();
                ActualizarBarraEstadoResumen();
            }
        }
    }
}
