using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SheinScraperApp.Servicios
{
    public class ServicioAlmacenamientoImagenesLocal : IServicioAlmacenamientoImagenes
    {
        private static readonly HttpClient _clienteHttp = new HttpClient();

        public async Task<string> DescargarYGuardarAsync(string urlImagen, string carpetaDestino, string sku, CancellationToken cancelacion = default)
        {
            if (string.IsNullOrWhiteSpace(urlImagen) || string.IsNullOrWhiteSpace(carpetaDestino) || string.IsNullOrWhiteSpace(sku))
            {
                return string.Empty;
            }

            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            if (urlImagen.StartsWith("//"))
            {
                urlImagen = "https:" + urlImagen;
            }

            string skuLimpio = string.Join("_", sku.Split(Path.GetInvalidFileNameChars()));
            string rutaArchivo = Path.Combine(carpetaDestino, $"{skuLimpio}.jpg");

            try
            {
                using var respuesta = await _clienteHttp.GetAsync(urlImagen, HttpCompletionOption.ResponseHeadersRead, cancelacion);
                if (!respuesta.IsSuccessStatusCode) return string.Empty;

                byte[] bytesImagen = await respuesta.Content.ReadAsByteArrayAsync(cancelacion);
                await File.WriteAllBytesAsync(rutaArchivo, bytesImagen, cancelacion);
                return rutaArchivo;
            }
            catch
            {
                return string.Empty;
            }
        }

        public string ResolverRutaLocal(string? rutaGuardada, string carpetaImagenes, string? sku = null)
        {
            // 1. Si la ruta guardada ya existe directamente en el disco actual
            if (!string.IsNullOrWhiteSpace(rutaGuardada) && File.Exists(rutaGuardada))
            {
                return Path.GetFullPath(rutaGuardada);
            }

            // 2. Si la ruta guardada era una ruta absoluta de otro equipo, buscar el archivo por su nombre dentro de la carpeta actual
            if (!string.IsNullOrWhiteSpace(rutaGuardada))
            {
                string nombreArchivo = Path.GetFileName(rutaGuardada);
                if (!string.IsNullOrWhiteSpace(nombreArchivo))
                {
                    if (!string.IsNullOrWhiteSpace(carpetaImagenes))
                    {
                        string candidata = Path.Combine(carpetaImagenes, nombreArchivo);
                        if (File.Exists(candidata))
                        {
                            return Path.GetFullPath(candidata);
                        }
                    }

                    string candidataBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", nombreArchivo);
                    if (File.Exists(candidataBase))
                    {
                        return Path.GetFullPath(candidataBase);
                    }
                }
            }

            // 3. Respaldo directo por SKU
            if (!string.IsNullOrWhiteSpace(sku))
            {
                string skuLimpio = string.Join("_", sku.Split(Path.GetInvalidFileNameChars()));
                if (!string.IsNullOrWhiteSpace(carpetaImagenes))
                {
                    string candidataSku = Path.Combine(carpetaImagenes, $"{skuLimpio}.jpg");
                    if (File.Exists(candidataSku))
                    {
                        return Path.GetFullPath(candidataSku);
                    }
                }

                string candidataSkuBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", $"{skuLimpio}.jpg");
                if (File.Exists(candidataSkuBase))
                {
                    return Path.GetFullPath(candidataSkuBase);
                }
            }

            return string.Empty;
        }

        public Image? CargarMiniatura(string rutaLocal, int ancho = 110, int alto = 110)
        {
            if (string.IsNullOrWhiteSpace(rutaLocal))
            {
                return null;
            }

            string rutaFinal = rutaLocal;
            if (!File.Exists(rutaFinal))
            {
                string nombre = Path.GetFileName(rutaLocal);
                string rutaBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", nombre);
                if (File.Exists(rutaBase))
                {
                    rutaFinal = rutaBase;
                }
                else
                {
                    return null;
                }
            }

            try
            {
                // Carga segura en memoria para jamás bloquear el archivo en disco
                byte[] bytes = File.ReadAllBytes(rutaFinal);
                using var flujoMemoria = new MemoryStream(bytes);
                using var imagenOriginal = Image.FromStream(flujoMemoria);

                var miniatura = new Bitmap(ancho, alto);
                using var graficos = Graphics.FromImage(miniatura);
                graficos.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graficos.SmoothingMode = SmoothingMode.HighQuality;
                graficos.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float escalaX = (float)ancho / imagenOriginal.Width;
                float escalaY = (float)alto / imagenOriginal.Height;
                float escala = Math.Min(escalaX, escalaY);

                int anchoFinal = (int)(imagenOriginal.Width * escala);
                int altoFinal = (int)(imagenOriginal.Height * escala);
                int posicionX = (ancho - anchoFinal) / 2;
                int posicionY = (alto - altoFinal) / 2;

                graficos.Clear(Color.Transparent);
                graficos.DrawImage(imagenOriginal, posicionX, posicionY, anchoFinal, altoFinal);

                return miniatura;
            }
            catch
            {
                return null;
            }
        }
    }
}

