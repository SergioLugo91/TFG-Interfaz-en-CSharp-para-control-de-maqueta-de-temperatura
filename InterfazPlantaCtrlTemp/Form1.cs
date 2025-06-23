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
        Stopwatch cronometro;
        private System.Windows.Forms.Timer portCheckTimer;

        private ChartValues<double> temperaturas = new ChartValues<double>();
        private ChartValues<double> tiempos = new ChartValues<double>();

        private ChartValues<double> entradas = new ChartValues<double>();

        public Form1()
        {
            InitializeComponent();

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

        // Método para el control directo de la maqueta con el botón Cargar
        private async void BtnCargar_Click(object sender, EventArgs e)
        {
            buttonCargar.Enabled = false; // Deshabilitar botón durante la operación
            entradaChart.Visible = false; // Ocultar el gráfico de entradas
            buttonOcultar.Visible = false; // Ocultar el botón de ocultar el gráfico de entradas

            tempChart.Size = new Size(810, 660); // Ajustar el tamaño del gráfico de temperatura

            try
            {
                // Limpiar gráfico
                tempChart.Series.Clear();
                temperaturas.Clear();
                tiempos.Clear();
                tempChart.Update(true, true);
                Debug.WriteLine("Gráfico actualizado");

                // Configurar el gráfico inicial
                ConfigurarGraficoTempInicial();
                ConfigurarGraficoEntradas();

                EnviarDatos($"v{numericVelVent.Value.ToString()}V");
                EnviarDatos($"n{numericPotCal.Value.ToString()}N");

                cronometro.Restart();

                // Iniciar la recepción de datos
                await Task.Run (() => RecibirDatos(16,(int)numericVelVent.Value));

                Debug.WriteLine("Fin de RecibirDatos");
            }
            finally 
            {
                // Rehabilitar botón al finalizar
                buttonCargar.Enabled = true; 
            }
            
        }

        // Método de enviar datos al arduino
        private void EnviarDatos(string datos)
        {
            PuertoArduino.WriteLine(datos);
            Debug.WriteLine($"Datos enviados: {datos}"); 
        }
        
        // Método para la configuración inicial del gráfico
        private void ConfigurarGraficoTempInicial()
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

        // Método para configurar el gráfico de entradas
        private void ConfigurarGraficoEntradas()
        {
            // Crear un pincel con opacidad personalizada para el Fill
            var fillBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            fillBrush.Opacity = 0.3;

            entradaChart.Series = new SeriesCollection
           {
               new LineSeries
               {
                   Title = "Entrada",
                   Values = entradas,
                   Fill = fillBrush,
                   Stroke = System.Windows.Media.Brushes.Orange,
                   PointGeometry = DefaultGeometries.Circle,
                   PointGeometrySize = 8,
                   StrokeThickness = 2  // Añadido para mejor visibilidad
               }
           };

            entradaChart.AxisX.Clear();
            entradaChart.AxisX.Add(new Axis
            {
                Title = "Tiempo (s)",
                LabelFormatter = value => value.ToString("N1") + "s",
                MinValue = 0,
                MaxValue = 11 // Establecer el máximo inicial según los puntos esperados
            });

            entradaChart.AxisY.Clear();
            entradaChart.AxisY.Add(new Axis
            {
                Title = "Entrada (%)",
                LabelFormatter = value => value.ToString("N1") + "%",
                MinValue = 0,  // Valor inicial mínimo
                MaxValue = 50  // Valor inicial máximo
            });
        }

        // Método de recibir datos del arduino
        private void RecibirDatos(int graph, int entrada)
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

                    // Añadir valores al gráfico (Actualizar UI)
                    tempChart.Invoke((MethodInvoker)delegate
                    {
                        // Añadir los valores a las series
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
                    });

                    // Añadir valores al gráfico de entradas (Actualizar UI) (Para el modo de entradas del sistema)
                    entradaChart.Invoke((MethodInvoker)delegate
                    {
                        // Añadir los valores a las series
                        entradas.Add(entrada);
                        tiempos.Add(tiempoActual);

                        // Ajustar eje X dinámicamente
                        entradaChart.AxisX[0].MaxValue = Math.Max(tiempoActual + 1, graph);
                        if (temperaturas.Count > 0)
                        {
                            double margen = Math.Max(5, entradas.Max() * 0.1); // 10% de margen o 5 unidades
                            entradaChart.AxisY[0].MinValue = Math.Floor(entradas.Min() - margen);
                            entradaChart.AxisY[0].MaxValue = Math.Ceiling(entradas.Max() + margen);
                        }
                    });

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

        // Método para el control a través de entradas del sistema con el botón Cargar Entradas
        private async void buttonCargarEntradas_Click(object sender, EventArgs e)
        {
            buttonCargarEntradas.Enabled = false; // Deshabilitar botón durante la operación
            entradaChart.Visible = true; // Mostrar el gráfico de entradas
            buttonOcultar.Visible = true; // Mostrar el botón de ocultar el gráfico de entradas

            tempChart.Size = new Size(810, 330); // Ajustar el tamaño del gráfico de temperatura

            try
            {
                cronometro.Restart();

                // Limpiar gráfico de temperatura
                tempChart.Series.Clear();
                temperaturas.Clear();
                tiempos.Clear();
                tempChart.Update(true, true);
                Debug.WriteLine("Gráfico actualizado");
                // Limpiar gráfico de entradas
                entradaChart.Series.Clear();
                entradas.Clear();
                tiempos.Clear();
                entradaChart.Update(true, true);
                Debug.WriteLine("Gráfico actualizado");

                // Configurar el gráfico inicial
                ConfigurarGraficoTempInicial();
                ConfigurarGraficoEntradas();

                if (buttonEntrEscalon.Enabled == false) // Se ha seleccionado la entrada escalón
                {
                    Debug.WriteLine("Se ha seleccionado la entrada escalón");

                    if (buttonEscVent.Enabled == false) // Entrada escalón para el ventilador con el calefactor fijo en 40%
                    {
                        Debug.WriteLine("Entrada escalón para el Ventilador");
                        // Valores iniciales antes de la entrada escalón
                        EnviarDatos("v0V");
                        EnviarDatos("n40N");
                        // Iniciar la recepción de datos
                        int valorPreEsc = (int)numericTInicioVent.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos(valorPreEsc, 0)); 
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        EnviarDatos($"v{numericConsignaVent.Value.ToString()}V");
                        int valorPostEsc = 21 - (int)numericTInicioVent.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos(valorPostEsc, (int)numericConsignaVent.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (buttonEscCal.Enabled == false) // Entrada escalón para el calefactor con el ventilador fijo en 60%
                    {
                        Debug.WriteLine("Entrada escalón para el Calefactor");
                        // Valores iniciales antes de la entrada escalón
                        EnviarDatos("v60V");
                        EnviarDatos("n0N");
                        // Iniciar la recepción de datos
                        int valorPreEsc = (int)numericTInicioCal.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos(valorPreEsc, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        EnviarDatos($"n{numericConsignaCal.Value.ToString()}N");
                        // Iniciar la recepción de datos
                        int valorPostEsc = 21 - (int)numericTInicioCal.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos(valorPostEsc, (int)numericConsignaCal.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else
                    {
                        MessageBox.Show("Seleccione a qué componente aplicarle la entrada escalón");
                    }
                }
                else if (buttonEntrRampa.Enabled == false) // Se ha seleccionado la entrada rampa
                {
                    Debug.WriteLine("Se ha seleccionado la entrada rampa");

                    if (buttonEscVent.Enabled == false) // Entrada rampa para el ventilador con el calefactor fijo en 40%
                    {
                        Debug.WriteLine("Entrada rampa para el Ventilador");
                        // Valores iniciales antes de la entrada rampa
                        EnviarDatos("v0V");
                        EnviarDatos("n40N");
                        // Iniciar la recepción de datos
                        int valorPreRamp = (int)numericTInicioVent.Value;
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");
                        await Task.Run(() => RecibirDatos(valorPreRamp, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericTFinalVent.Value - (int)numericTInicioVent.Value; i++)
                        {
                            int valorMidRamp = i * (int)(numericConsignaVent.Value / ((int)numericTFinalVent.Value - (int)numericTInicioVent.Value));
                            EnviarDatos($"v{valorMidRamp.ToString()}V");
                            await Task.Run(() => RecibirDatos(1, valorMidRamp));
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        EnviarDatos($"v{numericConsignaVent.Value.ToString()}V");
                        // Iniciar la recepción de datos
                        int valorPostRamp = 21 - (int)numericTFinalVent.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");
                        await Task.Run(() => RecibirDatos(valorPostRamp, (int)numericConsignaVent.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (buttonEscCal.Enabled == false) // Entrada rampa para el calefactor con el ventilador fijo en 60%
                    {
                        Debug.WriteLine("Entrada rampa para el Calefactor");
                        // Valores iniciales antes de la entrada rampa
                        EnviarDatos("v60V");
                        EnviarDatos("n0N");
                        // Iniciar la recepción de datos
                        int valorPreRamp = (int)numericTInicioCal.Value;
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");
                        await Task.Run(() => RecibirDatos(valorPreRamp, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericTFinalCal.Value - (int)numericTInicioCal.Value; i++)
                        {
                            int valorMidRamp = i * (int)(numericConsignaCal.Value / ((int)numericTFinalCal.Value - (int)numericTInicioCal.Value));
                            EnviarDatos($"n{valorMidRamp.ToString()}N");
                            await Task.Run(() => RecibirDatos(1, valorMidRamp));
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        EnviarDatos($"n{numericConsignaCal.Value.ToString()}N");
                        // Iniciar la recepción de datos
                        int valorPostRamp = 21 - (int)numericTFinalCal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");
                        await Task.Run(() => RecibirDatos(valorPostRamp, (int)numericConsignaCal.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else
                    {
                        MessageBox.Show("Seleccione a qué componente aplicarle la entrada rampa");
                    }
                }
                else
                {
                    MessageBox.Show("Seleccione un tipo de entrada");
                }
            }
            finally
            {
                // Rehabilitar botón al finalizar
                buttonCargarEntradas.Enabled = true;
            }
        }

        // Método para ocultar o mostrar el gráfico de entradas
        private void buttonOcultar_Click(object sender, EventArgs e)
        {
            if (entradaChart.Visible == true)
            {
                entradaChart.Visible = false; // Ocultar el gráfico de entradas
                tempChart.Size = new Size(810, 660); // Ajustar el tamaño del gráfico de temperatura
            }
            else
            {
                entradaChart.Visible = true; // Ocultar el gráfico de entradas
                tempChart.Size = new Size(810, 330); // Ajustar el tamaño del gráfico de temperatura
            }
        }
    }
}
