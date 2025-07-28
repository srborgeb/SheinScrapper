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
using WebDriverManager; // A�adido para la gesti�n autom�tica del driver
using WebDriverManager.DriverConfigs.Impl; // A�adido para la configuraci�n de Chrome
using System.Globalization; // Para el formato num�rico

namespace SheinScraperApp
{
    public partial class formScrap : Form
    {
        // Variables renombradas a espa�ol
        private string _carpetaSeleccionada = "";
        private string _productoNombre = "";
        private string _productoPrecio = "";
        private string _productoDescuento = "";
        private string _productoSku = "";
        private string _productoImagenUrl = "";

        // Ruta del perfil temporal para Chrome, gestionado por la aplicaci�n
        private string _chromeUserProfilePath;

        public formScrap()
        {
            InitializeComponent();
            // �IMPORTANTE! Con EPPlus 4.x, NO necesitas configurar ExcelPackage.LicenseContext aqu�.
            // Aseg�rate de que EPPlus versi�n 4.5.3.3 est� instalada y elimina cualquier l�nea que intente configurar ExcelPackage.LicenseContext.

            // Definir la ruta del perfil de usuario para Chrome
            // Esto asegura que las cookies y sesiones persistan entre ejecuciones
            // y que sea un perfil que la aplicaci�n puede controlar sin conflictos con tu Chrome principal.
            _chromeUserProfilePath = Path.Combine(Path.GetTempPath(), "SheinScraperChromeProfile");
            Directory.CreateDirectory(_chromeUserProfilePath); // Asegura que la carpeta exista.

            // Asignar el evento para seleccionar todo el texto al hacer clic en la caja de URL
            txtUrlProducto.Enter += TxtUrlProducto_Enter; // Modificaci�n para seleccionar todo el texto al entrar
        }

