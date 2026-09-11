using System;
using System.Collections.Generic;

namespace SheinScraperApp.Modelos
{
    public class ExcepcionCaptchaShein : Exception
    {
        public string UrlDondeOcurrio { get; }
        public List<string> UrlsPendientes { get; }

        public ExcepcionCaptchaShein(string mensaje, string urlDondeOcurrio, List<string> urlsPendientes)
            : base(mensaje)
        {
            UrlDondeOcurrio = urlDondeOcurrio;
            UrlsPendientes = urlsPendientes ?? new List<string>();
        }
    }
}

