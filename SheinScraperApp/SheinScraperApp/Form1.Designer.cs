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
            txtPerfilFirefox = new TextBox();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(31, 179);
            label1.Name = "label1";
            label1.Size = new Size(185, 15);
            label1.TabIndex = 0;
            label1.Text = "Introduce URL del producto Shein";
            // 
            // txtUrlProducto
            // 
            txtUrlProducto.Location = new Point(222, 176);
            txtUrlProducto.Name = "txtUrlProducto";
            txtUrlProducto.Size = new Size(560, 23);
            txtUrlProducto.TabIndex = 1;
            // 
            // btnScrape
            // 
            btnScrape.Location = new Point(31, 218);
            btnScrape.Name = "btnScrape";
            btnScrape.Size = new Size(151, 50);
            btnScrape.TabIndex = 2;
            btnScrape.Text = "Iniciar Scrape";
            btnScrape.UseVisualStyleBackColor = true;
            btnScrape.Click += btnScrape_Click;
            // 
            // rtbResultado
            // 
            rtbResultado.Location = new Point(222, 218);
            rtbResultado.Name = "rtbResultado";
            rtbResultado.ReadOnly = true;
            rtbResultado.Size = new Size(560, 185);
            rtbResultado.TabIndex = 3;
            rtbResultado.Text = "";
            // 
            // btnSeleccionarDirectorio
            // 
            btnSeleccionarDirectorio.Location = new Point(31, 424);
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
            lblDirectorio.Location = new Point(222, 453);
            lblDirectorio.Name = "lblDirectorio";
            lblDirectorio.Size = new Size(179, 15);
            lblDirectorio.TabIndex = 5;
            lblDirectorio.Text = "Carpeta: (Ninguna seleccionada)";
            // 
            // btnGuardarExcel
            // 
            btnGuardarExcel.Location = new Point(638, 424);
            btnGuardarExcel.Name = "btnGuardarExcel";
            btnGuardarExcel.Size = new Size(144, 72);
            btnGuardarExcel.TabIndex = 6;
            btnGuardarExcel.Text = "Guardar en Excel";
            btnGuardarExcel.UseVisualStyleBackColor = true;
            btnGuardarExcel.Click += btnGuardarExcel_Click;
            // 
            // txtPerfilFirefox
            // 
            txtPerfilFirefox.Location = new Point(586, 56);
            txtPerfilFirefox.Name = "txtPerfilFirefox";
            txtPerfilFirefox.Size = new Size(196, 23);
            txtPerfilFirefox.TabIndex = 7;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label2.Location = new Point(31, 19);
            label2.Name = "label2";
            label2.Size = new Size(407, 76);
            label2.TabIndex = 8;
            label2.Text = "1 - Inicia Firefox\r\n2 - Colocar en la barra de navegacion about:profiles\r\n3 - Tomar el nombre del perfil de Firefox para ser cargado.\r\n4 - Iniciar WebScraper";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(496, 59);
            label3.Name = "label3";
            label3.Size = new Size(73, 15);
            label3.TabIndex = 9;
            label3.Text = "Perfil Firefox";
            // 
            // formScrap
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(803, 516);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(txtPerfilFirefox);
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
        private TextBox txtPerfilFirefox;
        private Label label2;
        private Label label3;
    }
}