        private void TxtUrlProducto_Enter(object sender, EventArgs e)
        {
            // Selecciona todo el texto en la caja de texto cuando obtiene el foco
            txtUrlProducto.SelectAll();
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
                MessageBox.Show("Por favor, introduce una URL de producto v�lida.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // *** NUEVA VALIDACI�N: Verificar que se haya seleccionado una carpeta v�lida ***
            if (string.IsNullOrEmpty(_carpetaSeleccionada) || !Directory.Exists(_carpetaSeleccionada))
            {
                MessageBox.Show("Por favor, selecciona una carpeta v�lida para guardar las im�genes y el Excel antes de scrapear.", "Carpeta no seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbResultado.Clear();
            rtbResultado.AppendText("Iniciando scraping con Selenium (lanzando Chrome automatizado)... Por favor, espera.\n");
            btnScrape.Enabled = false;

            IWebDriver driver = null; // Declarar aqu� para que sea accesible en finally
            try
            {
                // *** Paso 1: Asegurar que el ChromeDriver est� descargado y listo ***
                // WebDriverManager descargar� el chromedriver.exe a una ubicaci�n conocida por Selenium.
                new DriverManager().SetUpDriver(new ChromeConfig());

                var service = ChromeDriverService.CreateDefaultService();
                service.SuppressInitialDiagnosticInformation = true; // Reduce la salida de consola en la consola de depuraci�n

                ChromeOptions options = new ChromeOptions();

                // *** Configuraci�n para usar un perfil de usuario gestionado por la aplicaci�n ***
                // Esto permite que las cookies, cach� y sesiones se mantengan entre ejecuciones
                // y que no haya conflicto con tu Chrome personal si est� abierto.
                options.AddArgument($"--user-data-dir={_chromeUserProfilePath}");
                options.AddArgument("--profile-directory=Default"); // Selenium usar� o crear� un perfil 'Default' dentro de user-data-dir

                // --- Opciones de "Stealth" para parecer m�s humano ---
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddExcludedArgument("enable-automation");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--start-maximized"); // Inicia maximizado
                options.AddArgument("--no-sandbox"); // Necesario para algunos entornos de ejecuci�n
                options.AddArgument("--disable-dev-shm-usage"); // Para Linux/Docker, �til en algunos Windows
                options.AddArgument("--disable-gpu"); // Deshabilita la aceleraci�n por hardware, �til para estabilidad
                // options.AddArgument("--headless"); // Mantenlo comentado para ver el navegador y resolver CAPTCHAs

                // --- Configuraci�n de idioma ---
                options.AddArgument("--lang=es");

                // *** Lanzar el navegador Chrome ***
                driver = new ChromeDriver(service, options);
                driver.Manage().Window.Maximize(); // Asegura que la ventana est� maximizada despu�s de abrir
                System.Threading.Thread.Sleep(2000); // Peque�a pausa para que el navegador se asiente

                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(90);

                rtbResultado.AppendText($"Navegando a: {productUrl}\n");
                try
                {
                    driver.Navigate().GoToUrl(productUrl);
                    rtbResultado.AppendText("Navegaci�n solicitada.\n");

                    // Espera a que la URL actual contenga "shein.com" para confirmar la navegaci�n
                    WebDriverWait urlWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                    urlWait.Until(d => d.Url.Contains("shein.com", StringComparison.OrdinalIgnoreCase));
                    rtbResultado.AppendText($"Actualmente en URL: {driver.Url}\n"); // Muestra la URL real
                }
                catch (Exception navEx)
                {
                    rtbResultado.AppendText($"Error durante la navegaci�n inicial a {productUrl}: {navEx.Message}\n");
                    throw new Exception("Error de navegaci�n inicial. La URL podr�a estar bloqueada o no accesible.");
                }

                // --- Manejo de Pop-ups (ej. Cookies o banners iniciales) ---
                try
                {
                    WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                    IWebElement acceptCookiesButton = shortWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".cookie-popup-accept-button, #onetrust-accept-btn-handler, [aria-label='Aceptar cookies'], .shein-button-black, .s-btn-primary")));

                    if (acceptCookiesButton != null && acceptCookiesButton.Displayed && acceptCookiesButton.Enabled)
                    {
                        acceptCookiesButton.Click();
                        rtbResultado.AppendText("Se hizo clic en el bot�n de aceptar cookies/pop-up.\n");
                        System.Threading.Thread.Sleep(2000); // Peque�a pausa despu�s de interactuar
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    rtbResultado.AppendText("No se detect� o no se pudo cerrar un pop-up inicial de cookies.\n");
                }
                catch (ElementClickInterceptedException)
                {
                    rtbResultado.AppendText("No se pudo hacer clic en el pop-up, algo lo intercept�. Intentando de nuevo o continuando.\n");
                    System.Threading.Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    rtbResultado.AppendText($"Error al intentar cerrar pop-up: {ex.Message}\n");
                }

                // --- Espera robusta para el nombre del producto ---
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
                    rtbResultado.AppendText("�Advertencia! El nombre del producto no se hizo visible a tiempo.\n");
                    rtbResultado.AppendText("Esto podr�a deberse a un CAPTCHA, un bloqueo o una carga extremadamente lenta.\n");
                    rtbResultado.AppendText("Por favor, revisa la ventana del navegador que se abri� y resuelve cualquier CAPTCHA o interacci�n manual necesaria.\n");

                    // *** Mensaje para el usuario: Resolver CAPTCHA / Iniciar sesi�n ***
                    MessageBox.Show("Se ha detectado un posible CAPTCHA o bloqueo.\n\nPor favor, resuelve la verificaci�n en la ventana del navegador que se abri� (puedes iniciar sesi�n si es necesario).\n\nLa aplicaci�n continuar� autom�ticamente una vez que el contenido sea accesible. Si ya lo has resuelto, haz clic en Aceptar aqu�.", "Resolver CAPTCHA Manualmente", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Despu�s de que el usuario haga clic en Aceptar, intentamos de nuevo obtener el elemento
                    try
                    {
                        nameElement = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".product-intro__head-name")));
                        _productoNombre = nameElement.Text;
                        rtbResultado.AppendText("Parece que el CAPTCHA fue resuelto o la p�gina carg�. Continuando.\n");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"El contenido del producto no se pudo cargar o el CAPTCHA no fue resuelto despu�s de la intervenci�n: {ex.Message}");
                    }
                }
                catch (NoSuchElementException)
                {
                    throw new Exception("El elemento del nombre del producto no fue encontrado despu�s de la carga de la p�gina. El selector CSS podr�a ser incorrecto.");
                }

