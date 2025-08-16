
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
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            label2 = new Label();
            label3 = new Label();
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
            txtUrlProducto.Size = new Size(526, 23);
            txtUrlProducto.TabIndex = 1;
            // 
            // btnScrape
            // 
            btnScrape.Location = new Point(31, 207);
            btnScrape.Name = "btnScrape";
            btnScrape.Size = new Size(151, 50);
            btnScrape.TabIndex = 2;
            btnScrape.Text = "Iniciar Scrape";
            btnScrape.UseVisualStyleBackColor = true;
            btnScrape.Click += btnScrape_Click;
            // 
            // rtbResultado
            // 
            rtbResultado.Location = new Point(188, 207);
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
            btnGuardarExcel.Location = new Point(31, 320);
            btnGuardarExcel.Name = "btnGuardarExcel";
            btnGuardarExcel.Size = new Size(151, 72);
            btnGuardarExcel.TabIndex = 6;
            btnGuardarExcel.Text = "Guardar en Excel";
            btnGuardarExcel.UseVisualStyleBackColor = true;
            btnGuardarExcel.Click += btnGuardarExcel_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(268, 160);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(151, 23);
            textBox1.TabIndex = 7;
            textBox1.TextChanged += textBox1_TextChanged;
            textBox1.KeyPress += Valor_KeyPress;
            // 
            // textBox2
            // 
            textBox2.ForeColor = SystemColors.ControlText;
            textBox2.Location = new Point(569, 160);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(151, 23);
            textBox2.TabIndex = 8;
            textBox2.KeyPress += Nombre_KeyPress;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(226, 163);
            label2.Name = "label2";
            label2.Size = new Size(36, 15);
            label2.TabIndex = 9;
            label2.Text = "Valor:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(509, 163);
            label3.Name = "label3";
            label3.Size = new Size(47, 15);
            label3.TabIndex = 10;
            label3.Text = "Cliente:";
            // 
            // formScrap
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(761, 410);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
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
        private TextBox textBox1;
        private TextBox textBox2;
        private Label label2;
        private Label label3;
    }
}
