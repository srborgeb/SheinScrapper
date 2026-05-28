
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
            EnvioTextBox = new TextBox();
            ClienteTextBox = new TextBox();
            label2 = new Label();
            label3 = new Label();
            TallaTextBox = new TextBox();
            Talla = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(31, 95);
            label1.Name = "label1";
            label1.Size = new Size(185, 15);
            label1.TabIndex = 0;
            label1.Text = "Introduce URL del producto Shein";
            // 
            // txtUrlProducto
            // 
            txtUrlProducto.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUrlProducto.Location = new Point(222, 92);
            txtUrlProducto.Multiline = true;
            txtUrlProducto.Name = "txtUrlProducto";
            txtUrlProducto.Size = new Size(656, 175);
            txtUrlProducto.TabIndex = 1;
            // 
            // btnScrape
            // 
            btnScrape.Location = new Point(31, 344);
            btnScrape.Name = "btnScrape";
            btnScrape.Size = new Size(151, 50);
            btnScrape.TabIndex = 5;
            btnScrape.Text = "Iniciar Scrape";
            btnScrape.UseVisualStyleBackColor = true;
            btnScrape.Click += btnScrape_Click;
            // 
            // rtbResultado
            // 
            rtbResultado.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbResultado.Location = new Point(188, 344);
            rtbResultado.Name = "rtbResultado";
            rtbResultado.ReadOnly = true;
            rtbResultado.Size = new Size(690, 210);
            rtbResultado.TabIndex = 3;
            rtbResultado.Text = "";
            // 
            // btnSeleccionarDirectorio
            // 
            btnSeleccionarDirectorio.Location = new Point(31, 12);
            btnSeleccionarDirectorio.Name = "btnSeleccionarDirectorio";
            btnSeleccionarDirectorio.Size = new Size(151, 72);
            btnSeleccionarDirectorio.TabIndex = 0;
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
            btnGuardarExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnGuardarExcel.Location = new Point(31, 482);
            btnGuardarExcel.Name = "btnGuardarExcel";
            btnGuardarExcel.Size = new Size(151, 72);
            btnGuardarExcel.TabIndex = 6;
            btnGuardarExcel.Text = "Guardar en Excel";
            btnGuardarExcel.UseVisualStyleBackColor = true;
            btnGuardarExcel.Click += btnGuardarExcel_Click;
            // 
            // EnvioTextBox
            // 
            EnvioTextBox.Location = new Point(555, 286);
            EnvioTextBox.Name = "EnvioTextBox";
            EnvioTextBox.Size = new Size(151, 23);
            EnvioTextBox.TabIndex = 4;
            EnvioTextBox.TextChanged += textBox1_TextChanged;
            EnvioTextBox.KeyPress += Valor_KeyPress;
            // 
            // ClienteTextBox
            // 
            ClienteTextBox.ForeColor = SystemColors.ControlText;
            ClienteTextBox.Location = new Point(99, 286);
            ClienteTextBox.Name = "ClienteTextBox";
            ClienteTextBox.Size = new Size(128, 23);
            ClienteTextBox.TabIndex = 2;
            ClienteTextBox.KeyPress += Nombre_KeyPress;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(514, 289);
            label2.Name = "label2";
            label2.Size = new Size(39, 15);
            label2.TabIndex = 9;
            label2.Text = "Envio:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(46, 289);
            label3.Name = "label3";
            label3.Size = new Size(47, 15);
            label3.TabIndex = 10;
            label3.Text = "Cliente:";
            // 
            // TallaTextBox
            // 
            TallaTextBox.Location = new Point(318, 286);
            TallaTextBox.Name = "TallaTextBox";
            TallaTextBox.Size = new Size(151, 23);
            TallaTextBox.TabIndex = 3;
            TallaTextBox.KeyPress += Talla_KeyPress;
            // 
            // Talla
            // 
            Talla.AutoSize = true;
            Talla.Location = new Point(279, 289);
            Talla.Name = "Talla";
            Talla.Size = new Size(34, 15);
            Talla.TabIndex = 12;
            Talla.Text = "Talla:";
            // 
            // formScrap
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(891, 572);
            Controls.Add(Talla);
            Controls.Add(TallaTextBox);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(ClienteTextBox);
            Controls.Add(EnvioTextBox);
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
            WindowState = FormWindowState.Maximized;
            ResumeLayout(false);
            PerformLayout();
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        private Label label1;
        private TextBox txtUrlProducto;
        private Button btnScrape;
        private RichTextBox rtbResultado;
        private Button btnSeleccionarDirectorio;
        private Label lblDirectorio;
        private Button btnGuardarExcel;
        private TextBox EnvioTextBox;
        private TextBox ClienteTextBox;
        private Label label2;
        private Label label3;
        private TextBox TallaTextBox;
        private Label Talla;
    }
}
