using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;

namespace InterfazPlantaCtrlTemp
{
    public partial class Form1 : Form
    {
        // Declaración de variables globales
        System.IO.Ports.SerialPort PuertoArduino;
        Stopwatch cronometro;
        private System.Windows.Forms.Timer portCheckTimer;

        private ChartValues<double> temperaturas = new ChartValues<double>();
        private ChartValues<double> tiempos = new ChartValues<double>();

        private ChartValues<double> entradas = new ChartValues<double>();
        private ChartValues<double> entradaCal = new ChartValues<double>();
        private ChartValues<double> entradaVent = new ChartValues<double>();

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

            numericRampVentTInicio.ValueChanged += numericRampVentTInicio_ValueChanged;
            numericRampVentTFinal.ValueChanged += numericRampVentTFinal_ValueChanged;
            numericRampCalTInicio.ValueChanged += numericRampCalTInicio_ValueChanged;
            numericRampCalTFinal.ValueChanged += numericRampCalTFinal_ValueChanged;

            // Asociar el evento .CheckedChanged con el método _CheckedChanged
            checkEscalon.CheckedChanged += checkEscalon_CheckedChanged;
            checkRampa.CheckedChanged += checkRampa_CheckedChanged;

            // Asociar el evento FormClosing con el método Form1_FormClosing
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            // Asociar el evento Load con el método Form1_Load
            this.Load += new EventHandler(Form1_Load);

            numericTEjecucion.ValueChanged += numericTEjecucion_ValueChanged;
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
            // Cerrar puerto
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
            // Crear Puerto Serial
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

            // Ajuste de los botones después de conectar el puerto serial
            comBox.Enabled = false;
            btnConectar.Enabled = false;
            buttonCargar.Enabled = true;
            buttonCargarEntradas.Enabled = true;
        }

        // Método para actualizar el valor máximo de Tfinal para el Ventilador y el Calefactor al cambiar el Tiempo de Ejecución
        private void numericTEjecucion_ValueChanged(object sender, EventArgs e)
        {
            // Actualizar el valor máximo de TFinal para el Ventilador
            numericRampVentTFinal.Maximum = numericTEjecucion.Value + 1;
            numericRampVentTInicio.Maximum = numericTEjecucion.Value;
            numericEscVentTInicio.Maximum = numericTEjecucion.Value;
            // Actualizar el valor máximo de TFinal para el Calefactor
            numericRampCalTFinal.Maximum = numericTEjecucion.Value + 1;
            numericRampCalTInicio.Maximum = numericTEjecucion.Value;
            numericEscCalTInicio.Maximum = numericTEjecucion.Value;
        }

        // Métodos para sincronizar la barra con el numericUpDown
        private void numericVelVent_ValueChanged(object sender, EventArgs e)
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
                MaxValue = 30 // Establecer el máximo inicial según los puntos esperados
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
                MaxValue = 30 // Establecer el máximo inicial según los puntos esperados
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

