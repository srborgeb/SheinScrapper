using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml; // Para EPPlus
using System.Linq; // Para LINQ en Excel
// Nuevas referencias para Selenium
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome; // Para Chrome
using OpenQA.Selenium.Support.UI; // Para WebDriverWait
using WebDriverManager; // Añadido para la gestión automática del driver
using WebDriverManager.DriverConfigs.Impl; // Añadido para la configuración de Chrome
using AngleSharp.Text; // Para el formato numérico
using System.Globalization; // Para la cultura y formato numérico
using System.Threading;

namespace SheinScraperApp
{
    public partial class formScrap : Form
    {
        private string _carpetaSeleccionada = "";
        private string _productoNombre = "";
        private string _productoPrecio = "";
        private string _productoDescuento = "";
        private string _productoSku = "";
        private string _productoImagenUrl = "";

        // Ruta del perfil temporal para Chrome
        private string _chromeUserProfilePath;

        public formScrap()
        {
            InitializeComponent();
            _chromeUserProfilePath = Path.Combine(Path.GetTempPath(), "SheinScraperChromeProfile");
            Directory.CreateDirectory(_chromeUserProfilePath); // Asegura que la carpeta de chrome exista.

            txtUrlProducto.Enter += TxtUrlProducto_Enter; // Modificación para seleccionar todo el texto al entrar
            txtUrlProducto.Text = ""; // Inicializar el campo de texto vacío
        }

        string _SeparadorDecimal = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        private void TxtUrlProducto_Enter(object sender, EventArgs e)
        {
            // Selecciona todo el texto en la caja de texto cuando obtiene el foco - No funciona
            txtUrlProducto.SelectAll();
        }

        private void Valor_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Solo recibe numeros y el separador decimal como entrada
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != _SeparadorDecimal[0])
            {
                e.Handled = true;
            }

