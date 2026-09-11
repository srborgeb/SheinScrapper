using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SheinScraperApp.Modelos;

namespace SheinScraperApp.Servicios
{
    public class ServicioExportacionExcel : IServicioExportacionExcel
    {
        public Task<string> ExportarPresupuestoAsync(
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
                    ? "ClienteGeneral" 
                    : string.Join("_", nombreCliente.Trim().Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

                if (string.IsNullOrWhiteSpace(carpetaDestino))
                {
                    carpetaDestino = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (!Directory.Exists(carpetaDestino))
                {
                    Directory.CreateDirectory(carpetaDestino);
                }

                // Formato exacto requerido: Nombre_YYYYMMDD_hhmm.xlsx
                string nombreArchivo = $"{clienteLimpio}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                string rutaFinal = Path.Combine(carpetaDestino, nombreArchivo);

                // Si está abierto en Excel, agregar segundos para evitar error de bloqueo
                try
                {
                    if (File.Exists(rutaFinal))
                    {
                        using var prueba = File.Open(rutaFinal, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                }
                catch (IOException)
                {
                    rutaFinal = Path.Combine(carpetaDestino, $"{clienteLimpio}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                }

                using var libroExcel = new XLWorkbook();
                var hoja = libroExcel.Worksheets.Add("Presupuesto");

                // 1. Banner Principal de Presupuesto
                hoja.Range("A1:J1").Merge();
                var celdaTitulo = hoja.Cell(1, 1);
                celdaTitulo.Value = "PRESUPUESTO / ESTIMACIÓN DE COMPRA SHEIN";
                celdaTitulo.Style.Font.Bold = true;
                celdaTitulo.Style.Font.FontSize = 14;
                celdaTitulo.Style.Font.FontColor = XLColor.White;
                celdaTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hoja.Row(1).Height = 32;

                // 2. Información de Cliente y Fecha
                hoja.Cell(2, 1).Value = $"Cliente: {(string.IsNullOrWhiteSpace(nombreCliente) ? "Todos" : nombreCliente)}";
                hoja.Cell(2, 1).Style.Font.Bold = true;
                hoja.Cell(2, 1).Style.Font.FontSize = 11;

                hoja.Cell(2, 10).Value = $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}";
                hoja.Cell(2, 10).Style.Font.Italic = true;
                hoja.Cell(2, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // 3. Encabezados de la Tabla
                string[] encabezados = new[]
                {
                    "N°",
                    "SKU",
                    "Nombre del Producto",
                    "Talla",
                    "Color",
                    "Cant.",
                    "Precio Unit.",
                    "Impuesto (7%)",
                    "Comisión",
                    "Total"
                };

                int filaEncabezado = 4;
                for (int col = 0; col < encabezados.Length; col++)
                {
                    var celda = hoja.Cell(filaEncabezado, col + 1);
                    celda.Value = encabezados[col];
                    celda.Style.Font.Bold = true;
                    celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
                    celda.Style.Font.FontColor = XLColor.White;
                    celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    celda.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                hoja.Row(filaEncabezado).Height = 22;

                // 4. Filas de Datos
                int filaInicioDatos = 5;
                int filaActual = filaInicioDatos;
                int contador = 1;

                foreach (var item in listaFiltrada)
                {
                    hoja.Cell(filaActual, 1).Value = contador++;
                    hoja.Cell(filaActual, 2).Value = item.Sku;
                    hoja.Cell(filaActual, 3).Value = item.Nombre;
                    hoja.Cell(filaActual, 4).Value = string.IsNullOrWhiteSpace(item.Talla) ? "-" : item.Talla;
                    hoja.Cell(filaActual, 5).Value = string.IsNullOrWhiteSpace(item.Color) ? "-" : item.Color;
                    hoja.Cell(filaActual, 6).Value = item.Cantidad;
                    hoja.Cell(filaActual, 7).Value = item.PrecioOriginal;
                    hoja.Cell(filaActual, 8).Value = item.Impuesto;
                    hoja.Cell(filaActual, 9).Value = item.Comision;
                    hoja.Cell(filaActual, 10).Value = item.Total;

                    // Alineaciones
                    hoja.Cell(filaActual, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hoja.Cell(filaActual, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hoja.Cell(filaActual, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hoja.Cell(filaActual, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hoja.Cell(filaActual, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Formatos de moneda
                    hoja.Cell(filaActual, 7).Style.NumberFormat.Format = "$#,##0.00";
                    hoja.Cell(filaActual, 8).Style.NumberFormat.Format = "$#,##0.00";
                    hoja.Cell(filaActual, 9).Style.NumberFormat.Format = "$#,##0.00";
                    hoja.Cell(filaActual, 10).Style.NumberFormat.Format = "$#,##0.00";

                    // Bordes delgados
                    hoja.Range(filaActual, 1, filaActual, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    hoja.Range(filaActual, 1, filaActual, 10).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    filaActual++;
                }

                int filaFinDatos = filaActual - 1;

                // 5. Fila Final de TOTALES (Presupuesto / Estimación)
                int filaTotales = filaActual;
                hoja.Range(filaTotales, 1, filaTotales, 5).Merge();
                var celdaEtiquetaTotales = hoja.Cell(filaTotales, 1);
                celdaEtiquetaTotales.Value = "TOTAL GENERAL DEL PRESUPUESTO:";
                celdaEtiquetaTotales.Style.Font.Bold = true;
                celdaEtiquetaTotales.Style.Font.FontSize = 11;
                celdaEtiquetaTotales.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Sumas de columnas usando fórmulas de Excel
                if (filaFinDatos >= filaInicioDatos)
                {
                    hoja.Cell(filaTotales, 6).FormulaA1 = $"SUM(F{filaInicioDatos}:F{filaFinDatos})";
                    hoja.Cell(filaTotales, 8).FormulaA1 = $"SUM(H{filaInicioDatos}:H{filaFinDatos})";
                    hoja.Cell(filaTotales, 9).FormulaA1 = $"SUM(I{filaInicioDatos}:I{filaFinDatos})";
                    hoja.Cell(filaTotales, 10).FormulaA1 = $"SUM(J{filaInicioDatos}:J{filaFinDatos})";
                }
                else
                {
                    hoja.Cell(filaTotales, 6).Value = 0;
                    hoja.Cell(filaTotales, 8).Value = 0;
                    hoja.Cell(filaTotales, 9).Value = 0;
                    hoja.Cell(filaTotales, 10).Value = 0;
                }

                // Estilos de la fila de totales
                hoja.Cell(filaTotales, 6).Style.Font.Bold = true;
                hoja.Cell(filaTotales, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                hoja.Cell(filaTotales, 8).Style.NumberFormat.Format = "$#,##0.00";
                hoja.Cell(filaTotales, 8).Style.Font.Bold = true;

                hoja.Cell(filaTotales, 9).Style.NumberFormat.Format = "$#,##0.00";
                hoja.Cell(filaTotales, 9).Style.Font.Bold = true;

                hoja.Cell(filaTotales, 10).Style.NumberFormat.Format = "$#,##0.00";
                hoja.Cell(filaTotales, 10).Style.Font.Bold = true;
                hoja.Cell(filaTotales, 10).Style.Font.FontSize = 12;
                hoja.Cell(filaTotales, 10).Style.Font.FontColor = XLColor.FromHtml("#166534"); // Verde oscuro

                var rangoTotales = hoja.Range(filaTotales, 1, filaTotales, 10);
                rangoTotales.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                rangoTotales.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                rangoTotales.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                hoja.Row(filaTotales).Height = 24;

                hoja.Columns().AdjustToContents();
                libroExcel.SaveAs(rutaFinal);

                return rutaFinal;
            });
        }
    }
}
