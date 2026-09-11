using System;
using System.Windows.Forms;
using SheinScraperApp.Formularios;
using Telerik.WinControls;

namespace SheinScraperApp
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            ThemeResolutionService.ApplicationThemeName = "Fluent";
            Application.Run(new FormularioPrincipalRad());
        }
    }
}