            //Si ya existe un un separador separador igonora la siguiente pulsacion de este
            if (e.KeyChar == _SeparadorDecimal[0] && (sender as TextBox).Text.IndexOf(_SeparadorDecimal) > -1)
            {
                e.Handled = true;
            }
        }

        private void Nombre_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo letras, numeros y espacios
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) && !char.IsWhiteSpace(e.KeyChar))
            {
                e.Handled = true;
            }

            //if (string.IsNullOrEmpty((sender as TextBox).Text))
            //{
            //    MessageBox.Show("El campo de nombre no puede estar vacío.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
        }


        private void btnSeleccionarDirectorio_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    _carpetaSeleccionada = fbd.SelectedPath;
                    lblDirectorio.Text = $"Carpeta: {_carpetaSeleccionada}";
                }
            }
        }

        private async void btnScrape_Click(object sender, EventArgs e)
        {
            string productUrl = txtUrlProducto.Text.Trim();

            if (string.IsNullOrEmpty(productUrl) || !Uri.IsWellFormedUriString(productUrl, UriKind.Absolute))
            {
                MessageBox.Show("Por favor, introduce una URL de producto válida.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar que se seleccionó una carpeta válida
            if (string.IsNullOrEmpty(_carpetaSeleccionada) || !Directory.Exists(_carpetaSeleccionada))
            {
                MessageBox.Show("Por favor, selecciona una carpeta válida para guardar las imágenes y el Excel antes de scrapear.", "Carpeta no seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            rtbResultado.Clear();
            rtbResultado.AppendText("Iniciando scraping con Selenium (lanzando Chrome automatizado)... Por favor, espera.\n");
            btnScrape.Enabled = false;

            IWebDriver driver = null;
            try
            {
                // WebDriverManager descargará el chromedriver.exe a una ubicación conocida por Selenium.
                new DriverManager().SetUpDriver(new ChromeConfig());

                var service = ChromeDriverService.CreateDefaultService();
                service.SuppressInitialDiagnosticInformation = true;

                ChromeOptions options = new ChromeOptions();

                options.AddArgument($"--user-data-dir={_chromeUserProfilePath}");
                options.AddArgument("--profile-directory=Default"); // selenium usará el perfil por defecto 

                //habilitar modo stealth para evitar detección de automatización
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddExcludedArgument("enable-automation");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--start-maximized"); // Inicia maximizado
                options.AddArgument("--no-sandbox"); // Necesario para algunos entornos de ejecución
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu"); // Deshabilita la aceleración por hardware.

                //solicitur idioma español
                options.AddArgument("--lang=es");

                //abrir chrome
                driver = new ChromeDriver(service, options);
                driver.Manage().Window.Maximize(); // Asegura que la ventana esté maximizada después de abrir
                System.Threading.Thread.Sleep(2000); // Pequeña pausa para que el navegador abra completamente

                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(90);

                rtbResultado.AppendText($"Navegando a: {productUrl}\n");
                try
                {
                    driver.Navigate().GoToUrl(productUrl);
                    rtbResultado.AppendText("Navegación solicitada.\n");

                    // Espera a que la URL actual contenga "us.shein.com" para confirmar la navegación
                    WebDriverWait urlWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                    urlWait.Until(d => d.Url.Contains("us.shein.com", StringComparison.OrdinalIgnoreCase));
                    rtbResultado.AppendText($"Actualmente en URL: {driver.Url}\n"); // Muestra la URL real
                }
                catch (Exception navEx)
                {
                    rtbResultado.AppendText($"Error durante la navegación inicial a {productUrl}: {navEx.Message}\n");
                    throw new Exception("Error de navegación inicial. La URL podría estar bloqueada o no accesible.");
                }

                //Menaje de pop-ups de cookies
                try
                {
                    WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                    IWebElement acceptCookiesButton = shortWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".cookie-popup-accept-button, #onetrust-accept-btn-handler, [aria-label='Aceptar cookies'], .shein-button-black, .s-btn-primary")));

                    if (acceptCookiesButton != null && acceptCookiesButton.Displayed && acceptCookiesButton.Enabled)
                    {
                        acceptCookiesButton.Click();
                        rtbResultado.AppendText("Se hizo clic en el botón de aceptar cookies/pop-up.\n");
                        System.Threading.Thread.Sleep(2000); // Pequeña pausa después de interactuar
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    rtbResultado.AppendText("No se detectó o no se pudo cerrar un pop-up inicial de cookies.\n");
                }
                catch (ElementClickInterceptedException)
                {
                    rtbResultado.AppendText("No se pudo hacer clic en el pop-up, algo lo interceptó. Intentando de nuevo o continuando.\n");
                    System.Threading.Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    rtbResultado.AppendText($"Error al intentar cerrar pop-up: {ex.Message}\n");
                }

                //Espera extendida para que el contenido del producto cargue completamente
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(300)); // 5 minutos de espera para CAPTCHA/carga completa

                rtbResultado.AppendText("Esperando que el contenido del producto cargue (buscando el nombre del producto)...\n");
                IWebElement nameElement = null;
                try
                {
                    nameElement = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".product-intro__head-name")));
                    _productoNombre = nameElement.Text;
                    rtbResultado.AppendText("Contenido principal del producto cargado.\n");
                }
                catch (WebDriverTimeoutException)
                {
                    rtbResultado.AppendText("¡Advertencia! El nombre del producto no se hizo visible a tiempo.\n");
                    rtbResultado.AppendText("Esto podría deberse a un CAPTCHA, un bloqueo o una carga extremadamente lenta.\n");
                    rtbResultado.AppendText("Por favor, revisa la ventana del navegador que se abrió y resuelve cualquier CAPTCHA o interacción manual necesaria.\n");

                    //Mensaje de espera para resolver CAPTCHA
                    MessageBox.Show("Se ha detectado un posible CAPTCHA o bloqueo.\n\nPor favor, resuelve la verificación en la ventana del navegador que se abrió (puedes iniciar sesión si es necesario).\n\nLa aplicación continuará automáticamente una vez que el contenido sea accesible. Si ya lo has resuelto, haz clic en Aceptar aquí.", "Resolver CAPTCHA Manualmente", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Después de hacer clic en Aceptar en captcha, intentamos de nuevo obtener los datos de la pagina
                    try
                    {
                        nameElement = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".product-intro__head-name")));
                        _productoNombre = nameElement.Text;
                        rtbResultado.AppendText("Parece que el CAPTCHA fue resuelto o la página cargó. Continuando.\n");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"El contenido del producto no se pudo cargar o el CAPTCHA no fue resuelto después de la intervención: {ex.Message}");
                    }
                }
                catch (NoSuchElementException)
                {
                    throw new Exception("El elemento del nombre del producto no fue encontrado después de la carga de la página. El selector CSS podría ser incorrecto.");
                }

                // Manejo de posibles elementos nulos
                IWebElement priceElement = null;
                try
                {
                    //si hay descuento, será el precio con descuento
                    priceElement = driver.FindElement(By.CssSelector("p.productDiscountInfo__retail")); // Selector revisado para el HTML proporcionado
                    string rawPrice = priceElement.Text.Replace("$", "").Trim(); // Eliminar el signo de dólar

                    if (decimal.TryParse(rawPrice, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal priceValue))
                    {
                        _productoPrecio = priceValue.ToString(CultureInfo.InvariantCulture); // Formato numérico con punto decimal
                    }
                    else
                    {
                        _productoPrecio = "N/A (Error de formato de precio)";
                    }
                }
                catch (NoSuchElementException)
                {
                    try
                    {
                        //Precio principal sin descuento
                        priceElement = driver.FindElement(By.CssSelector(".productPrice__main span:nth-of-type(2)"));
                        string rawPrice = priceElement.Text.Replace("$", "").Trim(); // Eliminar el signo de dólar

                        //Convertir a numérico
                        if (decimal.TryParse(rawPrice, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal priceValue))
                        {
                            _productoPrecio = priceValue.ToString(CultureInfo.InvariantCulture); // Formato numérico con punto decimal
                        }
                        else
                        {
                            _productoPrecio = "N/A (Error de formato de precio)";
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        _productoPrecio = "N/A";
                    }
                }

                IWebElement discountElement = null;
                try
                {
                    // Selector para el porcentaje de descuento
                    discountElement = driver.FindElement(By.CssSelector(".productDiscountPercent"));
                    _productoDescuento = discountElement.Text;
                }
                catch (NoSuchElementException) { _productoDescuento = "N/A"; }

                IWebElement skuElement = null;
                try
                {
                    skuElement = driver.FindElement(By.CssSelector(".product-intro__head-sku span"));
                    _productoSku = skuElement.Text.Replace("SKU: ", "").Trim();
                }
                catch (NoSuchElementException)
                {
                    // Intentar obtener de data-sku si existe en algún elemento
                    try
                    {
                        var elementsWithSku = driver.FindElements(By.CssSelector("[data-sku]"));
                        foreach (var el in elementsWithSku)
                        {
                            string dataSku = el.GetAttribute("data-sku");
                            if (!string.IsNullOrEmpty(dataSku))
                            {
                                _productoSku = dataSku;
                                break;
                            }
                        }
                    }
                    catch { /* Ignorar, el SKU se intentará de la URL */ }

                    if (_productoSku == "")
                    {
                        //Extraer SKU de la URL si no se encuentra en el HTML
                        var match = System.Text.RegularExpressions.Regex.Match(productUrl, @"/p-(\d+)(?:-\d+)?\.html");
                        if (match.Success)
                        {
                            _productoSku = match.Groups[1].Value;
                        }
                        else
                        {
                            _productoSku = "N/A (No encontrado)";
                        }
                    }
                }

                IWebElement imageElement = null;
                try
                {
                    // Selector para la imagen principal
                    imageElement = driver.FindElement(By.CssSelector("div.normal-picture.one-picture__normal img.crop-image-container__img"));
                    string imageUrlAttribute = imageElement.GetAttribute("src");

                    // Shein a veces usa src="//..." o data-src="//..." para lazy loading.
                    if (imageUrlAttribute.StartsWith("//"))
                    {
                        _productoImagenUrl = "https:" + imageUrlAttribute;
                    }
                    else
                    {
                        _productoImagenUrl = imageUrlAttribute;
                    }

                    // Si la imagen es un placeholder de lazyload, intentar data-src
                    if (_productoImagenUrl.Contains("bg-grey") || _productoImagenUrl.Contains("placeholder"))
                    {
                        string dataSrc = imageElement.GetAttribute("data-src");
                        if (!string.IsNullOrEmpty(dataSrc) && dataSrc.StartsWith("//"))
                        {
                            _productoImagenUrl = "https:" + dataSrc;
                        }
                        else if (!string.IsNullOrEmpty(dataSrc))
                        {
                            _productoImagenUrl = dataSrc;
                        }
                    }
                }
                catch (NoSuchElementException) { _productoImagenUrl = "N/A"; }


                rtbResultado.Clear();
                rtbResultado.AppendText("Datos del Producto:\n");
                rtbResultado.AppendText($"Nombre: {_productoNombre}\n");
                rtbResultado.AppendText($"Precio: {_productoPrecio}\n");
                rtbResultado.AppendText($"Descuento: {_productoDescuento}\n");
                rtbResultado.AppendText($"SKU: {_productoSku}\n");
                rtbResultado.AppendText($"URL de Imagen: {_productoImagenUrl}\n");

                if (_productoImagenUrl != "N/A" && !string.IsNullOrEmpty(_carpetaSeleccionada))
                {
                    rtbResultado.AppendText("Descargando imagen...\n");
                    await DownloadImageAsync(_productoImagenUrl, _carpetaSeleccionada, _productoSku);
                    rtbResultado.AppendText("Imagen descargada.\n");
                }
                else if (string.IsNullOrEmpty(_carpetaSeleccionada))
                {
                    rtbResultado.AppendText("No se ha seleccionado una carpeta para descargar la imagen.\n");
                }
                else
                {
                    rtbResultado.AppendText("No se pudo obtener la URL de la imagen.\n");
                }
            }
            catch (Exception ex)
            {
                rtbResultado.AppendText($"Ocurrió un error: {ex.Message}\n");
                rtbResultado.AppendText("Por favor, verifica la URL y asegúrate de que el driver del navegador esté instalado correctamente.\n");
            }
            finally
            {
                btnScrape.Enabled = true;
                if (driver != null)
                {
                    driver.Quit(); // Cierra el navegador
                }
                txtUrlProducto.Text = ""; // Inicializar el campo de texto vacío

            }
        }

        private async Task DownloadImageAsync(string imageUrl, string folderPath, string sku)
        {
            if (string.IsNullOrEmpty(imageUrl) || string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(sku))
            {
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
                    string fileName = $"{sku}.jpg";
                    string filePath = Path.Combine(folderPath, fileName);
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    rtbResultado.AppendText($"Imagen guardada en: {filePath}\n");
                }
            }
            catch (Exception ex)
            {
                rtbResultado.AppendText($"Error al descargar o guardar la imagen: {ex.Message}\n");
            }
        }

        private void btnGuardarExcel_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_productoSku) || _productoSku == "N/A")
            {
                MessageBox.Show("No hay datos de producto para guardar. Por favor, realiza un scraping primero.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_carpetaSeleccionada))
            {
                MessageBox.Show("Por favor, selecciona una carpeta para guardar el archivo Excel.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            if (string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(textBox2.Text))
            {
                MessageBox.Show("Por favor, completa ambos campos antes de continuar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }
            ;

            string excelFileName = "ProductosShein.xlsx";
            string excelFilePath = Path.Combine(_carpetaSeleccionada, excelFileName);

            try
            {
                FileInfo newFile = new FileInfo(excelFilePath);

                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    ExcelWorksheet worksheet;

                    if (newFile.Exists && package.Workbook.Worksheets.Any())
                    {
                        worksheet = package.Workbook.Worksheets.First();
                    }
                    else
                    {
                        worksheet = package.Workbook.Worksheets.Add("Datos Productos");
                        worksheet.Cells[1, 1].Value = "SKU";
                        worksheet.Cells[1, 2].Value = "Nombre Articulo";
                        worksheet.Cells[1, 3].Value = "Precio";
                        worksheet.Cells[1, 4].Value = "Descuento";
                        worksheet.Cells[1, 5].Value = "Envio";
                        worksheet.Cells[1, 6].Value = "Cliente";
                        worksheet.Cells[1, 7].Value = "Precio Total"; 
                        worksheet.Cells[1, 8].Value = "URL Imagen";
                        worksheet.Cells[1, 9].Value = "Ruta Imagen Local";

                        worksheet.Cells[1, 1, 1, 9].AutoFitColumns();
                    }

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    int newRow = rowCount + 1;

                    double _Envio = 0;
                    double.TryParse(textBox1.Text, out _Envio);
                    string _Cliente = textBox2.Text;
                    double.TryParse(_productoPrecio, NumberStyles.Any, CultureInfo.InvariantCulture, out double _productoPrecioDouble);
                    double _PrecioTotal = (_productoPrecioDouble * 1.07) + _Envio;

                    worksheet.Cells[newRow, 1].Value = _productoSku;
                    worksheet.Cells[newRow, 2].Value = _productoNombre;
                    worksheet.Cells[newRow, 3].Value = _productoPrecio;
                    worksheet.Cells[newRow, 4].Value = _productoDescuento;
                    worksheet.Cells[newRow, 5].Value = _Envio;
                    worksheet.Cells[newRow, 6].Value = _Cliente;
                    worksheet.Cells[newRow, 7].Value = _PrecioTotal;
                    worksheet.Cells[newRow, 8].Value = _productoImagenUrl;
                    worksheet.Cells[newRow, 9].Value = Path.Combine(_carpetaSeleccionada, $"{_productoSku}.jpg");

                    worksheet.Cells[newRow, 1, newRow, 9].AutoFitColumns();

                    package.Save();
                }
                MessageBox.Show($"Datos guardados exitosamente en: {excelFilePath}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar en Excel: {ex.Message}\n" +
                                "Asegúrate de que el archivo no esté abierto y tengas permisos de escritura.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
