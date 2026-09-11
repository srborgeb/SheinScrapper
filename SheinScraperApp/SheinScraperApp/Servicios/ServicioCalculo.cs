using System;

namespace SheinScraperApp.Servicios
{
    public interface IServicioCalculo
    {
        decimal CalcularImpuesto(decimal precioUnitario, int cantidad = 1, decimal tasaImpuesto = 0.07m);
        decimal CalcularTotal(decimal precioUnitario, int cantidad, decimal impuesto, decimal comisionUnitaria);
    }

    public class ServicioCalculo : IServicioCalculo
    {
        public decimal CalcularImpuesto(decimal precioUnitario, int cantidad = 1, decimal tasaImpuesto = 0.07m)
        {
            if (cantidad < 1) cantidad = 1;
            decimal subtotal = Math.Round(precioUnitario * cantidad, 2, MidpointRounding.AwayFromZero);
            return Math.Round(subtotal * tasaImpuesto, 2, MidpointRounding.AwayFromZero);
        }

        public decimal CalcularTotal(decimal precioUnitario, int cantidad, decimal impuesto, decimal comisionUnitaria)
        {
            if (cantidad < 1) cantidad = 1;
            decimal subtotal = Math.Round(precioUnitario * cantidad, 2, MidpointRounding.AwayFromZero);
            decimal comisionTotal = Math.Round(comisionUnitaria * cantidad, 2, MidpointRounding.AwayFromZero);
            return Math.Round(subtotal + impuesto + comisionTotal, 2, MidpointRounding.AwayFromZero);
        }
    }
}

