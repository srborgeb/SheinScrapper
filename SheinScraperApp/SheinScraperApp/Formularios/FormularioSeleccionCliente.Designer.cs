namespace SheinScraperApp.Formularios
{
    partial class FormularioSeleccionCliente
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.etiquetaInstruccion = new Telerik.WinControls.UI.RadLabel();
            this.listaClientesBox = new Telerik.WinControls.UI.RadListControl();
            this.botonAceptar = new Telerik.WinControls.UI.RadButton();
            this.botonCancelar = new Telerik.WinControls.UI.RadButton();

            ((System.ComponentModel.ISupportInitialize)(this.etiquetaInstruccion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.listaClientesBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.botonAceptar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.botonCancelar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();

            // 
            // etiquetaInstruccion
            // 
            this.etiquetaInstruccion.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.etiquetaInstruccion.Location = new System.Drawing.Point(16, 14);
            this.etiquetaInstruccion.Name = "etiquetaInstruccion";
            this.etiquetaInstruccion.Size = new System.Drawing.Size(310, 20);
            this.etiquetaInstruccion.TabIndex = 0;
            this.etiquetaInstruccion.Text = "Selecciona el cliente para exportar la estimación:";
            this.etiquetaInstruccion.ThemeName = "Fluent";

            // 
            // listaClientesBox
            // 
            this.listaClientesBox.ItemHeight = 28;
            this.listaClientesBox.Location = new System.Drawing.Point(16, 42);
            this.listaClientesBox.Name = "listaClientesBox";
            this.listaClientesBox.Size = new System.Drawing.Size(350, 190);
            this.listaClientesBox.TabIndex = 1;
            this.listaClientesBox.ThemeName = "Fluent";

            // 
            // botonAceptar
            // 
            this.botonAceptar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.botonAceptar.Location = new System.Drawing.Point(90, 245);
            this.botonAceptar.Name = "botonAceptar";
            this.botonAceptar.Size = new System.Drawing.Size(135, 34);
            this.botonAceptar.TabIndex = 2;
            this.botonAceptar.Text = "Exportar PDF";
            this.botonAceptar.ThemeName = "Fluent";
            this.botonAceptar.Click += new System.EventHandler(this.BotonAceptar_Click);

            // 
            // botonCancelar
            // 
            this.botonCancelar.Location = new System.Drawing.Point(235, 245);
            this.botonCancelar.Name = "botonCancelar";
            this.botonCancelar.Size = new System.Drawing.Size(130, 34);
            this.botonCancelar.TabIndex = 3;
            this.botonCancelar.Text = "Cancelar";
            this.botonCancelar.ThemeName = "Fluent";
            this.botonCancelar.Click += new System.EventHandler(this.BotonCancelar_Click);

            // 
            // FormularioSeleccionCliente
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 295);
            this.Controls.Add(this.botonCancelar);
            this.Controls.Add(this.botonAceptar);
            this.Controls.Add(this.listaClientesBox);
            this.Controls.Add(this.etiquetaInstruccion);
            this.Name = "FormularioSeleccionCliente";
            this.Text = "Seleccionar Cliente";
            this.ThemeName = "Fluent";
            ((System.ComponentModel.ISupportInitialize)(this.etiquetaInstruccion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.listaClientesBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.botonAceptar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.botonCancelar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Telerik.WinControls.UI.RadLabel etiquetaInstruccion;
        private Telerik.WinControls.UI.RadListControl listaClientesBox;
        private Telerik.WinControls.UI.RadButton botonAceptar;
        private Telerik.WinControls.UI.RadButton botonCancelar;
    }
}

