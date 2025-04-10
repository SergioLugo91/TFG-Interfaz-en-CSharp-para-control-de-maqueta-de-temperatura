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

namespace InterfazPlantaCtrlTemp
{
    public partial class Form1 : Form
    {
        System.IO.Ports.SerialPort PuertoArduino;
        List<datoEntrada> datosEntrada;
        Stopwatch cronometro;
        LiveCharts.Wpf.LineSeries lineaSeries;

        public Form1()
        {
            InitializeComponent();

            // Inicializar la lista de datos
            datosEntrada = new List<datoEntrada>();

            // Inicializar el cronómetro
            cronometro = new Stopwatch();

            // Inicializar la serie de datos
            lineaSeries = new LiveCharts.Wpf.LineSeries
            {
                Title = "Datos",
                Values = new LiveCharts.ChartValues<ObservablePoint>()
            };

            // Asociar el evento .ValueChanged con el método _ValueChanged
            numericVelVent.ValueChanged += numericVelVent_ValueChanged;
            numericPotCal.ValueChanged += numericPotCal_ValueChanged;
            trackVelVent.ValueChanged += trackVelVent_ValueChanged;
            trackPotCal.ValueChanged += trackPotCal_ValueChanged;

            numericTFinalCal.ValueChanged += numericTFinalCal_ValueChanged;
            numericTFinalVent.ValueChanged += numericTFinalVent_ValueChanged;
            numericTInicioCal.ValueChanged += numericTInicioCal_ValueChanged;
            numericTInicioVent.ValueChanged += numericTInicioVent_ValueChanged;

            // Configurar el gráfico
            tempChart.AxisX.Add(new LiveCharts.Wpf.Axis
            { 
                Title = "Tiempo",
                LabelFormatter = value => value.ToString("0") + "s" 
            });

            tempChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Temperatura",
                LabelFormatter = value => value.ToString("0") + "°C"
            });

            tempChart.Series = new LiveCharts.SeriesCollection { lineaSeries };

            // Asociar el evento FormClosing con el método Form1_FormClosing
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            // Asociar el evento Load con el método Form1_Load
            this.Load += new EventHandler(Form1_Load);
        }

        // Método para realizar las acciones de inicio y carga de la interfaz
        private void Form1_Load(object sender, EventArgs e)
        {
            // Búsqueda de puertos seriales disponibles
            Debug.WriteLine("Form1_Load ejecutándose...");
            var portNames = SerialPort.GetPortNames();
            Debug.WriteLine($"Puertos detectados: {string.Join(", ", portNames)}");

            foreach (string s in SerialPort.GetPortNames())
            {
                comBox.Items.Add(s);
            }
            if (comBox.Items.Count > 0)
                comBox.SelectedIndex = comBox.Items.Count - 1;
            else
                comBox.SelectedIndex = -1;

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
        // Método para cerrar el puerto serial
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //cerrar puerto
            if (PuertoArduino.IsOpen) PuertoArduino.Close();
            Debug.WriteLine("Puerto Cerrado");
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
            EnviarDatos($"v{numericVelVent.Value.ToString()}V");
            EnviarDatos($"n{numericPotCal.Value.ToString()}N");

            cronometro.Restart();

            tempChart.Series.Clear();

            RecibirDatos(100);
        }

        private void EnviarDatos(string datos)
        {
            PuertoArduino.WriteLine(datos);
            Debug.WriteLine($"Datos enviados: {datos}"); 
        }

        private void RecibirDatos(int graph)
        {
            for (int i = 0; i < graph; i++)
            {
                Debug.WriteLine($"Leyendo dato: {i}");
                PuertoArduino.ReadTimeout = 200;
                try 
                {
                    string lectura = PuertoArduino.ReadLine();
                    Debug.WriteLine($"Datos recibidos: {lectura}");
                    lectura = lectura.Substring(1, 4);
                    double dato = double.Parse(lectura);
                    double tiempo = cronometro.Elapsed.TotalSeconds;
                    datosEntrada.Add(new datoEntrada(tiempo,dato));
                    ActualizarGrafica();
                    Thread.Sleep(100);
                }
                catch (TimeoutException) { Debug.WriteLine("Timeout"); }
            }
        }

        private void ActualizarGrafica()
        {
            foreach (var dato in datosEntrada)
            {
                lineaSeries.Values.Add(new ObservablePoint(dato.Tiempo, dato.Dato));
                Debug.WriteLine($"Datos graficados: Tiempo={dato.Tiempo}, Dato={dato.Dato}");
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
