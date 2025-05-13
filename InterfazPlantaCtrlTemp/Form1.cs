using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO.Ports;
using System.Globalization;
using LiveCharts;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace InterfazPlantaCtrlTemp
{
    public partial class Form1 : Form
    {
        System.IO.Ports.SerialPort PuertoArduino;
        List<double> datosEntrada;
        Stopwatch cronometro;
        private System.Windows.Forms.Timer portCheckTimer;

        private ChartValues<double> temperaturas = new ChartValues<double>();
        private ChartValues<double> tiempos = new ChartValues<double>();

        public Form1()
        {
            InitializeComponent();

            // Inicializar la lista de datos
            datosEntrada = new List<double>();

            // Inicializar el cronómetro
            cronometro = new Stopwatch();

            // Asociar el evento .ValueChanged con el método _ValueChanged
            numericVelVent.ValueChanged += numericVelVent_ValueChanged;
            numericPotCal.ValueChanged += numericPotCal_ValueChanged;
            trackVelVent.ValueChanged += trackVelVent_ValueChanged;
            trackPotCal.ValueChanged += trackPotCal_ValueChanged;

            numericTFinalCal.ValueChanged += numericTFinalCal_ValueChanged;
            numericTFinalVent.ValueChanged += numericTFinalVent_ValueChanged;
            numericTInicioCal.ValueChanged += numericTInicioCal_ValueChanged;
            numericTInicioVent.ValueChanged += numericTInicioVent_ValueChanged;

            // Asociar el evento FormClosing con el método Form1_FormClosing
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            // Asociar el evento Load con el método Form1_Load
            this.Load += new EventHandler(Form1_Load);
        }

        // Método para realizar las acciones de inicio y carga de la interfaz
        private void Form1_Load(object sender, EventArgs e)
        {
            // Configurar el timer para buscar puertos
            portCheckTimer = new System.Windows.Forms.Timer();
            portCheckTimer.Interval = 1000; // Verificar cada segundo
            portCheckTimer.Tick += CheckSerialPorts;
            portCheckTimer.Start();

            // Ejecutar la primera verificación inmediatamente
            CheckSerialPorts(null, EventArgs.Empty);

            // Set up de los botones de la interfaz
            buttonCargar.Enabled = false;
            buttonCargarEntradas.Enabled = false;

            // Set up de los botones del control de entradas
            buttonEscCal.Enabled = false;
            buttonEscVent.Enabled = false;

            numericConsignaCal.Enabled = false;
            numericConsignaCal.Value = 40;
            numericConsignaVent.Enabled = false;
            numericConsignaVent.Value = 60;

            numericTInicioVent.Enabled = false;
            numericTFinalVent.Enabled = false;

            numericTInicioCal.Enabled = false;
            numericTFinalCal.Enabled = false;
        }

        // Método para verificar los puertos seriales disponibles
        private void CheckSerialPorts(object sender, EventArgs e)
        {
            // Limpiar los puertos existentes en el ComboBox
            comBox.Items.Clear();

            // Obtener los puertos disponibles
            var portNames = SerialPort.GetPortNames();
            Debug.WriteLine($"Puertos detectados: {string.Join(", ", portNames)}");

            // Añadir los puertos encontrados al ComboBox
            foreach (string s in portNames)
            {
                comBox.Items.Add(s);
            }

            // Seleccionar el último puerto si hay alguno
            if (comBox.Items.Count > 0)
            {
                comBox.SelectedIndex = comBox.Items.Count - 1;

                // Detener el timer si lo encontramos
                if (sender is System.Windows.Forms.Timer timer)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            }
            else
            {
                comBox.SelectedIndex = -1;
                Debug.WriteLine("No se encontraron puertos seriales. Volviendo a intentar...");
            }
        }

        // Método para cerrar el puerto serial
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //cerrar puerto
            if (PuertoArduino != null && PuertoArduino.IsOpen)
            {
                try { PuertoArduino.Close(); }
                catch (Exception ex) { Debug.WriteLine($"Error al cerrar el puerto: {ex.Message}"); }
                Debug.WriteLine("Puerto Cerrado");
            }
        }

        // Método para conectar el puerto serial con el botón conectar
        private void btnConectar_Click(object sender, EventArgs e)
        {
            //crear Puerto Serial
            PuertoArduino = new System.IO.Ports.SerialPort();
            PuertoArduino.PortName = comBox.SelectedItem.ToString();
            PuertoArduino.BaudRate = 115200;
            PuertoArduino.DtrEnable = true;
            PuertoArduino.Open();
            PuertoArduino.ReadExisting();
            if (PuertoArduino.IsOpen)
            {
                Debug.WriteLine("Puerto Abierto");
            }

            // ajuste de los botones después de conectar el puerto serial
            comBox.Enabled = false;
            btnConectar.Enabled = false;
            buttonCargar.Enabled = true;
            buttonCargarEntradas.Enabled = true;
        }

        // Métodos para sincronizar la barra con el numericUpDown
        private void numericVelVent_ValueChanged (object sender, EventArgs e)
        {
            // Sincronización entre la barra y el numericUpDown
            trackVelVent.Value = Convert.ToInt32(numericVelVent.Value);
        }
        private void trackVelVent_ValueChanged(object sender, EventArgs e)
        {
            // Sincronización entre la barra y el numericUpDown
            numericVelVent.Value = trackVelVent.Value;
        }
        private void numericPotCal_ValueChanged(object sender, EventArgs e)
        {
            // Sincronización entre la barra y el numericUpDown
            trackPotCal.Value = Convert.ToInt32(numericPotCal.Value);
        }
        private void trackPotCal_ValueChanged(object sender, EventArgs e)
        {
            // Sincronización entre la barra y el numericUpDown
            numericPotCal.Value = trackPotCal.Value;
        }

        private void BtnCargar_Click(object sender, EventArgs e)
        {
            datosEntrada.Clear();
            tempChart.Series.Clear();
            temperaturas.Clear();
            tiempos.Clear();

            // Forzar una actualización del gráfico para limpiar visualmente
            tempChart.Update(true, true);

            // Configurar el gráfico inicial
            ConfigurarGraficoInicial();

            EnviarDatos($"v{numericVelVent.Value.ToString()}V");
            EnviarDatos($"n{numericPotCal.Value.ToString()}N");

            cronometro.Restart();

            // Iniciar la recepción de datos
            Task.Factory.StartNew(() => RecibirDatos(11),
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void EnviarDatos(string datos)
        {
            PuertoArduino.WriteLine(datos);
            Debug.WriteLine($"Datos enviados: {datos}"); 
        }
        
        private void ConfigurarGraficoInicial()
        {
            tempChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperatura",
                    Values = temperaturas,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    StrokeThickness = 2  // Añadido para mejor visibilidad
                }
            };

            tempChart.AxisX.Clear();
            tempChart.AxisX.Add(new Axis
            {
                Title = "Tiempo (s)",
                LabelFormatter = value => value.ToString("N1") + "s",
                MinValue = 0,
                MaxValue = 11 // Establecer el máximo inicial según los puntos esperados
            });

            tempChart.AxisY.Clear();
            tempChart.AxisY.Add(new Axis
            {
                Title = "Temperatura (°C)",
                LabelFormatter = value => value.ToString("N1") + "°C",
                MinValue = 0,  // Valor inicial mínimo
                MaxValue = 50  // Valor inicial máximo
            });
        }

        private void RecibirDatos(int graph)
        {
            for (int i = 0; i < graph; i++)
            {
                Debug.WriteLine($"Leyendo dato: {i}");
                PuertoArduino.WriteLine($"t{i}T");
                PuertoArduino.ReadTimeout = 200;
                try 
                {
                    string lectura = PuertoArduino.ReadLine().Trim();
                    Debug.WriteLine($"Datos recibidos: {lectura}");

                    // Extraer y parsear el dato de temperatura
                    lectura = lectura.Substring(1, 5);
                    double temperatura = double.Parse(lectura, CultureInfo.InvariantCulture);
                    double tiempoActual = cronometro.Elapsed.TotalSeconds;

                    Debug.WriteLine($"Tiempo: {tiempoActual}s, Temperatura: {temperatura}°C");

                    // Actualizar la interfaz desde el hilo principal
                    tempChart.Invoke((MethodInvoker)delegate
                    {
                        temperaturas.Add(temperatura);
                        tiempos.Add(tiempoActual);

                        // Ajustar eje X dinámicamente
                        tempChart.AxisX[0].MaxValue = Math.Max(tiempoActual + 1, graph);

                        if (temperaturas.Count > 0)
                        {
                            double margen = Math.Max(5, temperaturas.Max() * 0.1); // 10% de margen o 5 unidades
                            tempChart.AxisY[0].MinValue = Math.Floor(temperaturas.Min() - margen);
                            tempChart.AxisY[0].MaxValue = Math.Ceiling(temperaturas.Max() + margen);
                        }

                        tempChart.Update(true, true); // Forzar actualización
                    });

                    // Actualizar gráfico
                    tempChart.Update(true, true);

                    // Esperar un segundo antes de la siguiente lectura
                    Thread.Sleep(1000);
                }
                catch (TimeoutException) { Debug.WriteLine("Timeout"); }
            }
        }

        // Control de Entradas del Sistema
        // Metodo de ocultar y mostrar botones relacionados con la entrada escalon
        private void buttonEntrEscalon_Click(object sender, EventArgs e)
        {
            buttonEntrEscalon.Enabled = false;
            buttonEntrRampa.Enabled = true;

            buttonEscCal.Enabled = true;
            buttonEscVent.Enabled = true;

            numericConsignaCal.Enabled = false;
            numericConsignaVent.Enabled = false;

            numericTInicioVent.Enabled = false;
            numericTFinalVent.Enabled = false;

            numericTInicioCal.Enabled = false;
            numericTFinalCal.Enabled = false;
        }

        // Metodo de ocultar y mostrar botones relacionados con la entrada rampa
        private void buttonEntrRampa_Click(object sender, EventArgs e)
        {
            buttonEntrEscalon.Enabled = true;
            buttonEntrRampa.Enabled = false;

            buttonEscCal.Enabled = true;
            buttonEscVent.Enabled = true;

            numericConsignaCal.Enabled = false;
            numericConsignaVent.Enabled = false;

            numericTInicioVent.Enabled = false;
            numericTFinalVent.Enabled = false;

            numericTInicioCal.Enabled = false;
            numericTFinalCal.Enabled = false;
        }

        // Metodo de ocultar y mostrar botones relacionados con el ventilador dentro del control de entradas
        private void buttonEscVent_Click(object sender, EventArgs e)
        {
            buttonEscVent.Enabled = false;
            buttonEscCal.Enabled = true;

            numericConsignaVent.Enabled = true;
            numericConsignaCal.Enabled = false;
            numericConsignaCal.Value = 40;

            numericTInicioVent.Enabled = true;
            numericTInicioCal.Enabled = false;
            numericTInicioCal.Value = 0;

            if (buttonEntrRampa.Enabled == false)
            {
                numericTFinalVent.Enabled = true;
                numericTFinalCal.Enabled = false;
                numericTFinalCal.Value = 1;
            }
            else
            {
                numericTFinalVent.Enabled = false;
                numericTFinalCal.Enabled = false;
            }
        }

        // Metodo de ocultar y mostrar botones relacionados con el calefactor dentro del control de entradas
        private void buttonEscCal_Click(object sender, EventArgs e)
        {
            buttonEscCal.Enabled = false;
            buttonEscVent.Enabled = true;

            numericConsignaCal.Enabled = true;
            numericConsignaVent.Enabled = false;
            numericConsignaVent.Value = 60;

            numericTInicioCal.Enabled = true;
            numericTInicioVent.Enabled = false;
            numericTInicioVent.Value = 0;

            if (buttonEntrRampa.Enabled == false)
            {
                numericTFinalVent.Enabled = false;
                numericTFinalCal.Enabled = true;
                numericTFinalVent.Value = 1;
            }
            else
            {
                numericTFinalVent.Enabled = false;
                numericTFinalCal.Enabled = false;
            }
        }

        // Metodos para asegurar que el valor de TFinal sea mayor que el de TInicio
        private void numericTFinalVent_ValueChanged(object sender, EventArgs e)
        {
            if (numericTFinalVent.Value <= numericTInicioVent.Value)
            {
                numericTFinalVent.Value = numericTInicioVent.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }
        private void numericTFinalCal_ValueChanged(object sender, EventArgs e)
        {
            if (numericTFinalCal.Value <= numericTInicioCal.Value)
            {
                numericTFinalCal.Value = numericTInicioCal.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }
        private void numericTInicioCal_ValueChanged(object sender, EventArgs e)
        {
            if (numericTInicioCal.Value >= numericTFinalCal.Value)
            {
                numericTFinalCal.Value = numericTInicioCal.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }
        private void numericTInicioVent_ValueChanged(object sender, EventArgs e)
        {
            if (numericTInicioVent.Value >= numericTFinalCal.Value)
            {
                numericTFinalCal.Value = numericTInicioCal.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }


        private void buttonCargarEntradas_Click(object sender, EventArgs e)
        {
            datosEntrada.Clear();
            cronometro.Restart();

            if (buttonEntrEscalon.Enabled == false)
            {

            }
            else if (buttonEntrRampa.Enabled == false)
            {

            }
            else
            {
                MessageBox.Show("Seleccione un tipo de entrada");
            }
        }
    }
}
