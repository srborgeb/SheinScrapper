using AngleSharp.Text;
using OfficeOpenXml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SheinScraperApp
{
    public partial class formScrap : Form
    {
        private string _carpetaSeleccionada = "";
        private string _rutaPerfilChrome; // Antes _chromeUserProfilePath

        // --- ESTRUCTURA DE DATOS PARA MULTIPLES PRODUCTOS ---
        public class ProductoShein
        {
            public string Sku { get; set; }
            public string Nombre { get; set; }
            public double Precio { get; set; }
            public string Descuento { get; set; }
            public string ImagenUrl { get; set; }
            public string RutaImagenLocal { get; set; }
        }

        // Lista global para mantener los productos extraídos
        private List<ProductoShein> _listaProductos = new List<ProductoShein>();

        public formScrap()
        {
            InitializeComponent();
            _rutaPerfilChrome = Path.Combine(Path.GetTempPath(), "SheinScraperChromeProfile");
            Directory.CreateDirectory(_rutaPerfilChrome);

            // Habilitar soporte multilínea (por si no lo has hecho en el diseñador visual)
            txtUrlProducto.Multiline = true;
            txtUrlProducto.ScrollBars = ScrollBars.Vertical;

            txtUrlProducto.Enter += TxtUrlProducto_Enter;
            txtUrlProducto.Text = "";
        }

        string _separadorDecimal = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        private void TxtUrlProducto_Enter(object sender, EventArgs e)
        {
            txtUrlProducto.SelectAll();
        }

        private void Valor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != _separadorDecimal[0])
            {
                e.Handled = true;
            }
        }

        private void Nombre_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void Talla_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Omitido como solicitaste
        }

        private void btnSeleccionarDirectorio_Click(object sender, EventArgs e)
        {
            using (var exploradorCarpetas = new FolderBrowserDialog())
            {
                DialogResult resultado = exploradorCarpetas.ShowDialog();
                if (resultado == DialogResult.OK && !string.IsNullOrWhiteSpace(exploradorCarpetas.SelectedPath))
                {
                    _carpetaSeleccionada = exploradorCarpetas.SelectedPath;
                    lblDirectorio.Text = $"Carpeta: {_carpetaSeleccionada}";
                }
            }
        }

        private async void btnScrape_Click(object sender, EventArgs e)
        {
            // 1. Extraer todas las URLs validas del cuadro de texto multilínea
            var urlsSinProcesar = txtUrlProducto.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> urlsValidas = new List<string>();

            foreach (var url in urlsSinProcesar)
            {
                string urlLimpia = url.Trim();
                if (!string.IsNullOrEmpty(urlLimpia) && Uri.IsWellFormedUriString(urlLimpia, UriKind.Absolute))
                {
                    urlsValidas.Add(urlLimpia);
                }
            }

            if (urlsValidas.Count == 0)
            {
                MessageBox.Show("Por favor, introduce al menos una URL de producto válida.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_carpetaSeleccionada) || !Directory.Exists(_carpetaSeleccionada))
            {
                MessageBox.Show("Por favor, selecciona una carpeta válida para guardar las imágenes y el Excel antes de scrapear.", "Carpeta no seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EnvioTextBox.Text) || string.IsNullOrWhiteSpace(ClienteTextBox.Text))
            {
                MessageBox.Show("Por favor, completa los campos de 'Envío' y 'Cliente' para que el Excel final se calcule correctamente.", "Faltan Datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbResultado.Clear();
            rtbResultado.AppendText($"Se encontraron {urlsValidas.Count} enlaces para procesar.\n");
            rtbResultado.AppendText("Iniciando extracción con Selenium... Por favor, espera.\n\n");

            btnScrape.Enabled = false;
            _listaProductos.Clear(); // Limpiamos la lista para un nuevo lote

            IWebDriver navegador = null; // Antes 'driver'
            try
            {
                // Configuración de Selenium (Ocurre UNA sola vez por lote)
                new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
                var servicio = ChromeDriverService.CreateDefaultService();
                servicio.SuppressInitialDiagnosticInformation = true;

                ChromeOptions opciones = new ChromeOptions();
                opciones.AddArgument($"--user-data-dir={_rutaPerfilChrome}");
                opciones.AddArgument("--profile-directory=Default");
                opciones.AddArgument("--disable-blink-features=AutomationControlled");
                opciones.AddExcludedArgument("enable-automation");
                opciones.AddArgument("--disable-infobars");
                opciones.AddArgument("--start-maximized");
                opciones.AddArgument("--no-sandbox");
                opciones.AddArgument("--disable-dev-shm-usage");
                opciones.AddArgument("--disable-gpu");
                opciones.AddArgument("--lang=es");

                navegador = new ChromeDriver(servicio, opciones);
                navegador.Manage().Window.Maximize();
                System.Threading.Thread.Sleep(2000);

                navegador.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                navegador.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(90);
                WebDriverWait esperaLarga = new WebDriverWait(navegador, TimeSpan.FromSeconds(60));

                bool popupsCerrados = false; // Variable para saber si ya libramos la página del pop-up

                // --- BUCLE MASIVO: PROCESAR CADA URL ---
                int contador = 1;
                foreach (string urlProducto in urlsValidas)
                {
                    rtbResultado.AppendText($"--- Procesando {contador}/{urlsValidas.Count} ---\n");
                    rtbResultado.AppendText($"URL: {urlProducto}\n");

                    try
                    {
                        navegador.Navigate().GoToUrl(urlProducto);

                        // Cerrar pop-up de cookies (Solo lo intentamos seriamente en el primer enlace o si no se ha cerrado)
                        if (!popupsCerrados)
                        {
                            try
                            {
                                WebDriverWait esperaCorta = new WebDriverWait(navegador, TimeSpan.FromSeconds(5));
                                IWebElement botonAceptarCookies = esperaCorta.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".cookie-popup-accept-button, #onetrust-accept-btn-handler, [aria-label='Aceptar cookies'], .shein-button-black, .s-btn-primary")));

                                if (botonAceptarCookies != null && botonAceptarCookies.Displayed)
                                {
                                    botonAceptarCookies.Click();
                                    popupsCerrados = true;
                                    System.Threading.Thread.Sleep(1000);
                                }
                            }
                            catch { /* Ignorar silenciosamente si no hay pop-up */ }
                        }

                        // Obtener Nombre
                        IWebElement elementoNombre = esperaLarga.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".product-intro__head-name")));
                        string nombre = elementoNombre.Text;

                        // Obtener Precio
                        double precioFinal = 0;
                        var selectoresDePrecio = new List<string> { "span.price-real", ".product-intro__head-price .sale-price", ".product-intro__head-price .original-price", "p.productDiscountInfo__retail", "p.productEstimatedTagNewRetail__retail", ".productPrice__main span:nth-of-type(2)" };

                        foreach (var selector in selectoresDePrecio)
                        {
                            try
                            {
                                IWebElement elementoPrecio = navegador.FindElement(By.CssSelector(selector));
                                if (elementoPrecio.Displayed)
                                {
                                    string precioCrudo = elementoPrecio.Text.Replace("$", "").Trim();
                                    if (double.TryParse(precioCrudo, NumberStyles.Any, CultureInfo.InvariantCulture, out precioFinal))
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (NoSuchElementException) { continue; }
                        }

                        // Obtener Descuento
                        string descuento = "N/A";
                        try
                        {
                            IWebElement elementoDescuento = navegador.FindElement(By.CssSelector(".productDiscountPercent"));
                            descuento = elementoDescuento.Text;
                        }
                        catch (NoSuchElementException) { }

                        // Obtener SKU
                        string sku = "";
                        try
                        {
                            IWebElement elementoSku = navegador.FindElement(By.CssSelector(".product-intro__head-sku span"));
                            sku = elementoSku.Text.Replace("SKU: ", "").Trim();
                        }
                        catch (NoSuchElementException)
                        {
                            try
                            {
                                var elementosConSku = navegador.FindElements(By.CssSelector("[data-sku]"));
                                foreach (var elementoWeb in elementosConSku)
                                {
                                    string atributoDataSku = elementoWeb.GetAttribute("data-sku");
                                    if (!string.IsNullOrEmpty(atributoDataSku)) { sku = atributoDataSku; break; }
                                }
                            }
                            catch { }

                            if (string.IsNullOrEmpty(sku))
                            {
                                var coincidenciaRegex = System.Text.RegularExpressions.Regex.Match(urlProducto, @"/p-(\d+)(?:-\d+)?\.html");
                                sku = coincidenciaRegex.Success ? coincidenciaRegex.Groups[1].Value : $"Desconocido_{Guid.NewGuid().ToString().Substring(0, 5)}";
                            }
                        }

                        // Obtener Imagen usando tu método robusto
                        string imagenUrl = ExtraerUrlImagen(navegador);
                        string rutaImagenDescargada = "N/A";

                        if (imagenUrl != "N/A")
                        {
                            await DescargarImagenAsync(imagenUrl, _carpetaSeleccionada, sku);
                            rutaImagenDescargada = Path.Combine(_carpetaSeleccionada, $"{sku}.jpg");
                        }

                        // Guardar en la estructura de memoria
                        _listaProductos.Add(new ProductoShein
                        {
                            Sku = sku,
                            Nombre = nombre,
                            Precio = precioFinal,
                            Descuento = descuento,
                            ImagenUrl = imagenUrl,
                            RutaImagenLocal = rutaImagenDescargada
                        });

                        rtbResultado.AppendText($"-> ¡Extraído con éxito! SKU: {sku} | Precio: ${precioFinal}\n\n");
                    }
                    catch (Exception excepcionProducto)
                    {
                        rtbResultado.AppendText($"[!] Error al extraer el producto {urlProducto}: {excepcionProducto.Message}\nSaltando al siguiente...\n\n");
                    }

                    contador++;
                }

                rtbResultado.AppendText($"=== EXTRACCIÓN FINALIZADA ===\nSe extrajeron {_listaProductos.Count} productos exitosamente.\n");

                // Escribir automáticamente al final todo el lote a Excel
                if (_listaProductos.Count > 0)
                {
                    GuardarLoteEnExcel();
                }
            }
            catch (Exception excepcion)
            {
                rtbResultado.AppendText($"Ocurrió un error crítico: {excepcion.Message}\n");
            }
            finally
            {
                btnScrape.Enabled = true;
                if (navegador != null)
                {
                    navegador.Quit();
                }
            }
        }

        // --- MÉTODO PARA EXTRAER IMAGEN ---
        private string ExtraerUrlImagen(IWebDriver navegador)
        {
            string urlImagen = "N/A";
            string[] selectoresImagen = new string[]
            {
                "div.normal-picture.one-picture__normal img.crop-image-container__img",
                ".product-intro__main img",
                ".crop-image-container img",
                ".gallery-image-item img",
                ".product-image img",
                "div[data-role='product-image'] img"
            };

            foreach (var selector in selectoresImagen)
            {
                try
                {
                    var nodosImagen = navegador.FindElements(By.CssSelector(selector));
                    if (nodosImagen.Count > 0)
                    {
                        var nodoImagen = nodosImagen[0];
                        string atributoSrc = nodoImagen.GetAttribute("src");
                        string atributoDataSrc = nodoImagen.GetAttribute("data-src");

                        string urlTemporal = atributoSrc;

                        if (string.IsNullOrEmpty(urlTemporal) || urlTemporal.Contains("bg-grey") || urlTemporal.Contains("placeholder"))
                        {
                            if (!string.IsNullOrEmpty(atributoDataSrc)) { urlTemporal = atributoDataSrc; }
                        }

                        if (!string.IsNullOrEmpty(urlTemporal) && urlTemporal.StartsWith("//"))
                        {
                            urlTemporal = "https:" + urlTemporal;
                        }

                        if (!string.IsNullOrEmpty(urlTemporal) && !urlTemporal.Contains("bg-grey") && !urlTemporal.Contains("placeholder"))
                        {
                            urlImagen = urlTemporal;
                            break;
                        }
                    }
                }
                catch (Exception) { continue; }
            }

            return string.IsNullOrEmpty(urlImagen) ? "N/A" : urlImagen;
        }

        private async Task DescargarImagenAsync(string urlImagen, string rutaCarpeta, string sku)
        {
            if (string.IsNullOrEmpty(urlImagen) || string.IsNullOrEmpty(rutaCarpeta) || string.IsNullOrEmpty(sku)) return;

            try
            {
                using (HttpClient clienteHttp = new HttpClient())
                {
                    byte[] bytesDeImagen = await clienteHttp.GetByteArrayAsync(urlImagen);
                    string nombreArchivo = $"{sku}.jpg";
                    string rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);
                    await File.WriteAllBytesAsync(rutaArchivo, bytesDeImagen);
                }
            }
            catch (Exception excepcion)
            {
                rtbResultado.AppendText($"[!] Error al descargar imagen del SKU {sku}: {excepcion.Message}\n");
            }
        }

        // --- LÓGICA DE EXPORTACIÓN MASIVA A EXCEL ---
        private void GuardarLoteEnExcel()
        {
            string nombreArchivoExcel = "ProductosShein.xlsx";
            string rutaArchivoExcel = Path.Combine(_carpetaSeleccionada, nombreArchivoExcel);

            double valorEnvio = 0;
            double.TryParse(EnvioTextBox.Text, out valorEnvio);
            string nombreCliente = ClienteTextBox.Text;

            try
            {
                FileInfo nuevoArchivo = new FileInfo(rutaArchivoExcel);
                using (ExcelPackage paqueteExcel = new ExcelPackage(nuevoArchivo))
                {
                    ExcelWorksheet hojaDeCalculo;

                    if (nuevoArchivo.Exists && paqueteExcel.Workbook.Worksheets.Any())
                    {
                        hojaDeCalculo = paqueteExcel.Workbook.Worksheets.First();
                    }
                    else
                    {
                        hojaDeCalculo = paqueteExcel.Workbook.Worksheets.Add("Datos Productos");
                        // Nueva estructura sin la columna "Talla" (9 columnas en total)
                        hojaDeCalculo.Cells[1, 1].Value = "SKU";
                        hojaDeCalculo.Cells[1, 2].Value = "Nombre Articulo";
                        hojaDeCalculo.Cells[1, 3].Value = "Precio";
                        hojaDeCalculo.Cells[1, 4].Value = "Descuento";
                        hojaDeCalculo.Cells[1, 5].Value = "Envio";
                        hojaDeCalculo.Cells[1, 6].Value = "Cliente";
                        hojaDeCalculo.Cells[1, 7].Value = "Precio Total";
                        hojaDeCalculo.Cells[1, 8].Value = "URL Imagen";
                        hojaDeCalculo.Cells[1, 9].Value = "Ruta Imagen Local";

                        hojaDeCalculo.Cells[1, 1, 1, 9].AutoFitColumns();
                    }

                    int totalFilas = hojaDeCalculo.Dimension?.Rows ?? 0;
                    int nuevaFila = totalFilas + 1;

                    // Agregar todas las filas del lote actual
                    foreach (var producto in _listaProductos)
                    {
                        double precioTotalCalculado = Math.Round((producto.Precio * 1.07) + valorEnvio, 2);

                        hojaDeCalculo.Cells[nuevaFila, 1].Value = producto.Sku;
                        hojaDeCalculo.Cells[nuevaFila, 2].Value = producto.Nombre;
                        hojaDeCalculo.Cells[nuevaFila, 3].Value = producto.Precio;
                        hojaDeCalculo.Cells[nuevaFila, 4].Value = producto.Descuento;
                        hojaDeCalculo.Cells[nuevaFila, 5].Value = valorEnvio;
                        hojaDeCalculo.Cells[nuevaFila, 6].Value = nombreCliente;
                        hojaDeCalculo.Cells[nuevaFila, 7].Value = precioTotalCalculado;
                        hojaDeCalculo.Cells[nuevaFila, 8].Value = producto.ImagenUrl;
                        hojaDeCalculo.Cells[nuevaFila, 9].Value = producto.RutaImagenLocal;

                        nuevaFila++;
                    }

                    hojaDeCalculo.Cells[1, 1, nuevaFila, 9].AutoFitColumns();
                    paqueteExcel.Save();
                }

                rtbResultado.AppendText($"\n-> ¡Excel actualizado exitosamente en: {rutaArchivoExcel}!\n");
                MessageBox.Show($"Proceso finalizado. {_listaProductos.Count} productos guardados en Excel.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception excepcion)
            {
                MessageBox.Show($"Error al guardar en Excel: {excepcion.Message}\nAsegúrate de que el archivo no esté abierto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Mantenemos el botón manual por si necesitas exportar de nuevo sin scrapear
        private void btnGuardarExcel_Click(object sender, EventArgs e)
        {
            if (_listaProductos == null || _listaProductos.Count == 0)
            {
                MessageBox.Show("No hay datos nuevos en memoria para guardar. Realiza un scraping primero.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GuardarLoteEnExcel();
        }

        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}