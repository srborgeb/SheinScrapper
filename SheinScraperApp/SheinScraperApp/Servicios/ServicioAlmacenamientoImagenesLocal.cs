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

        public Image? CargarMiniatura(string rutaLocal, int ancho = 110, int alto = 110)
        {
            if (string.IsNullOrWhiteSpace(rutaLocal) || !File.Exists(rutaLocal))
            {
                return null;
            }

            try
            {
                // Carga segura en memoria para jamás bloquear el archivo en disco
                byte[] bytes = File.ReadAllBytes(rutaLocal);
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