                // --- Extracci�n de Datos ---
                // Selectores actualizados seg�n el HTML proporcionado
                // Manejo de posibles elementos nulos (si un elemento no se encuentra)
                IWebElement priceElement = null;
                try
                {
                    // Selector para el precio de venta (si hay descuento, ser� el precio con descuento)
                    priceElement = driver.FindElement(By.CssSelector("p.productDiscountInfo__retail")); // Selector revisado para el HTML proporcionado
                    string rawPrice = priceElement.Text.Replace("$", "").Trim(); // Eliminar el signo de d�lar

                    // *** NUEVA L�GICA: Convertir a num�rico y formatear sin s�mbolo de moneda ***
                    if (decimal.TryParse(rawPrice, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal priceValue))
                    {
                        _productoPrecio = priceValue.ToString(CultureInfo.InvariantCulture); // Formato num�rico con punto decimal
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
                        // Fallback para el precio principal si no hay descuento
                        priceElement = driver.FindElement(By.CssSelector(".productPrice__main span:nth-of-type(2)"));
                        string rawPrice = priceElement.Text.Replace("$", "").Trim(); // Eliminar el signo de d�lar

                        // *** NUEVA L�GICA: Convertir a num�rico y formatear sin s�mbolo de moneda ***
                        if (decimal.TryParse(rawPrice, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal priceValue))
                        {
                            _productoPrecio = priceValue.ToString(CultureInfo.InvariantCulture); // Formato num�rico con punto decimal
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
                    // Selector para el porcentaje de descuento (revisado para el HTML proporcionado)
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
                    // Intentar obtener de data-sku si existe en alg�n elemento
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
                    catch { /* Ignorar, el SKU se intentar� de la URL */ }

                    if (_productoSku == "")
                    {
                        // Fallback: extraer SKU de la URL si no se encuentra en el HTML
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
                    // Selector para la imagen principal (revisado para el HTML proporcionado)
                    imageElement = driver.FindElement(By.CssSelector("div.normal-picture.one-picture__normal img.crop-image-container__img"));
                    string imageUrlAttribute = imageElement.GetAttribute("src");

                    // Shein a veces usa src="//..." o data-src="//..." para lazy loading. Normalizar a https:
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
                rtbResultado.AppendText($"Ocurri� un error: {ex.Message}\n");
                rtbResultado.AppendText("Por favor, verifica la URL y aseg�rate de que el driver del navegador est� instalado correctamente.\n");
            }
            finally
            {
                btnScrape.Enabled = true;
                if (driver != null)
                {
                    driver.Quit(); // Cierra el navegador de Selenium
                }
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
                        worksheet.Cells[1, 2].Value = "Nombre";
                        worksheet.Cells[1, 3].Value = "Precio";
                        worksheet.Cells[1, 4].Value = "Descuento";
                        worksheet.Cells[1, 5].Value = "URL Imagen";
                        worksheet.Cells[1, 6].Value = "Ruta Imagen Local";

                        worksheet.Cells[1, 1, 1, 6].AutoFitColumns();
                    }

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    int newRow = rowCount + 1;

                    worksheet.Cells[newRow, 1].Value = _productoSku;
                    worksheet.Cells[newRow, 2].Value = _productoNombre;
                    worksheet.Cells[newRow, 3].Value = _productoPrecio;
                    worksheet.Cells[newRow, 4].Value = _productoDescuento;
                    worksheet.Cells[newRow, 5].Value = _productoImagenUrl;
                    worksheet.Cells[newRow, 6].Value = Path.Combine(_carpetaSeleccionada, $"{_productoSku}.jpg");

                    worksheet.Cells[newRow, 1, newRow, 6].AutoFitColumns();

                    package.Save();
                }
                MessageBox.Show($"Datos guardados exitosamente en: {excelFilePath}", "�xito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar en Excel: {ex.Message}\n" +
                                "Aseg�rate de que el archivo no est� abierto y tengas permisos de escritura.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
