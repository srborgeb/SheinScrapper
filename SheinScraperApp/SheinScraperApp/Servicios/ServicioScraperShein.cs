using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SheinScraperApp.Modelos;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace SheinScraperApp.Servicios
{
    public class ServicioScraperShein : IServicioScraperShein
    {
        private readonly IServicioAlmacenamientoImagenes _almacenamientoImagenes;
        private readonly IServicioCalculo _servicioCalculo;

        public bool ModoSinCabeza { get; set; } = true;

        public ServicioScraperShein(
            IServicioAlmacenamientoImagenes almacenamientoImagenes,
            IServicioCalculo servicioCalculo)
        {
            _almacenamientoImagenes = almacenamientoImagenes;
            _servicioCalculo = servicioCalculo;
        }

        public async Task<List<ProductoItem>> ExtraerLoteAsync(
            List<string> urls,
            string carpetaImagenes,
            decimal comisionPorDefecto,
            string clientePorDefecto,
            string tallaPorDefecto = "",
            string colorPorDefecto = "",
            int cantidadPorDefecto = 1,
            IProgress<ReporteProgresoScraping>? progreso = null,
            CancellationToken cancelacion = default)
        {
            var productosExtraidos = new List<ProductoItem>();
            if (urls == null || urls.Count == 0) return productosExtraidos;

            if (cantidadPorDefecto < 1) cantidadPorDefecto = 1;

            IWebDriver? navegador = null;
            try
            {
                progreso?.Report(new ReporteProgresoScraping
                {
                    IndiceActual = 0,
                    TotalRegistros = urls.Count,
                    Mensaje = "Iniciando motor de navegación web..."
                });

                navegador = GestorNavegadorChrome.CrearNavegador(ModoSinCabeza);

                for (int i = 0; i < urls.Count; i++)
                {
                    cancelacion.ThrowIfCancellationRequested();

                    // Capa 1: Retardo aleatorio humano entre productos (2.5 a 4.5s)
                    if (i > 0)
                    {
                        int pausaMs = Random.Shared.Next(2500, 4500);
                        await Task.Delay(pausaMs, cancelacion);
                    }

                    string enlaceActual = urls[i].Trim();
                    int indiceActual = i + 1;
                    string sku = "";

                    progreso?.Report(new ReporteProgresoScraping
                    {
                        IndiceActual = indiceActual,
                        TotalRegistros = urls.Count,
                        Url = enlaceActual,
                        Mensaje = $"Procesando producto {indiceActual} de {urls.Count}..."
                    });

                    try
                    {
                        navegador.Navigate().GoToUrl(enlaceActual);

                        cancelacion.ThrowIfCancellationRequested();

                        // 1. Extraer SKU
                        sku = ExtraerSku(navegador, enlaceActual);

                        // 2. Extraer Nombre del producto (con espera reactiva de hasta 15s)
                        string nombre = ExtraerNombre(navegador);

                        // Si el nombre no pudo ser cargado, comprobar si se debe a un Captcha activo real
                        if (string.IsNullOrWhiteSpace(nombre) || nombre.Equals("Producto Shein", StringComparison.OrdinalIgnoreCase))
                        {
                            if (DetectarCaptchaReal(navegador))
                            {
                                progreso?.Report(new ReporteProgresoScraping
                                {
                                    IndiceActual = indiceActual,
                                    TotalRegistros = urls.Count,
                                    Url = enlaceActual,
                                    Mensaje = "⚠ Captcha detectado. Intentando resolución automática (Capa 2)...",
                                    EsExitoso = false
                                });

                                bool resuelto = IntentarResolverCaptchaAutomatico(navegador);
                                if (resuelto)
                                {
                                    nombre = ExtraerNombre(navegador);
                                }

                                if (string.IsNullOrWhiteSpace(nombre) || nombre.Equals("Producto Shein", StringComparison.OrdinalIgnoreCase))
                                {
                                    var urlsPendientes = urls.Skip(i).ToList();
                                    throw new ExcepcionCaptchaShein(
                                        "Shein ha solicitado resolver un Captcha de seguridad y los métodos automáticos no pudieron superarlo.",
                                        enlaceActual,
                                        urlsPendientes);
                                }
                            }
                        }

                        // 3. Extraer Precio original completo (sin rebajas)
                        decimal precioOriginal = ExtraerPrecioOriginal(navegador);

                        // Validar si el enlace cargó pero el producto está agotado, descontinuado o 404
                        bool esProductoInvalido = precioOriginal <= 0m ||
                                                  string.IsNullOrWhiteSpace(nombre) ||
                                                  nombre.Equals("Producto Shein", StringComparison.OrdinalIgnoreCase) ||
                                                  navegador.Title.Contains("404") ||
                                                  navegador.Url.Contains("/404") ||
                                                  navegador.PageSource.Contains("Sorry, the page you requested cannot be found");

                        if (esProductoInvalido)
                        {
                            throw new Exception("El producto no está disponible o el enlace no existe (404/Agotado).");
                        }

                        // 4. Extraer URL de imagen
                        string urlImagen = ExtraerUrlImagen(navegador);

                        // 5. Descargar imagen y crear miniatura local
                        string rutaImagenLocal = string.Empty;
                        Image? miniatura = null;
                        if (!string.IsNullOrWhiteSpace(urlImagen) && !string.IsNullOrWhiteSpace(carpetaImagenes))
                        {
                            rutaImagenLocal = await _almacenamientoImagenes.DescargarYGuardarAsync(urlImagen, carpetaImagenes, sku, cancelacion);
                            if (!string.IsNullOrWhiteSpace(rutaImagenLocal))
                            {
                                miniatura = _almacenamientoImagenes.CargarMiniatura(rutaImagenLocal, 110, 110);
                            }
                        }

                        // 6. Calcular impuesto (7%) y total con cantidad
                        decimal impuesto = _servicioCalculo.CalcularImpuesto(precioOriginal, cantidadPorDefecto);
                        decimal total = _servicioCalculo.CalcularTotal(precioOriginal, cantidadPorDefecto, impuesto, comisionPorDefecto);

                        var nuevoProducto = new ProductoItem
                        {
                            Sku = sku,
                            Nombre = nombre,
                            Talla = tallaPorDefecto,
                            Color = colorPorDefecto,
                            Cantidad = cantidadPorDefecto,
                            PrecioOriginal = precioOriginal,
                            Impuesto = impuesto,
                            Comision = comisionPorDefecto,
                            Total = total,
                            NombreCliente = clientePorDefecto,
                            UrlProducto = enlaceActual,
                            EstadoExitoso = true,
                            UrlImagen = urlImagen,
                            RutaImagenLocal = rutaImagenLocal,
                            ImagenMiniatura = miniatura,
                            FechaCreacion = DateTime.UtcNow
                        };

                        productosExtraidos.Add(nuevoProducto);

                        progreso?.Report(new ReporteProgresoScraping
                        {
                            IndiceActual = indiceActual,
                            TotalRegistros = urls.Count,
                            Url = enlaceActual,
                            Sku = sku,
                            Mensaje = $"✓ Extraído: {sku} - ${precioOriginal:N2}",
                            EsExitoso = true,
                            Producto = nuevoProducto
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (ExcepcionCaptchaShein)
                    {
                        throw;
                    }
                    catch (Exception excepcionProducto)
                    {
                        var productoFallido = new ProductoItem
                        {
                            Sku = !string.IsNullOrWhiteSpace(sku) ? sku : "ERROR",
                            Nombre = "⚠ No se pudo resolver (Error al extraer)",
                            UrlProducto = enlaceActual,
                            EstadoExitoso = false,
                            MensajeError = excepcionProducto.Message,
                            PrecioOriginal = 0m,
                            Impuesto = 0m,
                            Comision = comisionPorDefecto,
                            Total = 0m,
                            Cantidad = cantidadPorDefecto,
                            Talla = tallaPorDefecto,
                            Color = colorPorDefecto,
                            NombreCliente = clientePorDefecto,
                            FechaCreacion = DateTime.UtcNow
                        };

                        productosExtraidos.Add(productoFallido);

                        progreso?.Report(new ReporteProgresoScraping
                        {
                            IndiceActual = indiceActual,
                            TotalRegistros = urls.Count,
                            Url = enlaceActual,
                            Sku = productoFallido.Sku,
                            Mensaje = $"⚠ Error en enlace: {excepcionProducto.Message}",
                            EsExitoso = false,
                            Producto = productoFallido
                        });
                    }
                }
            }
            finally
            {
                if (navegador != null)
                {
                    try { navegador.Quit(); } catch { }
                    try { navegador.Dispose(); } catch { }
                }
            }

            return productosExtraidos;
        }

        private string ExtraerSku(IWebDriver navegador, string url)
        {
            string sku = "";
            try
            {
                var elemento = navegador.FindElement(By.CssSelector(".product-intro__head-sku span"));
                if (elemento != null && !string.IsNullOrWhiteSpace(elemento.Text))
                {
                    sku = elemento.Text.Replace("SKU: ", "").Replace("SKU:", "").Trim();
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(sku))
            {
                try
                {
                    var listaElementos = navegador.FindElements(By.CssSelector("[data-sku]"));
                    foreach (var elem in listaElementos)
                    {
                        string? atributo = elem.GetAttribute("data-sku");
                        if (!string.IsNullOrWhiteSpace(atributo)) { sku = atributo; break; }
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(sku))
            {
                var coincidencia = Regex.Match(url, @"/p-(\d+)(?:-\d+)?\.html");
                sku = coincidencia.Success ? coincidencia.Groups[1].Value : $"SKU_{Guid.NewGuid():N}".Substring(0, 12);
            }

            return sku;
        }

        private string ExtraerNombre(IWebDriver navegador)
        {
            var selectoresNombre = new[]
            {
                ".product-intro__head-name",
                "h1.product-intro__head-name",
                "h1.name",
                ".goods-name",
                "h1"
            };

            var espera = new WebDriverWait(navegador, TimeSpan.FromSeconds(30));
            try
            {
                var elemento = espera.Until(d =>
                {
                    foreach (var selector in selectoresNombre)
                    {
                        var elementos = d.FindElements(By.CssSelector(selector));
                        foreach (var el in elementos)
                        {
                            string t = ObtenerTextoElemento(el);
                            if (!string.IsNullOrWhiteSpace(t))
                            {
                                return el;
                            }
                        }
                    }
                    return null;
                });

                if (elemento != null)
                {
                    string texto = ObtenerTextoElemento(elemento);
                    if (!string.IsNullOrWhiteSpace(texto))
                    {
                        return texto;
                    }
                }
            }
            catch { }

            return "Producto Shein";
        }

        private static string ObtenerTextoElemento(IWebElement? elemento)
        {
            if (elemento == null) return string.Empty;
            try
            {
                string texto = elemento.Text;
                if (string.IsNullOrWhiteSpace(texto))
                {
                    texto = elemento.GetAttribute("innerText") ?? elemento.GetAttribute("textContent") ?? string.Empty;
                }
                return texto.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private decimal ExtraerPrecioOriginal(IWebDriver navegador)
        {
            var selectoresOriginal = new[]
            {
                ".product-intro__head-price .original-price",
                ".product-intro__head-price .del-price",
                "del.original-price",
                "span.original-price",
                "p.productDiscountInfo__retail",
                "p.productEstimatedTagNewRetail__retail",
                ".product-intro__head-price del",
                "del.del-price"
            };

            foreach (var selector in selectoresOriginal)
            {
                try
                {
                    var elementos = navegador.FindElements(By.CssSelector(selector));
                    foreach (var elemento in elementos)
                    {
                        string texto = ObtenerTextoElemento(elemento);
                        if (!string.IsNullOrWhiteSpace(texto))
                        {
                            decimal precio = ParsearPrecio(texto);
                            if (precio > 0) return precio;
                        }
                    }
                }
                catch { }
            }

            var selectoresNormales = new[]
            {
                "span.price-real",
                ".productPrice__main",
                ".productPrice__main span:nth-of-type(2)",
                ".productPrice__main span",
                ".product-intro__head-price .sale-price",
                ".product-intro__head-price .normal-price",
                ".product-intro__head-price span",
                ".from-price",
                ".productPrice__real"
            };

            foreach (var selector in selectoresNormales)
            {
                try
                {
                    var elementos = navegador.FindElements(By.CssSelector(selector));
                    foreach (var elemento in elementos)
                    {
                        string texto = ObtenerTextoElemento(elemento);
                        if (!string.IsNullOrWhiteSpace(texto))
                        {
                            decimal precio = ParsearPrecio(texto);
                            if (precio > 0) return precio;
                        }
                    }
                }
                catch { }
            }

            return 0.00m;
        }

        private decimal ParsearPrecio(string textoPrecio)
        {
            if (string.IsNullOrWhiteSpace(textoPrecio)) return 0;

            var coincidencia = Regex.Match(textoPrecio, @"\d+([.,]\d+)?");
            if (coincidencia.Success)
            {
                string valorNormalizado = coincidencia.Value.Replace(',', '.');
                if (decimal.TryParse(valorNormalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultado))
                {
                    return resultado;
                }
            }
            return 0;
        }

        private string ExtraerUrlImagen(IWebDriver navegador)
        {
            var selectoresImagen = new[]
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
                    var imagenes = navegador.FindElements(By.CssSelector(selector));
                    foreach (var img in imagenes)
                    {
                        string? origenSrc = img.GetAttribute("src");
                        string? origenDataSrc = img.GetAttribute("data-src");

                        string? urlFinal = !string.IsNullOrEmpty(origenSrc) && !origenSrc.Contains("bg-grey") && !origenSrc.Contains("placeholder")
                            ? origenSrc
                            : origenDataSrc;

                        if (!string.IsNullOrEmpty(urlFinal) && !urlFinal.Contains("placeholder") && !urlFinal.Contains("bg-grey"))
                        {
                            if (urlFinal.StartsWith("//"))
                            {
                                urlFinal = "https:" + urlFinal;
                            }
                            return urlFinal;
                        }
                    }
                }
                catch { }
            }

            return string.Empty;
        }

        public async Task<ProductoItem> ExtraerIndividualAsync(
            string url,
            string carpetaImagenes,
            decimal comisionPorDefecto,
            string clientePorDefecto,
            string tallaPorDefecto = "",
            string colorPorDefecto = "",
            int cantidadPorDefecto = 1,
            CancellationToken cancelacion = default)
        {
            var lista = await ExtraerLoteAsync(
                new List<string> { url },
                carpetaImagenes,
                comisionPorDefecto,
                clientePorDefecto,
                tallaPorDefecto,
                colorPorDefecto,
                cantidadPorDefecto,
                null,
                cancelacion);

            if (lista.Count > 0)
            {
                return lista[0];
            }

            return new ProductoItem
            {
                Sku = "ERROR",
                Nombre = "⚠ No se pudo resolver (Error al extraer)",
                UrlProducto = url,
                EstadoExitoso = false,
                PrecioOriginal = 0m,
                Impuesto = 0m,
                Comision = comisionPorDefecto,
                Total = 0m,
                Cantidad = cantidadPorDefecto,
                Talla = tallaPorDefecto,
                Color = colorPorDefecto,
                NombreCliente = clientePorDefecto,
                FechaCreacion = DateTime.UtcNow
            };
        }

        private bool DetectarCaptchaReal(IWebDriver navegador)
        {
            try
            {
                string url = navegador.Url.ToLowerInvariant();
                string titulo = navegador.Title.ToLowerInvariant();

                if (url.Contains("/risk/challenge") || url.Contains("/verify") || url.Contains("captcha_type"))
                {
                    return true;
                }

                if (titulo.Contains("security verification") || titulo.Contains("verificación de seguridad"))
                {
                    return true;
                }

                var selectoresCaptcha = new[]
                {
                    ".geetest_slider_button",
                    ".geetest_radar_tip",
                    ".geetest_popup_wrap",
                    ".sec-slider-wrap",
                    "#captcha_challenge"
                };

                foreach (var selector in selectoresCaptcha)
                {
                    var elementos = navegador.FindElements(By.CssSelector(selector));
                    if (elementos.Count > 0 && elementos.Any(e => e.Displayed))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private bool IntentarResolverCaptchaAutomatico(IWebDriver navegador)
        {
            try
            {
                // 1. Si hay un botón inicial de "Haz clic para verificar" (Geetest radar)
                var selectoresBoton = new[] { ".geetest_radar_tip", ".geetest_radar_btn", ".geetest_radar", ".sec-click-btn" };
                foreach (var selector in selectoresBoton)
                {
                    var elementos = navegador.FindElements(By.CssSelector(selector));
                    var boton = elementos.FirstOrDefault(e => e.Displayed);
                    if (boton != null)
                    {
                        new Actions(navegador).MoveToElement(boton).Click().Perform();
                        Thread.Sleep(2000);
                        break;
                    }
                }

                if (!DetectarCaptchaReal(navegador)) return true;

                // 2. Si hay un deslizador (Geetest slider)
                var selectoresDeslizador = new[] { ".geetest_slider_button", ".geetest_btn", ".sec-slider-btn", ".nc_iconfont.btn_slide", ".geetest_slider" };
                IWebElement? deslizador = null;
                foreach (var selector in selectoresDeslizador)
                {
                    var elementos = navegador.FindElements(By.CssSelector(selector));
                    deslizador = elementos.FirstOrDefault(e => e.Displayed);
                    if (deslizador != null) break;
                }

                if (deslizador != null)
                {
                    var accion = new Actions(navegador);
                    accion.ClickAndHold(deslizador);

                    int distanciaObjetivo = Random.Shared.Next(240, 270);
                    int recorrido = 0;

                    while (recorrido < distanciaObjetivo)
                    {
                        int paso = Random.Shared.Next(15, 35);
                        if (recorrido + paso > distanciaObjetivo) paso = distanciaObjetivo - recorrido;
                        recorrido += paso;

                        int desvioY = Random.Shared.Next(-2, 3);
                        accion.MoveByOffset(paso, desvioY);
                        accion.Perform();
                        Thread.Sleep(Random.Shared.Next(15, 40));
                    }

                    Thread.Sleep(Random.Shared.Next(200, 400));
                    accion.Release().Perform();

                    Thread.Sleep(3500);

                    if (!DetectarCaptchaReal(navegador))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return !DetectarCaptchaReal(navegador);
        }
    }
}

