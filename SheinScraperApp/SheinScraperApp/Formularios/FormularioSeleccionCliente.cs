using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace SheinScraperApp.Formularios
{
    public partial class FormularioSeleccionCliente : RadForm
    {
        public string ClienteSeleccionado { get; private set; } = string.Empty;

        public FormularioSeleccionCliente(IEnumerable<string> listaClientes)
        {
            InitializeComponent();

            this.ThemeName = "Fluent";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            botonAceptar.Image = IconChar.FilePdf.ToBitmap(Color.White, 16);
            botonAceptar.TextImageRelation = TextImageRelation.ImageBeforeText;

            botonCancelar.Image = IconChar.Xmark.ToBitmap(Color.White, 16);
            botonCancelar.TextImageRelation = TextImageRelation.ImageBeforeText;

            // Cargar clientes únicos en el ListBox
            var clientes = listaClientes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            listaClientesBox.Items.Clear();
            listaClientesBox.Items.Add("[Todos los Clientes]");

            foreach (var cliente in clientes)
            {
                listaClientesBox.Items.Add(cliente);
            }

            if (listaClientesBox.Items.Count > 1)
            {
                listaClientesBox.SelectedIndex = 1;
            }
            else
            {
                listaClientesBox.SelectedIndex = 0;
            }

            listaClientesBox.DoubleClick += ListaClientesBox_DoubleClick;
        }

        private void ListaClientesBox_DoubleClick(object? sender, EventArgs e)
        {
            ConfirmarSeleccion();
        }

        private void BotonAceptar_Click(object sender, EventArgs e)
        {
            ConfirmarSeleccion();
        }

        private void ConfirmarSeleccion()
        {
            if (listaClientesBox.SelectedItem == null)
            {
                RadMessageBox.Show(this, "Por favor selecciona un cliente de la lista.", "Selección requerida", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            string valor = listaClientesBox.SelectedItem.Text;
            ClienteSeleccionado = (valor == "[Todos los Clientes]") ? string.Empty : valor;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BotonCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
