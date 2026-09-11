using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Servicios
{
    public class ServicioExportacionPdf : IServicioExportacionPdf
    {
        static ServicioExportacionPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task<string> ExportarEstimacionPdfAsync(
            IEnumerable<ProductoItem> productos,
            string nombreCliente,
            string carpetaDestino)
        {
            return Task.Run(() =>
            {
                var listaFiltrada = productos
                    .Where(p => string.IsNullOrWhiteSpace(nombreCliente) ||
                                p.NombreCliente.Equals(nombreCliente, StringComparison.OrdinalIgnoreCase) ||
                                p.NombreCliente.Contains(nombreCliente, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (listaFiltrada.Count == 0)
                {
                    listaFiltrada = productos.ToList();
                }

                string clienteLimpio = string.IsNullOrWhiteSpace(nombreCliente)
                    ? "Estimacion"
                    : string.Join("_", nombreCliente.Trim().Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

                if (string.IsNullOrWhiteSpace(carpetaDestino))
                {
                    carpetaDestino = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (!Directory.Exists(carpetaDestino))
                {
                    Directory.CreateDirectory(carpetaDestino);
                }

                // Formato exacto: Nombre_YYYYMMDD_hhmm.pdf
                string nombreArchivo = $"{clienteLimpio}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                string rutaFinal = Path.Combine(carpetaDestino, nombreArchivo);

                try
                {
                    if (File.Exists(rutaFinal))
                    {
                        using var prueba = File.Open(rutaFinal, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                }
                catch (IOException)
                {
                    rutaFinal = Path.Combine(carpetaDestino, $"{clienteLimpio}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                }

                int totalUnidades = listaFiltrada.Sum(p => p.Cantidad);
                decimal totalGeneral = listaFiltrada.Sum(p => p.Total);
                string fechaHoraEmision = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                Document.Create(documento =>
                {
                    documento.Page(pagina =>
                    {
                        // 1. Orientación horizontal (Landscape)
                        pagina.Size(PageSizes.A4.Landscape());
                        pagina.Margin(20);
                        pagina.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                        // 2. Encabezado: Solo "ESTIMACION" y la Fecha/Hora (Sin nombre del cliente)
                        pagina.Header().Column(columnaEncabezado =>
                        {
                            columnaEncabezado.Item().Row(fila =>
                            {
                                fila.RelativeItem().Text("ESTIMACION")
                                    .FontSize(20)
                                    .Bold()
                                    .FontColor("#1E293B");

                                fila.RelativeItem().AlignRight().Text($"Fecha y Hora: {fechaHoraEmision}")
                                    .FontSize(10)
                                    .Italic()
                                    .FontColor("#64748B");
                            });

                            columnaEncabezado.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#CBD5E1");
                        });

                        // 3. Contenido: Tabla de Productos con Imágenes ampliadas
                        pagina.Content().PaddingTop(10).Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.ConstantColumn(90);     // Imagen
                                columnas.RelativeColumn(3.0f);   // Nombre
                                columnas.RelativeColumn(1.2f);   // SKU
                                columnas.RelativeColumn(0.7f);   // Talla
                                columnas.RelativeColumn(0.8f);   // Color
                                columnas.RelativeColumn(0.8f);   // Unidades
                                columnas.RelativeColumn(1.0f);   // Precio Unit.
                                columnas.RelativeColumn(1.0f);   // Impuesto
                                columnas.RelativeColumn(1.0f);   // Comisión
                                columnas.RelativeColumn(1.2f);   // Total
                                columnas.RelativeColumn(0.8f);   // Enlace
                            });

                            // Fila de Encabezados
                            tabla.Header(encabezado =>
                            {
                                void CeldaEncabezado(IContainer contenedor, string texto, bool alinearDerecha = false, bool alinearCentro = false)
                                {
                                    var celda = contenedor
                                        .Background("#1E293B")
                                        .PaddingVertical(6)
                                        .PaddingHorizontal(4);

                                    if (alinearDerecha) celda = celda.AlignRight();
                                    else if (alinearCentro) celda = celda.AlignCenter();
                                    else celda = celda.AlignLeft();

                                    celda.Text(texto).Bold().FontColor("#FFFFFF").FontSize(8.5f);
                                }

                                CeldaEncabezado(encabezado.Cell(), "Imagen", alinearCentro: true);
                                CeldaEncabezado(encabezado.Cell(), "Nombre del Producto");
                                CeldaEncabezado(encabezado.Cell(), "SKU", alinearCentro: true);
                                CeldaEncabezado(encabezado.Cell(), "Talla", alinearCentro: true);
                                CeldaEncabezado(encabezado.Cell(), "Color", alinearCentro: true);
                                CeldaEncabezado(encabezado.Cell(), "Unidades", alinearCentro: true);
                                CeldaEncabezado(encabezado.Cell(), "Precio Unit.", alinearDerecha: true);
                                CeldaEncabezado(encabezado.Cell(), "Impuesto (7%)", alinearDerecha: true);
                                CeldaEncabezado(encabezado.Cell(), "Comisión", alinearDerecha: true);
                                CeldaEncabezado(encabezado.Cell(), "Total", alinearDerecha: true);
                                CeldaEncabezado(encabezado.Cell(), "Enlace", alinearCentro: true);
                            });

                            // Filas de Datos
                            int indiceFila = 0;
                            foreach (var item in listaFiltrada)
                            {
                                string colorFondo = (indiceFila % 2 == 0) ? "#FFFFFF" : "#F8FAFC";
                                indiceFila++;

                                IContainer CeldaDatos(IContainer c, bool alinearDerecha = false, bool alinearCentro = false, bool bordeAbajo = true)
                                {
                                    var contenedor = c.Background(colorFondo)
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(3);
                                    if (bordeAbajo)
                                    {
                                        contenedor = contenedor.BorderBottom(0.5f).BorderColor("#E2E8F0");
                                    }
                                    if (alinearDerecha) contenedor = contenedor.AlignRight();
                                    else if (alinearCentro) contenedor = contenedor.AlignCenter();
                                    else contenedor = contenedor.AlignLeft();

                                    return contenedor.AlignMiddle();
                                }

                                // 1. Imagen incrustada ampliada
                                var celdaImg = CeldaDatos(tabla.Cell(), alinearCentro: true);
                                if (!string.IsNullOrWhiteSpace(item.RutaImagenLocal) && File.Exists(item.RutaImagenLocal))
                                {
                                    try
                                    {
                                        byte[] bytesImagen = File.ReadAllBytes(item.RutaImagenLocal);
                                        celdaImg.MaxHeight(80).Image(bytesImagen).FitArea();
                                    }
                                    catch
                                    {
                                        celdaImg.Text("-").FontSize(8);
                                    }
                                }
                                else
                                {
                                    celdaImg.Text("-").FontSize(8);
                                }

                                // 2. Nombre
                                CeldaDatos(tabla.Cell()).Text(item.Nombre).FontSize(8.5f);

                                // 3. SKU
                                CeldaDatos(tabla.Cell(), alinearCentro: true).Text(item.Sku).FontSize(8);

                                // 4. Talla
                                CeldaDatos(tabla.Cell(), alinearCentro: true).Text(string.IsNullOrWhiteSpace(item.Talla) ? "-" : item.Talla).FontSize(8.5f);

                                // 5. Color
                                CeldaDatos(tabla.Cell(), alinearCentro: true).Text(string.IsNullOrWhiteSpace(item.Color) ? "-" : item.Color).FontSize(8.5f);

                                // 6. Unidades
                                CeldaDatos(tabla.Cell(), alinearCentro: true).Text(item.Cantidad.ToString()).Bold().FontSize(9f);

                                // 7. Precio Unitario (Siempre con símbolo $)
                                CeldaDatos(tabla.Cell(), alinearDerecha: true).Text($"${item.PrecioOriginal:N2}").FontSize(8.5f);

                                // 8. Impuesto (Siempre con símbolo $)
                                CeldaDatos(tabla.Cell(), alinearDerecha: true).Text($"${item.Impuesto:N2}").FontSize(8.5f);

                                // 9. Comisión (Siempre con símbolo $)
                                CeldaDatos(tabla.Cell(), alinearDerecha: true).Text($"${item.Comision:N2}").FontSize(8.5f);

                                // 10. Total (Siempre con símbolo $)
                                CeldaDatos(tabla.Cell(), alinearDerecha: true).Text($"${item.Total:N2}").Bold().FontSize(9f);

                                // 11. Enlace con Hipervínculo
                                var celdaEnlace = CeldaDatos(tabla.Cell(), alinearCentro: true);
                                if (!string.IsNullOrWhiteSpace(item.UrlProducto) && item.EstadoExitoso)
                                {
                                    celdaEnlace.Hyperlink(item.UrlProducto)
                                        .Text("Enlace").Bold().FontSize(8.5f).FontColor("#2563EB").Underline();
                                }
                                else
                                {
                                    celdaEnlace.Text("-").FontSize(8f).FontColor("#94A3B8");
                                }
                            }

                            // 4. Fila Final de Totales (Solo Unidades y TOTAL GENERAL con símbolo $)
                            tabla.Cell().ColumnSpan(5)
                                .Background("#F1F5F9")
                                .BorderTop(1.5f).BorderColor("#0F172A")
                                .BorderBottom(1.5f).BorderColor("#0F172A")
                                .PaddingVertical(8)
                                .PaddingHorizontal(4)
                                .AlignRight().AlignMiddle()
                                .Text("Unidades:").Bold().FontSize(9.5f);

                            tabla.Cell()
                                .Background("#F1F5F9")
                                .BorderTop(1.5f).BorderColor("#0F172A")
                                .BorderBottom(1.5f).BorderColor("#0F172A")
                                .PaddingVertical(8)
                                .AlignCenter().AlignMiddle()
                                .Text(totalUnidades.ToString()).Bold().FontSize(10.5f);

                            tabla.Cell().ColumnSpan(3)
                                .Background("#F1F5F9")
                                .BorderTop(1.5f).BorderColor("#0F172A")
                                .BorderBottom(1.5f).BorderColor("#0F172A")
                                .PaddingVertical(8)
                                .PaddingHorizontal(4)
                                .AlignRight().AlignMiddle()
                                .Text("TOTAL GENERAL:").Bold().FontSize(10.5f).FontColor("#0F172A");

                            tabla.Cell()
                                .Background("#F1F5F9")
                                .BorderTop(1.5f).BorderColor("#0F172A")
                                .BorderBottom(1.5f).BorderColor("#0F172A")
                                .PaddingVertical(8)
                                .PaddingHorizontal(4)
                                .AlignRight().AlignMiddle()
                                .Text($"${totalGeneral:N2}").Bold().FontSize(12).FontColor("#166534");

                            tabla.Cell()
                                .Background("#F1F5F9")
                                .BorderTop(1.5f).BorderColor("#0F172A")
                                .BorderBottom(1.5f).BorderColor("#0F172A");
                        });

                        // 5. Pie de Página
                        pagina.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                }).GeneratePdf(rutaFinal);

                return rutaFinal;
            });
        }
    }
}
