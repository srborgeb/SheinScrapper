using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.Json;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace SheinScraperApp.Servicios
{
    /// <summary>
    /// Administrador centralizado y resiliente para el ciclo de vida del navegador Chrome y ChromeDriver.
    /// Resuelve problemas de conectividad local (localhost vs 127.0.0.1 / IPv6 / Proxies),
    /// bloqueos de Windows SmartScreen (:Zone.Identifier), corrupción de perfiles (EOF at line 1 column 0)
    /// y procesos huérfanos entre diferentes máquinas.
    /// </summary>
    public static class GestorNavegadorChrome
    {
        private static readonly object _bloqueoPerfil = new object();

        /// <summary>
        /// Obtiene la ruta estable del perfil de Chrome para el scraper en LocalApplicationData.
        /// </summary>
        public static string ObtenerRutaPerfil()
        {
            try
            {
                string rutaBase = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SheinScraperApp",
                    "ChromeProfile");

                if (!Directory.Exists(rutaBase))
                {
                    Directory.CreateDirectory(rutaBase);
                }

                return rutaBase;
            }
            catch
            {
                string rutaTemp = Path.Combine(Path.GetTempPath(), "SheinScraperChromeProfile");
                if (!Directory.Exists(rutaTemp))
                {
                    Directory.CreateDirectory(rutaTemp);
                }
                return rutaTemp;
            }
        }

        /// <summary>
        /// Crea e inicializa una instancia de IWebDriver de manera resiliente.
        /// Implementa auto-recuperación automática ante fallos de inicio del servicio (Cannot start the driver service),
        /// resolución de localhost IPv6, proxies y perfiles bloqueados.
        /// </summary>
        public static IWebDriver CrearNavegador(bool modoSinCabeza)
        {
            lock (_bloqueoPerfil)
            {
                string rutaPerfil = ObtenerRutaPerfil();

                // 1. Limpieza preventiva de archivos JSON vacíos o corruptos
                SanitizarPerfil(rutaPerfil);

                // También sanear residuos en Temp si existían
                try
                {
                    string rutaTempAntigua = Path.Combine(Path.GetTempPath(), "SheinScraperChromeProfile");
                    if (Directory.Exists(rutaTempAntigua))
                    {
                        SanitizarPerfil(rutaTempAntigua);
                    }
                }
                catch { }

                // 2. Intento principal: Conectar mediante 127.0.0.1 y perfil persistente
                try
                {
                    return IniciarInstanciaChromeDriver(rutaPerfil, modoSinCabeza);
                }
                catch (Exception ex) when (EsErrorRecuperable(ex))
                {
                    Debug.WriteLine($"[GestorNavegadorChrome] Fallo en intento 1 ({ex.Message}). Iniciando recuperación...");

                    // 3. Auto-recuperación: Cerrar procesos zombi, limpiar perfil y desbloquear binarios
                    CerrarProcesosHuerfanos(rutaPerfil);
                    LimpiarDirectorioPerfilCompleto(rutaPerfil);
                    DesbloquearEjecutablesLocales();
                    Thread.Sleep(800);

                    try
                    {
                        // Intentar configurar driver si Selenium Manager necesitara respaldo
                        IntentarConfigurarDriver();
                        return IniciarInstanciaChromeDriver(rutaPerfil, modoSinCabeza);
                    }
                    catch (Exception exReintento) when (EsErrorRecuperable(exReintento))
                    {
                        Debug.WriteLine($"[GestorNavegadorChrome] Reintento con perfil falló ({exReintento.Message}). Iniciando en modo perfil temporal aislado...");

                        // 4. Último recurso infalible: Iniciar sin ruta de perfil personalizada (perfil efímero en memoria)
                        try
                        {
                            return IniciarInstanciaChromeDriver(null, modoSinCabeza);
                        }
                        catch (Exception exFinal)
                        {
                            throw new Exception(
                                $"No se pudo iniciar el servicio de Chrome/ChromeDriver.\n\n" +
                                $"Detalle técnico: {exFinal.Message}\n\n" +
                                $"Pasos recomendados para resolverlo en este equipo:\n" +
                                $"1. Asegúrate de que Google Chrome esté instalado.\n" +
                                $"2. Si tienes antivirus (ESET, Defender, etc.), verifica que no esté bloqueando o poniendo en cuarentena 'chromedriver.exe'.\n" +
                                $"3. Si usas un Proxy corporativo o VPN, asegúrate de que permita conexiones locales a 127.0.0.1.", exFinal);
                        }
                    }
                }
            }
        }

        private static void IntentarConfigurarDriver()
        {
            try
            {
                new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GestorNavegadorChrome] WebDriverManager no disponible, usando Selenium Manager: {ex.Message}");
            }
        }

        private static ChromeDriverService CrearServicioControlador()
        {
            // 1. Desbloquear ejecutables en la carpeta del aplicativo para eliminar el 'Mark of the Web' (:Zone.Identifier)
            DesbloquearEjecutablesLocales();

            // 2. Configurar variables de entorno para forzar a .NET a omitir proxies en loopback local
            Environment.SetEnvironmentVariable("NO_PROXY", "localhost,127.0.0.1");
            Environment.SetEnvironmentVariable("no_proxy", "localhost,127.0.0.1");

            var servicio = ChromeDriverService.CreateDefaultService();

            // 3. CRÍTICO: Forzar 127.0.0.1 en lugar de localhost para evitar fallos de resolución IPv6 (::1)
            // que ocasionan: "Cannot start the driver service on http://localhost:PORT/"
            servicio.HostName = "127.0.0.1";
            servicio.AllowedIPAddresses = "127.0.0.1";
            servicio.SuppressInitialDiagnosticInformation = true;
            servicio.HideCommandPromptWindow = true;
            servicio.InitializationTimeout = TimeSpan.FromSeconds(45);

            return servicio;
        }

        private static IWebDriver IniciarInstanciaChromeDriver(string? rutaPerfil, bool modoSinCabeza)
        {
            var servicioControlador = CrearServicioControlador();

            var opcionesNavegador = new ChromeOptions();

            if (!string.IsNullOrWhiteSpace(rutaPerfil))
            {
                opcionesNavegador.AddArgument($"--user-data-dir={rutaPerfil}");
                opcionesNavegador.AddArgument("--profile-directory=Default");
            }

            // Opciones anti-detección y estabilidad entre diferentes máquinas
            opcionesNavegador.AddArgument("--disable-blink-features=AutomationControlled");
            opcionesNavegador.AddExcludedArgument("enable-automation");
            opcionesNavegador.AddArgument("--disable-infobars");
            opcionesNavegador.AddArgument("--disable-notifications");
            opcionesNavegador.AddArgument("--remote-allow-origins=*");
            opcionesNavegador.AddArgument("--no-first-run");
            opcionesNavegador.AddArgument("--no-default-browser-check");
            opcionesNavegador.AddArgument("--disable-dev-shm-usage");
            opcionesNavegador.AddArgument("--disable-gpu");
            opcionesNavegador.AddArgument("--no-sandbox");
            opcionesNavegador.AddArgument("--lang=es-ES,es");

            if (modoSinCabeza)
            {
                opcionesNavegador.AddArgument("--window-position=-32000,-32000");
                opcionesNavegador.AddArgument("--window-size=1920,1080");
            }
            else
            {
                opcionesNavegador.AddArgument("--start-maximized");
            }

            var driver = new ChromeDriver(servicioControlador, opcionesNavegador);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            return driver;
        }

        /// <summary>
        /// Determina si una excepción en el inicio del navegador es recuperable mediante reintentos y limpieza.
        /// </summary>
        private static bool EsErrorRecuperable(Exception ex)
        {
            string msg = (ex.Message + " " + (ex.InnerException?.Message ?? "")).ToLowerInvariant();
            return msg.Contains("cannot start the driver service") ||
                   msg.Contains("session not created") ||
                   msg.Contains("cannot parse internal json") ||
                   msg.Contains("eof while parsing") ||
                   msg.Contains("devtoolsactiveport") ||
                   msg.Contains("user data directory is already in use") ||
                   msg.Contains("profile") ||
                   msg.Contains("timed out") ||
                   msg.Contains("timeout") ||
                   msg.Contains("refused") ||
                   msg.Contains("chrome failed to start");
        }

        /// <summary>
        /// Elimina el flujo de datos 'Zone.Identifier' (Mark of the Web) de todos los ejecutables
        /// para evitar que Windows SmartScreen o Defender bloqueen la ejecución de chromedriver.exe al ser copiado entre equipos.
        /// </summary>
        public static void DesbloquearEjecutablesLocales()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (Directory.Exists(baseDir))
                {
                    var archivosExe = Directory.GetFiles(baseDir, "*.exe", SearchOption.AllDirectories);
                    foreach (var archivo in archivosExe)
                    {
                        try
                        {
                            string rutaZone = archivo + ":Zone.Identifier";
                            if (File.Exists(rutaZone))
                            {
                                File.Delete(rutaZone);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Inspecciona el directorio de perfil y elimina cualquier archivo JSON o de preferencias que esté vacío (0 bytes) o corrupto.
        /// </summary>
        public static void SanitizarPerfil(string rutaPerfil)
        {
            try
            {
                if (!Directory.Exists(rutaPerfil)) return;

                // Cerrar procesos huérfanos que puedan tener bloqueados los archivos
                CerrarProcesosHuerfanos(rutaPerfil);

                // 1. Eliminar residuos de DevToolsActivePort
                EliminarArchivoSeguro(Path.Combine(rutaPerfil, "DevToolsActivePort"));

                // 2. Validar Local State en la raíz del perfil
                ValidarOEliminarJson(Path.Combine(rutaPerfil, "Local State"));

                // 3. Validar Preferences y Secure Preferences en Default
                string dirDefault = Path.Combine(rutaPerfil, "Default");
                if (Directory.Exists(dirDefault))
                {
                    ValidarOEliminarJson(Path.Combine(dirDefault, "Preferences"));
                    ValidarOEliminarJson(Path.Combine(dirDefault, "Secure Preferences"));
                }

                // 4. Barrido general de archivos vacíos de 0 bytes en todo el árbol de perfil
                var archivos = Directory.GetFiles(rutaPerfil, "*", SearchOption.AllDirectories);
                foreach (var archivo in archivos)
                {
                    try
                    {
                        var info = new FileInfo(archivo);
                        if (info.Exists && info.Length == 0)
                        {
                            string nombre = info.Name.ToLowerInvariant();
                            if (nombre.Contains("preferences") || nombre.Contains("state") || nombre.EndsWith(".json"))
                            {
                                EliminarArchivoSeguro(archivo);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GestorNavegadorChrome] Error al sanitizar perfil: {ex.Message}");
            }
        }

        private static void ValidarOEliminarJson(string rutaArchivo)
        {
            try
            {
                if (!File.Exists(rutaArchivo)) return;

                var info = new FileInfo(rutaArchivo);
                if (info.Length == 0)
                {
                    EliminarArchivoSeguro(rutaArchivo);
                    return;
                }

                string contenido = File.ReadAllText(rutaArchivo).Trim();
                if (string.IsNullOrWhiteSpace(contenido))
                {
                    EliminarArchivoSeguro(rutaArchivo);
                    return;
                }

                try
                {
                    using var doc = JsonDocument.Parse(contenido);
                }
                catch
                {
                    EliminarArchivoSeguro(rutaArchivo);
                }
            }
            catch
            {
                EliminarArchivoSeguro(rutaArchivo);
            }
        }

        private static void EliminarArchivoSeguro(string rutaArchivo)
        {
            try
            {
                if (File.Exists(rutaArchivo))
                {
                    File.SetAttributes(rutaArchivo, FileAttributes.Normal);
                    File.Delete(rutaArchivo);
                }
            }
            catch { }
        }

        /// <summary>
        /// Elimina por completo el directorio de perfil si está irreversiblemente corrupto.
        /// </summary>
        public static void LimpiarDirectorioPerfilCompleto(string rutaPerfil)
        {
            try
            {
                CerrarProcesosHuerfanos(rutaPerfil);
                if (Directory.Exists(rutaPerfil))
                {
                    Directory.Delete(rutaPerfil, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GestorNavegadorChrome] No se pudo eliminar directorio completo: {ex.Message}");
            }
        }

        /// <summary>
        /// Finaliza procesos de chromedriver y chrome que pertenezcan a este perfil o hayan quedado huérfanos.
        /// No afecta las ventanas de Chrome de uso personal del usuario.
        /// </summary>
        public static void CerrarProcesosHuerfanos(string rutaPerfil)
        {
            try
            {
                // Terminar chromedriver huérfanos
                foreach (var proc in Process.GetProcessesByName("chromedriver"))
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(500);
                    }
                    catch { }
                }

                // Terminar instancias de chrome.exe que estén usando específicamente nuestro perfil
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'chrome.exe'");

                foreach (var obj in searcher.Get())
                {
                    string? cmdLine = obj["CommandLine"]?.ToString();
                    if (!string.IsNullOrEmpty(cmdLine))
                    {
                        bool coincidePerfil = cmdLine.IndexOf(rutaPerfil, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              cmdLine.IndexOf("SheinScraperChromeProfile", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (coincidePerfil)
                        {
                            try
                            {
                                int pid = Convert.ToInt32(obj["ProcessId"]);
                                var proc = Process.GetProcessById(pid);
                                proc.Kill();
                                proc.WaitForExit(500);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch
            {
                // Si WMI no estuviera disponible, continuar sin bloquear
            }
        }
    }
}