        // Método de enviar datos al arduino
        private void EnviarDatos(string datos)
        {
            PuertoArduino.WriteLine(datos);
            Debug.WriteLine($"Datos enviados: {datos}");
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

                    // Añadir valores al gráfico de temperatura (Actualizar UI)
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
                        if (entradas.Count > 0)
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

        // Método de recibir datos para dos entradas de control automático
        private void RecibirDatos2(int graph, int entradavent, int entradacal)
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


                    // Añadir valores al gráfico de temperatura (Actualizar UI)
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

                    // Ajuste del gráfico de entradas para reflejar ambas entradas
                    var fillBrushVent = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                    fillBrushVent.Opacity = 0.3;
                    fillBrushVent.Freeze();

                    var fillBrushCal = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    fillBrushCal.Opacity = 0.3;
                    fillBrushCal.Freeze();

                    if (i == 0) // Solo configurar las series la primera vez
                    {
                        entradaChart.Invoke((MethodInvoker)delegate
                        {
                            entradaChart.Series.Clear();
                            entradaChart.Series = new SeriesCollection
                            {
                                new LineSeries
                                {
                                    Title = "Entrada Ventilador",
                                    Values = entradaVent,
                                    Fill = fillBrushVent,
                                    Stroke = System.Windows.Media.Brushes.Blue,
                                    PointGeometry = DefaultGeometries.Square,
                                    PointGeometrySize = 8,
                                    StrokeThickness = 2  // Añadido para mejor visibilidad
                                },

                                new LineSeries
                                {
                                    Title = "Entrada Calefactor",
                                    Values = entradaCal,
                                    Fill = fillBrushCal,
                                    Stroke = System.Windows.Media.Brushes.Orange,
                                    PointGeometry = DefaultGeometries.Circle,
                                    PointGeometrySize = 8,
                                    StrokeThickness = 2  // Añadido para mejor visibilidad
                                }
                            };
                        });
                    }

                    // Añadir valores al gráfico de entradas (Actualizar UI) (Para el modo de entradas del sistema)
                    entradaChart.Invoke((MethodInvoker)delegate
                    {
                        // Añadir los valores a las series
                        entradaVent.Add(entradavent);
                        entradaCal.Add(entradacal);
                        tiempos.Add(tiempoActual);

                        // Ajustar eje X dinámicamente
                        entradaChart.AxisX[0].MaxValue = Math.Max(tiempoActual + 1, graph);
                        if (entradaVent.Count > 0 || entradaCal.Count > 0)
                        {
                            double margen = Math.Max(5, Math.Max(entradaVent.Max(), entradaCal.Max()) * 0.1); // 10% de margen o 5 unidades
                            entradaChart.AxisY[0].MinValue = Math.Floor(Math.Min(entradaVent.Min(), entradaCal.Min()) - margen);
                            entradaChart.AxisY[0].MaxValue = Math.Ceiling(Math.Max(entradaVent.Max(), entradaCal.Max()) + margen);
                        }
                    });

                    // Esperar un segundo antes de la siguiente lectura
                    Thread.Sleep(1000);
                }
                catch (TimeoutException) { Debug.WriteLine("Timeout"); }
            }
        }

        // Método para el control manual de la maqueta con el botón Cargar
        private async void BtnCargar_Click(object sender, EventArgs e)
        {
            buttonCargar.Enabled = false; // Deshabilitar botón durante la operación
            buttonCargarEntradas.Enabled = false; // Deshabilitar el botón de cargar entradas durante la operación
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
                await Task.Run(() => RecibirDatos((int)numericTEjecucion.Value + 1, (int)numericVelVent.Value));

                Debug.WriteLine("Fin de RecibirDatos");
            }
            finally
            {
                // Rehabilitar botón al finalizar
                buttonCargar.Enabled = true;
            }

        }

        // Control de Entradas del Sistema
        // Metodos para asegurar que el valor de TFinal sea mayor que el de TInicio
        private void numericRampVentTInicio_ValueChanged(object sender, EventArgs e)
        {
            if (numericRampVentTFinal.Value <= numericRampVentTInicio.Value)
            {
                numericRampVentTFinal.Value = numericRampVentTInicio.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }
        private void numericRampVentTFinal_ValueChanged(object sender, EventArgs e)
        {
            if (numericRampVentTFinal.Value <= numericRampVentTInicio.Value)
            {
                numericRampVentTFinal.Value = numericRampVentTInicio.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }
        private void numericRampCalTInicio_ValueChanged(object sender, EventArgs e)
        {
            if (numericRampCalTFinal.Value <= numericRampCalTInicio.Value)
            {
                numericRampCalTFinal.Value = numericRampCalTInicio.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }
        private void numericRampCalTFinal_ValueChanged(object sender, EventArgs e)
        {
            if (numericRampCalTFinal.Value <= numericRampCalTInicio.Value)
            {
                numericRampCalTFinal.Value = numericRampCalTInicio.Value + 1;
                MessageBox.Show("El tiempo final debe ser mayor al tiempo inicial");
            }
        }

        // Método para comprobar que solo una entrada de tipo escalón o rampa esté seleccionada
        private void checkEscalon_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEscalon.Checked)
            {
                checkRampa.Checked = false; // Desmarcar la entrada rampa si se selecciona escalón

                // Habilitar los controles de entrada escalón
                numericEscVentConsg.Enabled = true;
                numericEscVentTInicio.Enabled = true;
                numericEscCalConsg.Enabled = true;
                numericEscCalTInicio.Enabled = true;

                // Deshabilitar los controles de entrada rampa
                numericRampVentConsg.Enabled = false;
                numericRampVentTInicio.Enabled = false;
                numericRampVentTFinal.Enabled = false;
                numericRampCalConsg.Enabled = false;
                numericRampCalTInicio.Enabled = false;
                numericRampCalTFinal.Enabled = false;
            }
            else
            {
                // Deshabilitar los controles de entrada escalón
                numericEscVentConsg.Enabled = false;
                numericEscVentTInicio.Enabled = false;
                numericEscCalConsg.Enabled = false;
                numericEscCalTInicio.Enabled = false;
            }
        }

        private void checkRampa_CheckedChanged(object sender, EventArgs e)
        {
            if (checkRampa.Checked)
            {
                checkEscalon.Checked = false; // Desmarcar la entrada escalón si se selecciona rampa

                // Habilitar los controles de entrada rampa
                numericRampVentConsg.Enabled = true;
                numericRampVentTInicio.Enabled = true;
                numericRampVentTFinal.Enabled = true;
                numericRampCalConsg.Enabled = true;
                numericRampCalTInicio.Enabled = true;
                numericRampCalTFinal.Enabled = true;

                // Deshabilitar los controles de entrada escalón
                numericEscVentConsg.Enabled = false;
                numericEscVentTInicio.Enabled = false;
                numericEscCalConsg.Enabled = false;
                numericEscCalTInicio.Enabled = false;
            }
            else
            {
                // Deshabilitar los controles de entrada rampa
                numericRampVentConsg.Enabled = false;
                numericRampVentTInicio.Enabled = false;
                numericRampVentTFinal.Enabled = false;
                numericRampCalConsg.Enabled = false;
                numericRampCalTInicio.Enabled = false;
                numericRampCalTFinal.Enabled = false;
            }
        }

        // Método para el control a través de entradas del sistema con el botón Cargar Entradas
        private async void buttonCargarEntradas_Click(object sender, EventArgs e)
        {
            buttonCargarEntradas.Enabled = false; // Deshabilitar botón durante la operación
            buttonCargar.Enabled = false; // Deshabilitar el botón de cargar durante la operación
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
                Debug.WriteLine("Gráfico de temperatura actualizado");
                // Limpiar gráfico de entradas
                entradaChart.Series.Clear();
                entradas.Clear();
                tiempos.Clear();
                entradaChart.Update(true, true);
                Debug.WriteLine("Gráfico de entradas actualizado");

                // Configurar el gráfico inicial
                ConfigurarGraficoTempInicial();
                ConfigurarGraficoEntradas();

                if (checkEscalon.Checked) // Se ha seleccionado la entrada escalón
                {
                    Debug.WriteLine("Se ha seleccionado la entrada escalón");

                    if (checkEscVent.Checked && !checkEscCal.Checked) // Solo entrada escalón para el ventilador
                    {
                        Debug.WriteLine("Entrada escalón para el Ventilador");
                        // Valores iniciales antes de la entrada escalón
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreEsc = (int)numericEscVentTInicio.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos(valorPreEsc, 40));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                        int valorPostEsc = (int)numericTEjecucion.Value + 1 - (int)numericEscVentTInicio.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos(valorPostEsc, (int)numericEscVentConsg.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (!checkEscVent.Checked && checkEscCal.Checked) // Solo entrada escalón para el calefactor
                    {
                        Debug.WriteLine("Entrada escalón para el Calefactor");
                        // Valores iniciales antes de la entrada escalón
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreEsc = (int)numericEscCalTInicio.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos(valorPreEsc, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");

                        // Iniciar la recepción de datos
                        int valorPostEsc = (int)numericTEjecucion.Value + 1 - (int)numericEscCalTInicio.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos(valorPostEsc, (int)numericEscCalConsg.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (checkEscVent.Checked && checkEscCal.Checked) // Entradas escalón para ambos componentes
                    {
                        Debug.WriteLine("Entrada escalón para ambos componentes");
                        // Valores iniciales antes de la entrada escalón
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreEsc = Math.Min((int)numericEscVentTInicio.Value, (int)numericEscCalTInicio.Value);
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos2(valorPreEsc, 40, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Enviar el primer valor de escalón
                        if ((int)numericEscVentTInicio.Value < (int)numericEscCalTInicio.Value) // Se envía primero el del ventilador
                        {
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                            int valorFirstEsc = (int)numericEscCalTInicio.Value - (int)numericEscVentTInicio.Value;
                            await Task.Run(() => RecibirDatos2(valorFirstEsc, (int)numericEscVentConsg.Value, 0));
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                        }
                        else if ((int)numericEscVentTInicio.Value > (int)numericEscCalTInicio.Value) // Se envía primero el del calefactor
                        {
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                            int valorFirstEsc = (int)numericEscVentTInicio.Value - (int)numericEscCalTInicio.Value;
                            await Task.Run(() => RecibirDatos2(valorFirstEsc, 40, (int)numericEscCalConsg.Value));
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                        }
                        else // Si ambos tiempos de inicio son iguales
                        {
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                        }

                        // Iniciar la recepción de datos
                        int valorPostEsc = (int)numericTEjecucion.Value + 1 - Math.Max((int)numericEscVentTInicio.Value, (int)numericEscCalTInicio.Value);
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos2(valorPostEsc, (int)numericEscVentConsg.Value, (int)numericEscCalConsg.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else
                    {
                        MessageBox.Show("Seleccione a qué componente aplicarle la entrada escalón");
                    }
                }
                else if (checkRampa.Checked) // Se ha seleccionado la entrada rampa
                {
                    Debug.WriteLine("Se ha seleccionado la entrada rampa");

                    if (checkRampVent.Checked && !checkRampCal.Checked) // Entrada rampa para el ventilador
                    {
                        Debug.WriteLine("Entrada rampa para el Ventilador");
                        // Valores iniciales antes de la entrada rampa
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreRamp = (int)numericRampVentTInicio.Value;
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");
                        await Task.Run(() => RecibirDatos(valorPreRamp, 40));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value; i++)
                        {
                            int valorMidRamp = 40 + (i * ((int)numericRampVentConsg.Value - 40 / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value)));
                            EnviarDatos($"v{valorMidRamp.ToString()}V");
                            await Task.Run(() => RecibirDatos(1, valorMidRamp));
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        EnviarDatos($"v{numericRampVentConsg.Value.ToString()}V");
                        // Iniciar la recepción de datos
                        int valorPostRamp = (int)numericTEjecucion.Value + 1 - (int)numericRampVentTFinal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");
                        await Task.Run(() => RecibirDatos(valorPostRamp, (int)numericRampVentConsg.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (!checkRampVent.Checked && checkRampCal.Checked) // Entrada rampa para el calefactor
                    {
                        Debug.WriteLine("Entrada rampa para el Calefactor");
                        // Valores iniciales antes de la entrada rampa
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreRamp = (int)numericRampCalTInicio.Value;
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");
                        await Task.Run(() => RecibirDatos(valorPreRamp, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value; i++)
                        {
                            int valorMidRamp = i * ((int)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value));
                            EnviarDatos($"n{valorMidRamp.ToString()}N");
                            await Task.Run(() => RecibirDatos(1, valorMidRamp));
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        EnviarDatos($"n{numericRampCalConsg.Value.ToString()}N");
                        // Iniciar la recepción de datos
                        int valorPostRamp = (int)numericTEjecucion.Value + 1 - (int)numericRampCalTFinal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");
                        await Task.Run(() => RecibirDatos(valorPostRamp, (int)numericRampCalConsg.Value));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (checkRampVent.Checked && checkRampCal.Checked) // Entrada rampa para ambos componentes
                    {
                        Debug.WriteLine("Entrada rampa para ambos componentes");
                        // Valores iniciales antes de la entrada rampa
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreRamp = Math.Min((int)numericRampVentTInicio.Value, (int)numericRampCalTInicio.Value);
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");
                        await Task.Run(() => RecibirDatos2(valorPreRamp, 40, 0));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Enviar el primer valor de rampa
                        if ((int)numericRampVentTInicio.Value < (int)numericRampCalTInicio.Value) // Se envía primero el del ventilador
                        {
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                            int valorFirstEsc = (int)numericEscCalTInicio.Value - (int)numericEscVentTInicio.Value;
                            await Task.Run(() => RecibirDatos2(valorFirstEsc, (int)numericEscVentConsg.Value, 0));
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                        }
                        else if ((int)numericEscVentTInicio.Value > (int)numericEscCalTInicio.Value) // Se envía primero el del calefactor
                        {
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                            int valorFirstEsc = (int)numericEscVentTInicio.Value - (int)numericEscCalTInicio.Value;
                            await Task.Run(() => RecibirDatos2(valorFirstEsc, 0, (int)numericEscCalConsg.Value));
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                        }
                        else // Si ambos tiempos de inicio son iguales
                        {
                            // Se itera hasta el tiempo final de la rampa que termine más tarde y se controla
                            // internamente el valor de cada rampa para no superar el valor de consigna
                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampVentTInicio.Value; i++)
                            {
                                // El valor de cada rampa no puede superar el valor de consigna definido por el usuario
                                int valorMidRampVent = Math.Min(40 + (i * ((int)numericRampVentConsg.Value - 40 / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (int)numericRampVentConsg.Value);
                                EnviarDatos($"v{valorMidRampVent.ToString()}V");
                                int valorMidRampCal = Math.Min(i * ((int)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (int)numericRampCalConsg.Value);
                                EnviarDatos($"n{valorMidRampCal.ToString()}N");

                                await Task.Run(() => RecibirDatos2(1, valorMidRampVent, valorMidRampCal));
                            }

                            // Ajustar los valores enviados a los seleccionados por el usuario para cada entrada rampa
                            EnviarDatos($"v{numericRampVentConsg.Value.ToString()}V");
                            EnviarDatos($"n{numericRampCalConsg.Value.ToString()}N");
                        }

                        // Iniciar la recepción de datos
                        int valorPostEsc = (int)numericTEjecucion.Value + 1 - Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value);
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos2(valorPostEsc, (int)numericEscVentConsg.Value, (int)numericEscCalConsg.Value));
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
                entradaChart.Visible = true; // Mostrar el gráfico de entradas
                tempChart.Size = new Size(810, 330); // Ajustar el tamaño del gráfico de temperatura
            }
        }
    }
}
