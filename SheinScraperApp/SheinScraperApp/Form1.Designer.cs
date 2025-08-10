namespace SheinScraperApp
{
    partial class formScrap
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            txtUrlProducto = new TextBox();
            btnScrape = new Button();
            rtbResultado = new RichTextBox();
            btnSeleccionarDirectorio = new Button();
            lblDirectorio = new Label();
            btnGuardarExcel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(31, 116);
            label1.Name = "label1";
            label1.Size = new Size(185, 15);
            label1.TabIndex = 0;
            label1.Text = "Introduce URL del producto Shein";
            // 
            // txtUrlProducto
            // 
            txtUrlProducto.Location = new Point(222, 113);
            txtUrlProducto.Name = "txtUrlProducto";
            txtUrlProducto.Size = new Size(560, 23);
            txtUrlProducto.TabIndex = 1;
            // 
            // btnScrape
            // 
            btnScrape.Location = new Point(31, 160);
            btnScrape.Name = "btnScrape";
            btnScrape.Size = new Size(151, 50);
            btnScrape.TabIndex = 2;
            btnScrape.Text = "Iniciar Scrape";
            btnScrape.UseVisualStyleBackColor = true;
            btnScrape.Click += btnScrape_Click;
            // 
            // rtbResultado
            // 
            rtbResultado.Location = new Point(222, 160);
            rtbResultado.Name = "rtbResultado";
            rtbResultado.ReadOnly = true;
            rtbResultado.Size = new Size(560, 185);
            rtbResultado.TabIndex = 3;
            rtbResultado.Text = "";
            // 
            // btnSeleccionarDirectorio
            // 
            btnSeleccionarDirectorio.Location = new Point(31, 12);
            btnSeleccionarDirectorio.Name = "btnSeleccionarDirectorio";
            btnSeleccionarDirectorio.Size = new Size(151, 72);
            btnSeleccionarDirectorio.TabIndex = 4;
            btnSeleccionarDirectorio.Text = "Seleccionar Carpeta de Descarga";
            btnSeleccionarDirectorio.UseVisualStyleBackColor = true;
            btnSeleccionarDirectorio.Click += btnSeleccionarDirectorio_Click;
            // 
            // lblDirectorio
            // 
            lblDirectorio.AutoSize = true;
            lblDirectorio.Location = new Point(222, 41);
            lblDirectorio.Name = "lblDirectorio";
            lblDirectorio.Size = new Size(179, 15);
            lblDirectorio.TabIndex = 5;
            lblDirectorio.Text = "Carpeta: (Ninguna seleccionada)";
            // 
            // btnGuardarExcel
            // 
            btnGuardarExcel.Location = new Point(31, 356);
            btnGuardarExcel.Name = "btnGuardarExcel";
            btnGuardarExcel.Size = new Size(151, 72);
            btnGuardarExcel.TabIndex = 6;
            btnGuardarExcel.Text = "Guardar en Excel";
            btnGuardarExcel.UseVisualStyleBackColor = true;
            btnGuardarExcel.Click += btnGuardarExcel_Click;
            // 
            // formScrap
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(803, 443);
            Controls.Add(btnGuardarExcel);
            Controls.Add(lblDirectorio);
            Controls.Add(btnSeleccionarDirectorio);
            Controls.Add(rtbResultado);
            Controls.Add(btnScrape);
            Controls.Add(txtUrlProducto);
            Controls.Add(label1);
            Name = "formScrap";
            RightToLeftLayout = true;
            Text = "Shein Scraper";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtUrlProducto;
        private Button btnScrape;
        private RichTextBox rtbResultado;
        private Button btnSeleccionarDirectorio;
        private Label lblDirectorio;
        private Button btnGuardarExcel;
    }
}
