namespace InterfazPlantaCtrlTemp
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.title = new System.Windows.Forms.Label();
            this.numericVelVent = new System.Windows.Forms.NumericUpDown();
            this.numericPotCal = new System.Windows.Forms.NumericUpDown();
            this.groupCtrlMaqueta = new System.Windows.Forms.GroupBox();
            this.trackPotCal = new System.Windows.Forms.TrackBar();
            this.trackVelVent = new System.Windows.Forms.TrackBar();
            this.buttonCargar = new System.Windows.Forms.Button();
            this.titleCal = new System.Windows.Forms.Label();
            this.titleVent = new System.Windows.Forms.Label();
            this.labelCal = new System.Windows.Forms.Label();
            this.labelVent = new System.Windows.Forms.Label();
            this.groupCtrlEntradas = new System.Windows.Forms.GroupBox();
            this.labelBtnRampa = new System.Windows.Forms.Label();
            this.labelBtnEscalon = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonCargarEntradas = new System.Windows.Forms.Button();
            this.labelTFinalCal = new System.Windows.Forms.Label();
            this.numericTFinalCal = new System.Windows.Forms.NumericUpDown();
            this.labelConsignaCal = new System.Windows.Forms.Label();
            this.numericConsignaCal = new System.Windows.Forms.NumericUpDown();
            this.labelTInicioCal = new System.Windows.Forms.Label();
            this.numericTInicioCal = new System.Windows.Forms.NumericUpDown();
            this.buttonEscCal = new System.Windows.Forms.Button();
            this.buttonEscVent = new System.Windows.Forms.Button();
            this.labelTFinalVent = new System.Windows.Forms.Label();
            this.numericTFinalVent = new System.Windows.Forms.NumericUpDown();
            this.labelConsignaVent = new System.Windows.Forms.Label();
            this.numericConsignaVent = new System.Windows.Forms.NumericUpDown();
            this.labelTInicioVent = new System.Windows.Forms.Label();
            this.numericTInicioVent = new System.Windows.Forms.NumericUpDown();
            this.buttonEntrRampa = new System.Windows.Forms.Button();
            this.buttonEntrEscalon = new System.Windows.Forms.Button();
            this.groupGrafico = new System.Windows.Forms.GroupBox();
            this.buttonOcultar = new System.Windows.Forms.Button();
            this.entradaChart = new LiveCharts.WinForms.CartesianChart();
            this.tempChart = new LiveCharts.WinForms.CartesianChart();
            this.comBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnConectar = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.labelTEjec = new System.Windows.Forms.Label();
            this.numericTEjecucion = new System.Windows.Forms.NumericUpDown();
            this.labelTEjecucion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericVelVent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPotCal)).BeginInit();
            this.groupCtrlMaqueta.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackPotCal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackVelVent)).BeginInit();
            this.groupCtrlEntradas.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericTFinalCal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericConsignaCal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTInicioCal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTFinalVent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericConsignaVent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTInicioVent)).BeginInit();
            this.groupGrafico.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericTEjecucion)).BeginInit();
            this.SuspendLayout();
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.Font = new System.Drawing.Font("Verdana", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.title.ForeColor = System.Drawing.Color.Purple;
            this.title.Location = new System.Drawing.Point(239, 9);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(773, 45);
            this.title.TabIndex = 0;
            this.title.Text = "Maqueta de Control de Temperatura";
            // 
            // numericVelVent
            // 
            this.numericVelVent.Location = new System.Drawing.Point(288, 66);
            this.numericVelVent.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.numericVelVent.Name = "numericVelVent";
            this.numericVelVent.Size = new System.Drawing.Size(38, 20);
            this.numericVelVent.TabIndex = 1;
            this.numericVelVent.Tag = "";
            this.numericVelVent.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // numericPotCal
            // 
            this.numericPotCal.Location = new System.Drawing.Point(288, 169);
            this.numericPotCal.Maximum = new decimal(new int[] {
            85,
            0,
            0,
            0});
            this.numericPotCal.Name = "numericPotCal";
            this.numericPotCal.Size = new System.Drawing.Size(38, 20);
            this.numericPotCal.TabIndex = 2;
            // 
            // groupCtrlMaqueta
            // 
            this.groupCtrlMaqueta.Controls.Add(this.trackPotCal);
            this.groupCtrlMaqueta.Controls.Add(this.trackVelVent);
            this.groupCtrlMaqueta.Controls.Add(this.buttonCargar);
            this.groupCtrlMaqueta.Controls.Add(this.titleCal);
            this.groupCtrlMaqueta.Controls.Add(this.titleVent);
            this.groupCtrlMaqueta.Controls.Add(this.labelCal);
            this.groupCtrlMaqueta.Controls.Add(this.labelVent);
            this.groupCtrlMaqueta.Controls.Add(this.numericPotCal);
            this.groupCtrlMaqueta.Controls.Add(this.numericVelVent);
            this.groupCtrlMaqueta.Location = new System.Drawing.Point(12, 178);
            this.groupCtrlMaqueta.Name = "groupCtrlMaqueta";
            this.groupCtrlMaqueta.Size = new System.Drawing.Size(350, 243);
            this.groupCtrlMaqueta.TabIndex = 3;
            this.groupCtrlMaqueta.TabStop = false;
            this.groupCtrlMaqueta.Text = "Control de la Maqueta";
            // 
            // trackPotCal
            // 
            this.trackPotCal.Location = new System.Drawing.Point(14, 169);
            this.trackPotCal.Maximum = 85;
            this.trackPotCal.Name = "trackPotCal";
            this.trackPotCal.Size = new System.Drawing.Size(268, 45);
            this.trackPotCal.TabIndex = 8;
            this.trackPotCal.TickFrequency = 5;
            // 
            // trackVelVent
            // 
            this.trackVelVent.Location = new System.Drawing.Point(14, 66);
            this.trackVelVent.Maximum = 100;
            this.trackVelVent.Minimum = 40;
            this.trackVelVent.Name = "trackVelVent";
            this.trackVelVent.Size = new System.Drawing.Size(268, 45);
            this.trackVelVent.TabIndex = 6;
            this.trackVelVent.TickFrequency = 5;
            this.trackVelVent.Value = 40;
            // 
            // buttonCargar
            // 
            this.buttonCargar.Location = new System.Drawing.Point(269, 213);
            this.buttonCargar.Name = "buttonCargar";
            this.buttonCargar.Size = new System.Drawing.Size(75, 23);
            this.buttonCargar.TabIndex = 7;
            this.buttonCargar.Text = "Cargar";
            this.buttonCargar.UseVisualStyleBackColor = true;
            this.buttonCargar.Click += new System.EventHandler(this.BtnCargar_Click);
            // 
            // titleCal
            // 
            this.titleCal.AutoSize = true;
            this.titleCal.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleCal.Location = new System.Drawing.Point(9, 133);
            this.titleCal.Name = "titleCal";
            this.titleCal.Size = new System.Drawing.Size(262, 29);
            this.titleCal.TabIndex = 6;
            this.titleCal.Text = "Potencia del Calefactor";
            // 
            // titleVent
            // 
            this.titleVent.AutoSize = true;
            this.titleVent.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleVent.Location = new System.Drawing.Point(9, 29);
            this.titleVent.Name = "titleVent";
            this.titleVent.Size = new System.Drawing.Size(276, 29);
            this.titleVent.TabIndex = 5;
            this.titleVent.Text = "Velocidad del Ventilador";
            // 
            // labelCal
            // 
            this.labelCal.AutoSize = true;
            this.labelCal.Location = new System.Drawing.Point(11, 217);
            this.labelCal.Name = "labelCal";
            this.labelCal.Size = new System.Drawing.Size(50, 13);
            this.labelCal.TabIndex = 4;
            this.labelCal.Text = "Max 85%";
            // 
            // labelVent
            // 
            this.labelVent.AutoSize = true;
            this.labelVent.Location = new System.Drawing.Point(11, 112);
            this.labelVent.Name = "labelVent";
            this.labelVent.Size = new System.Drawing.Size(47, 13);
            this.labelVent.TabIndex = 3;
            this.labelVent.Text = "Min 40%";
            // 
            // groupCtrlEntradas
            // 
            this.groupCtrlEntradas.Controls.Add(this.labelBtnRampa);
            this.groupCtrlEntradas.Controls.Add(this.labelBtnEscalon);
            this.groupCtrlEntradas.Controls.Add(this.groupBox1);
            this.groupCtrlEntradas.Controls.Add(this.buttonEntrRampa);
            this.groupCtrlEntradas.Controls.Add(this.buttonEntrEscalon);
            this.groupCtrlEntradas.Location = new System.Drawing.Point(12, 427);
            this.groupCtrlEntradas.Name = "groupCtrlEntradas";
            this.groupCtrlEntradas.Size = new System.Drawing.Size(350, 364);
            this.groupCtrlEntradas.TabIndex = 4;
            this.groupCtrlEntradas.TabStop = false;
            this.groupCtrlEntradas.Text = "Control de Entradas del Sistema";
            // 
            // labelBtnRampa
            // 
            this.labelBtnRampa.AutoSize = true;
            this.labelBtnRampa.Location = new System.Drawing.Point(204, 91);
            this.labelBtnRampa.Name = "labelBtnRampa";
            this.labelBtnRampa.Size = new System.Drawing.Size(81, 13);
            this.labelBtnRampa.TabIndex = 7;
            this.labelBtnRampa.Text = "Entrada Rampa";
            // 
            // labelBtnEscalon
            // 
            this.labelBtnEscalon.AutoSize = true;
            this.labelBtnEscalon.Location = new System.Drawing.Point(58, 91);
            this.labelBtnEscalon.Name = "labelBtnEscalon";
            this.labelBtnEscalon.Size = new System.Drawing.Size(85, 13);
            this.labelBtnEscalon.TabIndex = 6;
            this.labelBtnEscalon.Text = "Entrada Escalón";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonCargarEntradas);
            this.groupBox1.Controls.Add(this.labelTFinalCal);
            this.groupBox1.Controls.Add(this.numericTFinalCal);
            this.groupBox1.Controls.Add(this.labelConsignaCal);
            this.groupBox1.Controls.Add(this.numericConsignaCal);
            this.groupBox1.Controls.Add(this.labelTInicioCal);
            this.groupBox1.Controls.Add(this.numericTInicioCal);
            this.groupBox1.Controls.Add(this.buttonEscCal);
            this.groupBox1.Controls.Add(this.buttonEscVent);
            this.groupBox1.Controls.Add(this.labelTFinalVent);
            this.groupBox1.Controls.Add(this.numericTFinalVent);
            this.groupBox1.Controls.Add(this.labelConsignaVent);
            this.groupBox1.Controls.Add(this.numericConsignaVent);
            this.groupBox1.Controls.Add(this.labelTInicioVent);
            this.groupBox1.Controls.Add(this.numericTInicioVent);
            this.groupBox1.Location = new System.Drawing.Point(0, 120);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(350, 245);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            // 
            // buttonCargarEntradas
            // 
            this.buttonCargarEntradas.Location = new System.Drawing.Point(269, 218);
            this.buttonCargarEntradas.Name = "buttonCargarEntradas";
            this.buttonCargarEntradas.Size = new System.Drawing.Size(75, 23);
            this.buttonCargarEntradas.TabIndex = 10;
            this.buttonCargarEntradas.Text = "Cargar";
            this.buttonCargarEntradas.UseVisualStyleBackColor = true;
            this.buttonCargarEntradas.Click += new System.EventHandler(this.buttonCargarEntradas_Click);
            // 
            // labelTFinalCal
            // 
            this.labelTFinalCal.AutoSize = true;
            this.labelTFinalCal.Location = new System.Drawing.Point(199, 175);
            this.labelTFinalCal.Name = "labelTFinalCal";
            this.labelTFinalCal.Size = new System.Drawing.Size(42, 13);
            this.labelTFinalCal.TabIndex = 21;
            this.labelTFinalCal.Text = "T. Final";
            // 
            // numericTFinalCal
            // 
            this.numericTFinalCal.DecimalPlaces = 1;
            this.numericTFinalCal.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericTFinalCal.Location = new System.Drawing.Point(194, 191);
            this.numericTFinalCal.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericTFinalCal.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericTFinalCal.Name = "numericTFinalCal";
            this.numericTFinalCal.Size = new System.Drawing.Size(120, 20);
            this.numericTFinalCal.TabIndex = 20;
            this.numericTFinalCal.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // labelConsignaCal
            // 
            this.labelConsignaCal.AutoSize = true;
            this.labelConsignaCal.Location = new System.Drawing.Point(199, 89);
            this.labelConsignaCal.Name = "labelConsignaCal";
            this.labelConsignaCal.Size = new System.Drawing.Size(51, 13);
            this.labelConsignaCal.TabIndex = 19;
            this.labelConsignaCal.Text = "Consigna";
            // 
            // numericConsignaCal
            // 
            this.numericConsignaCal.Location = new System.Drawing.Point(194, 105);
            this.numericConsignaCal.Maximum = new decimal(new int[] {
            85,
            0,
            0,
            0});
            this.numericConsignaCal.Name = "numericConsignaCal";
            this.numericConsignaCal.Size = new System.Drawing.Size(120, 20);
            this.numericConsignaCal.TabIndex = 18;
            // 
            // labelTInicioCal
            // 
            this.labelTInicioCal.AutoSize = true;
            this.labelTInicioCal.Location = new System.Drawing.Point(199, 131);
            this.labelTInicioCal.Name = "labelTInicioCal";
            this.labelTInicioCal.Size = new System.Drawing.Size(45, 13);
            this.labelTInicioCal.TabIndex = 17;
            this.labelTInicioCal.Text = "T. Inicio";
            // 
            // numericTInicioCal
            // 
            this.numericTInicioCal.DecimalPlaces = 1;
            this.numericTInicioCal.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericTInicioCal.Location = new System.Drawing.Point(194, 147);
            this.numericTInicioCal.Maximum = new decimal(new int[] {
            19,
            0,
            0,
            0});
            this.numericTInicioCal.Name = "numericTInicioCal";
            this.numericTInicioCal.Size = new System.Drawing.Size(120, 20);
            this.numericTInicioCal.TabIndex = 16;
            // 
            // buttonEscCal
            // 
            this.buttonEscCal.Image = ((System.Drawing.Image)(resources.GetObject("buttonEscCal.Image")));
            this.buttonEscCal.Location = new System.Drawing.Point(216, 25);
            this.buttonEscCal.Name = "buttonEscCal";
            this.buttonEscCal.Size = new System.Drawing.Size(55, 55);
            this.buttonEscCal.TabIndex = 15;
            this.buttonEscCal.UseVisualStyleBackColor = true;
            this.buttonEscCal.Click += new System.EventHandler(this.buttonEscCal_Click);
            // 
            // buttonEscVent
            // 
            this.buttonEscVent.Image = ((System.Drawing.Image)(resources.GetObject("buttonEscVent.Image")));
            this.buttonEscVent.Location = new System.Drawing.Point(70, 25);
            this.buttonEscVent.Name = "buttonEscVent";
            this.buttonEscVent.Size = new System.Drawing.Size(55, 55);
            this.buttonEscVent.TabIndex = 14;
            this.buttonEscVent.UseVisualStyleBackColor = true;
            this.buttonEscVent.Click += new System.EventHandler(this.buttonEscVent_Click);
            // 
            // labelTFinalVent
            // 
            this.labelTFinalVent.AutoSize = true;
            this.labelTFinalVent.Location = new System.Drawing.Point(22, 175);
            this.labelTFinalVent.Name = "labelTFinalVent";
            this.labelTFinalVent.Size = new System.Drawing.Size(42, 13);
            this.labelTFinalVent.TabIndex = 11;
            this.labelTFinalVent.Text = "T. Final";
            // 
            // numericTFinalVent
            // 
            this.numericTFinalVent.DecimalPlaces = 1;
            this.numericTFinalVent.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericTFinalVent.Location = new System.Drawing.Point(17, 191);
            this.numericTFinalVent.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericTFinalVent.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericTFinalVent.Name = "numericTFinalVent";
            this.numericTFinalVent.Size = new System.Drawing.Size(120, 20);
            this.numericTFinalVent.TabIndex = 10;
            this.numericTFinalVent.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // labelConsignaVent
            // 
            this.labelConsignaVent.AutoSize = true;
            this.labelConsignaVent.Location = new System.Drawing.Point(22, 89);
            this.labelConsignaVent.Name = "labelConsignaVent";
            this.labelConsignaVent.Size = new System.Drawing.Size(51, 13);
            this.labelConsignaVent.TabIndex = 9;
            this.labelConsignaVent.Text = "Consigna";
            // 
            // numericConsignaVent
            // 
            this.numericConsignaVent.Location = new System.Drawing.Point(17, 105);
            this.numericConsignaVent.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.numericConsignaVent.Name = "numericConsignaVent";
            this.numericConsignaVent.Size = new System.Drawing.Size(120, 20);
            this.numericConsignaVent.TabIndex = 8;
            this.numericConsignaVent.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // labelTInicioVent
            // 
            this.labelTInicioVent.AutoSize = true;
            this.labelTInicioVent.Location = new System.Drawing.Point(22, 131);
            this.labelTInicioVent.Name = "labelTInicioVent";
            this.labelTInicioVent.Size = new System.Drawing.Size(45, 13);
            this.labelTInicioVent.TabIndex = 7;
            this.labelTInicioVent.Text = "T. Inicio";
            // 
            // numericTInicioVent
            // 
            this.numericTInicioVent.DecimalPlaces = 1;
            this.numericTInicioVent.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericTInicioVent.Location = new System.Drawing.Point(17, 147);
            this.numericTInicioVent.Maximum = new decimal(new int[] {
            19,
            0,
            0,
            0});
            this.numericTInicioVent.Name = "numericTInicioVent";
            this.numericTInicioVent.Size = new System.Drawing.Size(120, 20);
            this.numericTInicioVent.TabIndex = 0;
            // 
            // buttonEntrRampa
            // 
            this.buttonEntrRampa.Image = ((System.Drawing.Image)(resources.GetObject("buttonEntrRampa.Image")));
            this.buttonEntrRampa.Location = new System.Drawing.Point(215, 29);
            this.buttonEntrRampa.Name = "buttonEntrRampa";
            this.buttonEntrRampa.Size = new System.Drawing.Size(58, 59);
            this.buttonEntrRampa.TabIndex = 1;
            this.buttonEntrRampa.UseVisualStyleBackColor = true;
            this.buttonEntrRampa.Click += new System.EventHandler(this.buttonEntrRampa_Click);
            // 
            // buttonEntrEscalon
            // 
            this.buttonEntrEscalon.Image = ((System.Drawing.Image)(resources.GetObject("buttonEntrEscalon.Image")));
            this.buttonEntrEscalon.Location = new System.Drawing.Point(69, 29);
            this.buttonEntrEscalon.Name = "buttonEntrEscalon";
            this.buttonEntrEscalon.Size = new System.Drawing.Size(58, 59);
            this.buttonEntrEscalon.TabIndex = 0;
            this.buttonEntrEscalon.UseVisualStyleBackColor = true;
            this.buttonEntrEscalon.Click += new System.EventHandler(this.buttonEntrEscalon_Click);
            // 
            // groupGrafico
            // 
            this.groupGrafico.Controls.Add(this.buttonOcultar);
            this.groupGrafico.Controls.Add(this.entradaChart);
            this.groupGrafico.Controls.Add(this.tempChart);
            this.groupGrafico.Location = new System.Drawing.Point(382, 77);
            this.groupGrafico.Name = "groupGrafico";
            this.groupGrafico.Size = new System.Drawing.Size(833, 715);
            this.groupGrafico.TabIndex = 5;
            this.groupGrafico.TabStop = false;
            this.groupGrafico.Text = "Gráfica de Temperatura";
            // 
            // buttonOcultar
            // 
            this.buttonOcultar.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonOcultar.BackgroundImage")));
            this.buttonOcultar.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonOcultar.Location = new System.Drawing.Point(7, 684);
            this.buttonOcultar.Name = "buttonOcultar";
            this.buttonOcultar.Size = new System.Drawing.Size(34, 24);
            this.buttonOcultar.TabIndex = 2;
            this.buttonOcultar.UseVisualStyleBackColor = true;
            this.buttonOcultar.Visible = false;
            this.buttonOcultar.Click += new System.EventHandler(this.buttonOcultar_Click);
            // 
            // entradaChart
            // 
            this.entradaChart.Location = new System.Drawing.Point(15, 350);
            this.entradaChart.Name = "entradaChart";
            this.entradaChart.Size = new System.Drawing.Size(810, 330);
            this.entradaChart.TabIndex = 1;
            this.entradaChart.Text = "cartesianChart1";
            this.entradaChart.Visible = false;
            // 
            // tempChart
            // 
            this.tempChart.Location = new System.Drawing.Point(15, 20);
            this.tempChart.Name = "tempChart";
            this.tempChart.Size = new System.Drawing.Size(810, 660);
            this.tempChart.TabIndex = 0;
            this.tempChart.Text = "cartesianChart1";
            // 
            // comBox
            // 
            this.comBox.FormattingEnabled = true;
            this.comBox.Location = new System.Drawing.Point(137, 18);
            this.comBox.Name = "comBox";
            this.comBox.Size = new System.Drawing.Size(121, 21);
            this.comBox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(125, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Seleccionar Puerto USB:";
            // 
            // btnConectar
            // 
            this.btnConectar.Location = new System.Drawing.Point(269, 16);
            this.btnConectar.Name = "btnConectar";
            this.btnConectar.Size = new System.Drawing.Size(75, 23);
            this.btnConectar.TabIndex = 7;
            this.btnConectar.Text = "Conectar";
            this.btnConectar.UseVisualStyleBackColor = true;
            this.btnConectar.Click += new System.EventHandler(this.btnConectar_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.comBox);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.btnConectar);
            this.groupBox2.Location = new System.Drawing.Point(12, 77);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(350, 51);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 9);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(143, 62);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.labelTEjec);
            this.groupBox3.Controls.Add(this.numericTEjecucion);
            this.groupBox3.Controls.Add(this.labelTEjecucion);
            this.groupBox3.Location = new System.Drawing.Point(12, 127);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(350, 51);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            // 
            // labelTEjec
            // 
            this.labelTEjec.AutoSize = true;
            this.labelTEjec.Location = new System.Drawing.Point(276, 22);
            this.labelTEjec.Name = "labelTEjec";
            this.labelTEjec.Size = new System.Drawing.Size(55, 13);
            this.labelTEjec.TabIndex = 9;
            this.labelTEjec.Text = "Segundos";
            // 
            // numericTEjecucion
            // 
            this.numericTEjecucion.Location = new System.Drawing.Point(137, 20);
            this.numericTEjecucion.Maximum = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.numericTEjecucion.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericTEjecucion.Name = "numericTEjecucion";
            this.numericTEjecucion.Size = new System.Drawing.Size(121, 20);
            this.numericTEjecucion.TabIndex = 11;
            this.numericTEjecucion.Tag = "";
            this.numericTEjecucion.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // labelTEjecucion
            // 
            this.labelTEjecucion.AutoSize = true;
            this.labelTEjecucion.Location = new System.Drawing.Point(6, 22);
            this.labelTEjecucion.Name = "labelTEjecucion";
            this.labelTEjecucion.Size = new System.Drawing.Size(110, 13);
            this.labelTEjecucion.TabIndex = 6;
            this.labelTEjecucion.Text = "Tiempo de Ejecución:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1239, 796);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupGrafico);
            this.Controls.Add(this.groupCtrlEntradas);
            this.Controls.Add(this.groupCtrlMaqueta);
            this.Controls.Add(this.title);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.numericVelVent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPotCal)).EndInit();
            this.groupCtrlMaqueta.ResumeLayout(false);
            this.groupCtrlMaqueta.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackPotCal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackVelVent)).EndInit();
            this.groupCtrlEntradas.ResumeLayout(false);
            this.groupCtrlEntradas.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericTFinalCal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericConsignaCal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTInicioCal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTFinalVent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericConsignaVent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTInicioVent)).EndInit();
            this.groupGrafico.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericTEjecucion)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label title;
        private System.Windows.Forms.NumericUpDown numericVelVent;
        private System.Windows.Forms.NumericUpDown numericPotCal;
        private System.Windows.Forms.GroupBox groupCtrlMaqueta;
        private System.Windows.Forms.Label labelVent;
        private System.Windows.Forms.Label titleCal;
        private System.Windows.Forms.Label titleVent;
        private System.Windows.Forms.Label labelCal;
        private System.Windows.Forms.GroupBox groupCtrlEntradas;
        private System.Windows.Forms.GroupBox groupGrafico;
        private System.Windows.Forms.Button buttonEntrEscalon;
        private System.Windows.Forms.Button buttonEntrRampa;
        private LiveCharts.WinForms.CartesianChart tempChart;
        private System.Windows.Forms.Button buttonCargar;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label labelBtnRampa;
        private System.Windows.Forms.Label labelBtnEscalon;
        private System.Windows.Forms.Label labelTInicioVent;
        private System.Windows.Forms.NumericUpDown numericTInicioVent;
        private System.Windows.Forms.Label labelConsignaVent;
        private System.Windows.Forms.NumericUpDown numericConsignaVent;
        private System.Windows.Forms.Label labelTFinalVent;
        private System.Windows.Forms.NumericUpDown numericTFinalVent;
        private System.Windows.Forms.TrackBar trackVelVent;
        private System.Windows.Forms.TrackBar trackPotCal;
        private System.Windows.Forms.ComboBox comBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnConectar;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonEscVent;
        private System.Windows.Forms.Button buttonEscCal;
        private System.Windows.Forms.Label labelTFinalCal;
        private System.Windows.Forms.NumericUpDown numericTFinalCal;
        private System.Windows.Forms.Label labelConsignaCal;
        private System.Windows.Forms.NumericUpDown numericConsignaCal;
        private System.Windows.Forms.Label labelTInicioCal;
        private System.Windows.Forms.NumericUpDown numericTInicioCal;
        private System.Windows.Forms.Button buttonCargarEntradas;
        private LiveCharts.WinForms.CartesianChart entradaChart;
        private System.Windows.Forms.Button buttonOcultar;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label labelTEjecucion;
        private System.Windows.Forms.NumericUpDown numericTEjecucion;
        private System.Windows.Forms.Label labelTEjec;
    }
}

