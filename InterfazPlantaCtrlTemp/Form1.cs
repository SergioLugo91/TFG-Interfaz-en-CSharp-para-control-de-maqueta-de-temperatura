using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace InterfazPlantaCtrlTemp
{
    public partial class Form1 : Form
    {
        // Declaración de variables globales
        System.IO.Ports.SerialPort PuertoArduino;
        readonly Stopwatch cronometro;
        private System.Windows.Forms.Timer portCheckTimer;

        private int valorPendienteVentVel = -1;
        private int valorPendienteCalPot = -1;
        private System.Windows.Forms.Timer ventDebounceTimer;
        private System.Windows.Forms.Timer calDebounceTimer;
        private readonly int ratioEnvio = 200;

        private Ventilador ventilador;
        private Calefactor calefactor;

        private ChartValues<double> temperaturas = new ChartValues<double>();
        private ChartValues<double> tiempos = new ChartValues<double>();

        private ChartValues<double> entradas = new ChartValues<double>();
        private ChartValues<double> entradaCal = new ChartValues<double>();
        private ChartValues<double> entradaVent = new ChartValues<double>();

        private AxisSection setpointSection;

        private CancellationTokenSource manualModeCts;

        public Form1()
        {
            InitializeComponent();

            ventilador = new Ventilador();
            InitializeVentDebounce();
            calefactor = new Calefactor();
            InitializeCalDebounce();

            // Inicializar el cronómetro
            cronometro = new Stopwatch();

            // Suscripciones de eventos para los controles de la interfaz
            numericVelVent.ValueChanged += NumericVelVent_ValueChanged;
            numericPotCal.ValueChanged += NumericPotCal_ValueChanged;
            trackVelVent.MouseUp += TrackVelVent_MouseUp;
            trackPotCal.MouseUp += TrackPotCal_MouseUp;

            numericRampVentTInicio.ValueChanged += numericRampVentTInicio_ValueChanged;
            numericRampVentTFinal.ValueChanged += numericRampVentTFinal_ValueChanged;
            numericRampCalTInicio.ValueChanged += numericRampCalTInicio_ValueChanged;
            numericRampCalTFinal.ValueChanged += numericRampCalTFinal_ValueChanged;

            numericTEjecucionAuto.ValueChanged += NumericTEjecucion_ValueChanged;
            numericTEjecucionLC.ValueChanged += NumericTEjecucionLC_ValueChanged;

            checkEscalon.CheckedChanged += checkEscalon_CheckedChanged;
            checkRampa.CheckedChanged += checkRampa_CheckedChanged;

            checkCrtlManual.CheckedChanged += CheckCrtlManual_CheckedChanged;

            // Asociar el evento FormClosing con el método Form1_FormClosing
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            // Asociar el evento Load con el método Form1_Load
            this.Load += new EventHandler(Form1_Load);

            // Suscripciones para enviar cuando se cambien los valores
            ventilador.VelocidadChanged += v =>
            {
                if (PuertoArduino != null && PuertoArduino.IsOpen)
                    EnviarDatos($"v{v}V");

            };

            calefactor.PotenciaChanged += p =>
            {
                if (PuertoArduino != null && PuertoArduino.IsOpen)
                    EnviarDatos($"n{p}N");
            };
        }

        // Debounce para el ventilador para evitar envíos excesivos en el control manual
        private void InitializeVentDebounce()
        {
            ventDebounceTimer = new System.Windows.Forms.Timer();
            ventDebounceTimer.Interval = ratioEnvio;
            ventDebounceTimer.Tick += VentDebounceTimer_Tick;
        }
        private void VentDebounceTimer_Tick(object sender, EventArgs e)
        {
            // Se dispara cuando han pasado ventDebounceMs sin cambios: aplicar pending
            ventDebounceTimer.Stop();
            ApplyPendingVentVel();
        }
        private void ApplyPendingVentVel()
        {
            if (valorPendienteVentVel < 0) return;
            // Establecer velocidad en el objeto (disparará Ventilador.VelocidadChanged y enviará al Arduino)
            ventilador.SetVelocidad(valorPendienteVentVel);
            Debug.WriteLine($"[Debounce] Aplicada velocidad ventilador: {valorPendienteVentVel}");
            valorPendienteVentVel = -1;
        }

        // Debounce para el calefactor para evitar envíos excesivos en el control manual
        private void InitializeCalDebounce()
        {
            calDebounceTimer = new System.Windows.Forms.Timer();
            calDebounceTimer.Interval = ratioEnvio;
            calDebounceTimer.Tick += CalDebounceTimer_Tick;
        }
        private void CalDebounceTimer_Tick(object sender, EventArgs e)
        {
            // Se dispara cuando han pasado ratioEnvio cambios: aplicar pending
            calDebounceTimer.Stop();
            ApplyPendingCalPot();
        }
        private void ApplyPendingCalPot()
        {
            if (valorPendienteCalPot < 0) return;
            // Establecer potencia en el objeto (disparará Calefactor.PotenciaChanged y enviará al Arduino)
            calefactor.SetPotencia(valorPendienteCalPot);
            Debug.WriteLine($"[Debounce] Aplicada potencia calefactor: {valorPendienteCalPot}");
            valorPendienteCalPot = -1;
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
            buttonCargarEntradas.Enabled = false;
            checkCrtlManual.Enabled = false;
            numericVelVent.Enabled = false;
            trackVelVent.Enabled = false;
            numericPotCal.Enabled = false;
            trackPotCal.Enabled = false;
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
            ventilador.SetVelocidad(40); // Reducir ventilador
            calefactor.SetPotencia(0);   // Apagar calefactor

            // Cerrar puerto
            if (PuertoArduino != null && PuertoArduino.IsOpen)
            {
                try { PuertoArduino.Close(); }
                catch (Exception ex) { Debug.WriteLine($"Error al cerrar el puerto: {ex.Message}"); }
                Debug.WriteLine("Puerto Cerrado");
            }
        }

        // Método para conectar el puerto serial con el botón conectar
        private void BtnConectar_Click(object sender, EventArgs e)
        {
            // Crear Puerto Serial
            try
            {
                if (comBox.SelectedItem == null)
                {
                    MessageBox.Show("Seleccione un puerto serial antes de conectar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                PuertoArduino = new System.IO.Ports.SerialPort();
                PuertoArduino.PortName = comBox.SelectedItem.ToString();
                PuertoArduino.BaudRate = 115200;
                PuertoArduino.DtrEnable = true;

                PuertoArduino.Open();
                PuertoArduino.ReadExisting();

                if (PuertoArduino.IsOpen)
                {
                    Debug.WriteLine("Puerto Abierto");
                    comBox.Enabled = false;
                    btnConectar.Enabled = false;
                    buttonCargarEntradas.Enabled = true;
                    checkCrtlManual.Enabled = true;

                    // Detener el timer si lo encontramos
                    portCheckTimer.Stop();
                    portCheckTimer.Dispose();
                }
            }
            catch (IOException)
            {
                MessageBox.Show("No se puede acceder al puerto seleccionado. Verifique la conexión y el puerto.", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para actualizar el valor máximo de Tfinal para el Ventilador y el Calefactor al cambiar el Tiempo de Ejecución
        private void NumericTEjecucion_ValueChanged(object sender, EventArgs e)
        {
            // Actualizar el valor máximo de TFinal para el Ventilador
            numericRampVentTFinal.Maximum = numericTEjecucionAuto.Value + 1;
            numericRampVentTInicio.Maximum = numericTEjecucionAuto.Value;
            numericEscVentTInicio.Maximum = numericTEjecucionAuto.Value;
            // Actualizar el valor máximo de TFinal para el Calefactor
            numericRampCalTFinal.Maximum = numericTEjecucionAuto.Value + 1;
            numericRampCalTInicio.Maximum = numericTEjecucionAuto.Value;
            numericEscCalTInicio.Maximum = numericTEjecucionAuto.Value;

            // Sincronizar con numericTEjecucionLC
            numericTEjecucionLC.Value = numericTEjecucionAuto.Value;
        }
        private void NumericTEjecucionLC_ValueChanged(object sender, EventArgs e)
        {
            // Sincronizar con numericTEjecucionAuto
            numericTEjecucionAuto.Value = numericTEjecucionLC.Value;
        }

        // Métodos para sincronizar la barra con el numericUpDown y cambiar los valores del Ventilador y el Calefactor
        private void NumericVelVent_ValueChanged(object sender, EventArgs e)
        {
            // Sincronización inmediata de la UI
            trackVelVent.Value = Convert.ToInt32(numericVelVent.Value);

            // Guardar el valor pendiente y reiniciar debounce
            valorPendienteVentVel = (int)numericVelVent.Value;
            if (ventDebounceTimer != null)
            {
                ventDebounceTimer.Stop();
                ventDebounceTimer.Start();
            }
        }
        private void TrackVelVent_MouseUp(object sender, EventArgs e)
        {
            // Asegurar que la UI y pending estén con el valor final
            numericVelVent.Value = trackVelVent.Value;
            valorPendienteVentVel = trackVelVent.Value;

            // Enviar inmediatamente: parar debounce y aplicar
            if (ventDebounceTimer != null) ventDebounceTimer.Stop();
            ApplyPendingVentVel();
        }
        private void NumericPotCal_ValueChanged(object sender, EventArgs e)
        {
            // Sincronización inmediata de la UI
            trackPotCal.Value = Convert.ToInt32(numericPotCal.Value);

            // Guardar el valor pendiente y reiniciar debounce
            valorPendienteCalPot = (int)numericPotCal.Value;
            if (calDebounceTimer != null)
            {
                calDebounceTimer.Stop();
                calDebounceTimer.Start();
            }
        }
        private void TrackPotCal_MouseUp(object sender, EventArgs e)
        {
            // Asegurar que la UI y pending estén con el valor final
            numericPotCal.Value = trackPotCal.Value;
            valorPendienteCalPot = trackPotCal.Value;

            // Enviar inmediatamente: parar debounce y aplicar
            calDebounceTimer?.Stop();
            ApplyPendingCalPot();
        }

        // Obtener el valor máximo del eje X para inicializar el gráfico
        private double GetMaxEjeX()
        {
            // Selecciona el numeric correspondiente según el modo
            double maxEje = checkCrtlManual.Checked ? (double)numericTamVentana.Value : (double)numericTEjecucionAuto.Value;
            return maxEje;
        }

        // Método para la configuración inicial del gráfico
        private void ConfigurarGraficoTempInicial()
        {
            // Crear un pincel con opacidad personalizada para el Fill
            var fillBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            fillBrush.Opacity = 0.3;

            tempChart.Series.Clear();
            tempChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperatura",
                    Values = temperaturas,
                    Fill = fillBrush,
                    Stroke = System.Windows.Media.Brushes.Orange,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    StrokeThickness = 2  // Añadido para mejor visibilidad
                }
            };
            double MaxEjeX = GetMaxEjeX();

            tempChart.AxisX.Clear();
            tempChart.AxisX.Add(new Axis
            {
                Title = "Tiempo (s)",
                LabelFormatter = value => value.ToString("N1") + "s",
                MinValue = 0d,
                MaxValue = MaxEjeX
            });

            tempChart.AxisY.Clear();
            tempChart.AxisY.Add(new Axis
            {
                Title = "Temperatura (°C)",
                LabelFormatter = value => value.ToString("N1") + "°C",
                MinValue = 0,  // Valor inicial mínimo
                MaxValue = 65  // Valor inicial máximo
            });
        }

        // Método para configurar el gráfico de entradas
        private void ConfigurarGraficoEntradas()
        {
            // Crear un pincel con opacidad personalizada para el Fill
            var fillBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightSkyBlue);
            fillBrush.Opacity = 0.3;

            entradaChart.Series.Clear();
            entradaChart.Series = new SeriesCollection
            {
               new LineSeries
               {
                   Title = "Entrada",
                   Values = entradas,
                   Fill = fillBrush,
                   Stroke = System.Windows.Media.Brushes.LightSkyBlue,
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
                MinValue = 0d,
                MaxValue = (int)numericTEjecucionAuto.Value // Establecer el máximo inicial según los puntos esperados
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

        // Método para configurar el gráfico de entradas cuando se usan dos entradas automáticas
        private void ConfigurarGraficoEntradas2()
        {
            var fillBrushVent = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
            fillBrushVent.Opacity = 0.3;

            var fillBrushCal = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            fillBrushCal.Opacity = 0.3;

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
                    Stroke = System.Windows.Media.Brushes.Red,
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
                MinValue = 0d,
                MaxValue = (int)numericTEjecucionAuto.Value // Establecer el máximo inicial según los puntos esperados
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

        // Método para recortar la serie de temperatura a una ventana móvil (segundos)
        private void TrimTemperatureWindow(double windowSeconds)
        {
            // Asegurar ejecución en hilo UI
            tempChart.Invoke((MethodInvoker)delegate
            {
                if (tiempos == null || temperaturas == null || tiempos.Count == 0) return;

                double now = cronometro.Elapsed.TotalSeconds;
                double minAllowed = Math.Max(0.0, now - windowSeconds);

                // Ajustar ejes X para mostrar la ventana
                if (tempChart.AxisX != null && tempChart.AxisX.Count > 0)
                {
                    tempChart.AxisX[0].MinValue = minAllowed;
                    tempChart.AxisX[0].MaxValue = Math.Max(now,(double)numericTamVentana.Value);
                }
            });
        }

        // Método de enviar datos al arduino
        private void EnviarDatos(string datos)
        {
            PuertoArduino.WriteLine(datos);
            Debug.WriteLine($"Datos enviados: {datos}");
        }

        // Método de recibir datos del arduino
        private void RecibirDatos(int graph, float entrada, int dt)
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

                        // Ajustar eje Y dinámicamente
                        if (temperaturas.Count > 0)
                        {
                            double margen = Math.Max(5, temperaturas.Max() * 0.1); // 10% de margen o 5 unidades
                            tempChart.AxisY[0].MinValue = Math.Floor(temperaturas.Min() - margen);
                            tempChart.AxisY[0].MaxValue = Math.Ceiling(temperaturas.Max() + margen);
                        }

                    });

                    // Recortar la serie de temperatura si está activado el control manual
                    if (checkCrtlManual.Checked)
                    {
                        TrimTemperatureWindow((double)numericTamVentana.Value);
                    }

                    // Añadir valores al gráfico de entradas (Actualizar UI) (Para el modo de entradas del sistema)
                    entradaChart.Invoke((MethodInvoker)delegate
                    {
                        // Añadir los valores a las series
                        entradas.Add(entrada);
                        tiempos.Add(tiempoActual);

                        // Ajustar eje Y dinámicamente
                        if (entradas.Count > 0)
                        {
                            double margen = Math.Max(5, entradas.Max() * 0.1); // 10% de margen o 5 unidades
                            entradaChart.AxisY[0].MinValue = Math.Floor(entradas.Min() - margen);
                            entradaChart.AxisY[0].MaxValue = Math.Ceiling(entradas.Max() + margen);
                        }
                    });

                    // Esperar un segundo antes de la siguiente lectura
                    Thread.Sleep(dt);
                }
                catch (TimeoutException) { Debug.WriteLine("Timeout"); }
            }
        }

        // Método de recibir datos para dos entradas de control automático
        private void RecibirDatos2(int graph, float entradavent, float entradacal, int dt)
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

                        // Ajustar eje Y dinámicamente
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
                        entradaVent.Add(entradavent);
                        entradaCal.Add(entradacal);
                        tiempos.Add(tiempoActual);

                        // Ajustar eje Y dinámicamente
                        if (entradaVent.Count > 0 || entradaCal.Count > 0)
                        {
                            double margen = Math.Max(5, Math.Max(entradaVent.Max(), entradaCal.Max()) * 0.1); // 10% de margen o 5 unidades
                            entradaChart.AxisY[0].MinValue = Math.Floor(Math.Min(entradaVent.Min(), entradaCal.Min()) - margen);
                            entradaChart.AxisY[0].MaxValue = Math.Ceiling(Math.Max(entradaVent.Max(), entradaCal.Max()) + margen);
                        }
                    });

                    // Esperar un segundo antes de la siguiente lectura
                    Thread.Sleep(dt);
                }
                catch (TimeoutException) { Debug.WriteLine("Timeout"); }
            }
        }

        // Control manual del sistema
        private void StartModoManual()
        {
            // Cancelar cualquier loop previo por seguridad
            StopModoManual();

            manualModeCts = new CancellationTokenSource();
            var token = manualModeCts.Token;

            // Asegurar que el cronómetro está en marcha
            cronometro.Restart();

            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        RecibirDatos(1, ventilador.GetVelocidad(), 1000);
                    }
                    finally
                    {
                        Debug.WriteLine("Iteración del loop manual completada");
                    }
                }
            }, token);
        }
        private void StopModoManual()
        {
            if (manualModeCts != null)
            {
                try
                {
                    manualModeCts.Cancel();
                    manualModeCts.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cancelando loop manual: {ex.Message}");
                }
                finally
                {
                    manualModeCts = null;
                }
            }
        }
        private void CheckCrtlManual_CheckedChanged(object sender, EventArgs e)
        {
            if (checkCrtlManual.Checked)
            {
                // Habilitar controles de control manual
                numericVelVent.Enabled = true;
                trackVelVent.Enabled = true;
                numericPotCal.Enabled = true;
                trackPotCal.Enabled = true;
                // Deshabilitar controles de entradas del sistema
                checkEscalon.Enabled = false;
                checkRampa.Enabled = false;
                buttonCargarEntradas.Enabled = false;

                entradaChart.Visible = false; // Ocultar el gráfico de entradas

                tempChart.Size = new Size(900, 700); // Ajustar el tamaño del gráfico de temperatura

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
                }
                finally
                {
                    // Rehabilitar botón al finalizar
                    buttonCargarEntradas.Enabled = true;
                }

                cronometro.Restart();
                StartModoManual();
            }
            else
            {
                StopModoManual();

                // Deshabilitar controles de control manual
                numericVelVent.Enabled = false;
                trackVelVent.Enabled = false;
                numericPotCal.Enabled = false;
                trackPotCal.Enabled = false;
                // Habilitar controles de entradas del sistema
                checkEscalon.Enabled = true;
                checkRampa.Enabled = true;
                buttonCargarEntradas.Enabled = true;
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
                buttonCargarEntradas.Enabled = true;

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
                buttonCargarEntradas.Enabled = true;

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
            entradaChart.Visible = true; // Mostrar el gráfico de entradas
            checkCrtlManual.Enabled = false; // Deshabilitar el control manual durante la operación

            tempChart.Size = new Size(900, 450); // Ajustar el tamaño del gráfico de temperatura

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
                entradaCal.Clear();
                entradaVent.Clear();
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

                        // Iniciar la recepción de datos
                        int valorPreEsc = (int)numericEscVentTInicio.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos(valorPreEsc, ventilador.GetVelocidad(), 1000));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        ventilador.SetVelocidad((int)numericEscVentConsg.Value);
                        EnviarDatos($"v{ventilador.GetVelocidad().ToString()}V");
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - (int)numericEscVentTInicio.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos(valorPostEsc, ventilador.GetVelocidad(), 1000));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (!checkEscVent.Checked && checkEscCal.Checked) // Solo entrada escalón para el calefactor
                    {
                        Debug.WriteLine("Entrada escalón para el Calefactor");

                        // Iniciar la recepción de datos
                        int valorPreEsc = (int)numericEscCalTInicio.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos(valorPreEsc, calefactor.GetPotencia(), 1000));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        calefactor.SetPotencia((int)numericEscCalConsg.Value);
                        EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");

                        // Iniciar la recepción de datos
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - (int)numericEscCalTInicio.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos(valorPostEsc, (float)numericEscCalConsg.Value, 1000));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (checkEscVent.Checked && checkEscCal.Checked) // Entradas escalón para ambos componentes
                    {
                        Debug.WriteLine("Entrada escalón para ambos componentes");
                        ConfigurarGraficoEntradas2();
                        // Valores iniciales antes de la entrada escalón
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreEsc = Math.Min((int)numericEscVentTInicio.Value, (int)numericEscCalTInicio.Value);
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");
                        await Task.Run(() => RecibirDatos2(valorPreEsc, 40, 0, 1000));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Enviar el primer valor de escalón
                        if ((int)numericEscVentTInicio.Value < (int)numericEscCalTInicio.Value) // Se envía primero el del ventilador
                        {
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                            int valorFirstEsc = (int)numericEscCalTInicio.Value - (int)numericEscVentTInicio.Value;
                            await Task.Run(() => RecibirDatos2(valorFirstEsc, (float)numericEscVentConsg.Value, 0, 1000));
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                        }
                        else if ((int)numericEscVentTInicio.Value > (int)numericEscCalTInicio.Value) // Se envía primero el del calefactor
                        {
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                            int valorFirstEsc = (int)numericEscVentTInicio.Value - (int)numericEscCalTInicio.Value;
                            await Task.Run(() => RecibirDatos2(valorFirstEsc, 40, (float)numericEscCalConsg.Value, 1000));
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                        }
                        else // Si ambos tiempos de inicio son iguales
                        {
                            EnviarDatos($"v{numericEscVentConsg.Value.ToString()}V");
                            EnviarDatos($"n{numericEscCalConsg.Value.ToString()}N");
                        }

                        // Iniciar la recepción de datos
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - Math.Max((int)numericEscVentTInicio.Value, (int)numericEscCalTInicio.Value);
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos2(valorPostEsc, (float)numericEscVentConsg.Value, (float)numericEscCalConsg.Value, 1000));
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
                        await Task.Run(() => RecibirDatos(valorPreRamp, 40, 1000));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value; i++)
                        {
                            float valorMidRamp = 40 + (i * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value)));
                            EnviarDatos($"v{valorMidRamp.ToString()}V");
                            await Task.Run(() => RecibirDatos(1, valorMidRamp, 1000));
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        EnviarDatos($"v{numericRampVentConsg.Value.ToString()}V");
                        // Iniciar la recepción de datos
                        int valorPostRamp = (int)numericTEjecucionAuto.Value + 1 - (int)numericRampVentTFinal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");
                        await Task.Run(() => RecibirDatos(valorPostRamp, (float)numericRampVentConsg.Value, 1000));
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
                        await Task.Run(() => RecibirDatos(valorPreRamp, 0, 1000));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value; i++)
                        {
                            float valorMidRamp = i * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value));
                            EnviarDatos($"n{valorMidRamp.ToString()}N");
                            await Task.Run(() => RecibirDatos(1, valorMidRamp, 1000));
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        EnviarDatos($"n{numericRampCalConsg.Value.ToString()}N");
                        // Iniciar la recepción de datos
                        int valorPostRamp = (int)numericTEjecucionAuto.Value + 1 - (int)numericRampCalTFinal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");
                        await Task.Run(() => RecibirDatos(valorPostRamp, (float)numericRampCalConsg.Value, 1000));
                        Debug.WriteLine("Fin de RecibirDatos");
                    }
                    else if (checkRampVent.Checked && checkRampCal.Checked) // Entrada rampa para ambos componentes
                    {
                        Debug.WriteLine("Entrada rampa para ambos componentes");
                        ConfigurarGraficoEntradas2();
                        // Valores iniciales antes de la entrada rampa
                        EnviarDatos("v40V");
                        EnviarDatos("n0N");

                        // Iniciar la recepción de datos
                        int valorPreRamp = Math.Min((int)numericRampVentTInicio.Value, (int)numericRampCalTInicio.Value);
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");
                        await Task.Run(() => RecibirDatos2(valorPreRamp, 40, 0, 1000));
                        Debug.WriteLine("Fin de RecibirDatos");

                        // Enviar el primer valor de rampa
                        if ((int)numericRampVentTInicio.Value < (int)numericRampCalTInicio.Value) // Se envía primero el del ventilador
                        {
                            int j = 0;

                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampVentTInicio.Value; i++)
                            {
                                // El valor de cada rampa no puede superar el valor de consigna definido por el usuario
                                float valorMidRampVent = Math.Min(40 + (i * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (float)numericRampVentConsg.Value);
                                EnviarDatos($"v{valorMidRampVent.ToString()}V");

                                // Antes de que se alcance el tiempo de inicio de la rampa del calefactor, su valor se mantiene en 0
                                if (cronometro.Elapsed.TotalSeconds < (int)numericRampCalTInicio.Value + 1)
                                {
                                    int valorMidRampCal = 0;
                                    EnviarDatos($"n{valorMidRampCal.ToString()}N");

                                    await Task.Run(() => RecibirDatos2(1, valorMidRampVent, valorMidRampCal, 1000));
                                }
                                // Cuando el tiempo de inicio de la rampa del calefactor se alcanza, se empieza a incrementar su valor
                                else
                                {
                                    j++;
                                    float valorMidRampCal = Math.Min(j * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (float)numericRampCalConsg.Value);
                                    EnviarDatos($"n{valorMidRampCal.ToString()}N");

                                    await Task.Run(() => RecibirDatos2(1, valorMidRampVent, valorMidRampCal, 1000));
                                }
                            }
                        }
                        else if ((int)numericRampVentTInicio.Value > (int)numericRampCalTInicio.Value) // Se envía primero el del calefactor
                        {
                            int j = 0;

                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampCalTInicio.Value; i++)
                            {
                                // El valor de cada rampa no puede superar el valor de consigna definido por el usuario
                                float valorMidRampCal = Math.Min(i * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (float)numericRampCalConsg.Value);
                                EnviarDatos($"n{valorMidRampCal.ToString()}N");

                                // Antes de que se alcance el tiempo de inicio de la rampa del ventilador, su valor se mantiene en 40
                                if (cronometro.Elapsed.TotalSeconds < (int)numericRampVentTInicio.Value + 1)
                                {
                                    int valorMidRampVent = 40;
                                    EnviarDatos($"v{valorMidRampVent.ToString()}V");

                                    await Task.Run(() => RecibirDatos2(1, valorMidRampVent, valorMidRampCal, 1000));
                                }
                                // Cuando el tiempo de inicio de la rampa del ventilador se alcanza, se empieza a incrementar su valor
                                else
                                {
                                    j++;
                                    float valorMidRampVent = Math.Min(40 + (j * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (float)numericRampVentConsg.Value);
                                    EnviarDatos($"v{valorMidRampVent.ToString()}V");

                                    await Task.Run(() => RecibirDatos2(1, valorMidRampVent, valorMidRampCal, 1000));
                                }
                            }

                        }
                        else // Si ambos tiempos de inicio son iguales
                        {
                            // Se itera hasta el tiempo final de la rampa que termine más tarde y se controla
                            // internamente el valor de cada rampa para no superar el valor de consigna
                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampVentTInicio.Value; i++)
                            {
                                // El valor de cada rampa no puede superar el valor de consigna definido por el usuario
                                float valorMidRampVent = Math.Min(40 + (i * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (float)numericRampVentConsg.Value);
                                EnviarDatos($"v{valorMidRampVent.ToString()}V");
                                float valorMidRampCal = Math.Min(i * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (float)numericRampCalConsg.Value);
                                EnviarDatos($"n{valorMidRampCal.ToString()}N");

                                await Task.Run(() => RecibirDatos2(1, valorMidRampVent, valorMidRampCal, 1000));
                            }

                            // Ajustar los valores enviados a los seleccionados por el usuario para cada entrada rampa
                            EnviarDatos($"v{numericRampVentConsg.Value.ToString()}V");
                            EnviarDatos($"n{numericRampCalConsg.Value.ToString()}N");
                        }

                        // Envío de valores finales de rampa
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value);
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");
                        await Task.Run(() => RecibirDatos2(valorPostEsc, (float)numericRampVentConsg.Value, (float)numericRampCalConsg.Value, 1000));
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
                checkCrtlManual.Enabled = true;
            }
        }

        // Método para ocultar o mostrar el gráfico de entradas
        private void buttonOcultar_Click(object sender, EventArgs e)
        {
            if (checkEscalon.Checked || checkRampa.Checked)
            {
                if (entradaChart.Visible == true)
                {
                    entradaChart.Visible = false; // Ocultar el gráfico de entradas
                    tempChart.Size = new Size(900, 700); // Ajustar el tamaño del gráfico de temperatura
                }
                else
                {
                    entradaChart.Visible = true; // Mostrar el gráfico de entradas
                    tempChart.Size = new Size(900, 450); // Ajustar el tamaño del gráfico de temperatura
                }
            }
        }

        // Método para refrescar la ventana de visualización
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                // Limpiar gráfico
                tempChart.Series.Clear();
                entradaChart.Series.Clear();
                temperaturas.Clear();
                entradaCal.Clear();
                entradaVent.Clear();
                tiempos.Clear();
                tempChart.Update(true, true);
                entradaChart.Update(true, true);
                Debug.WriteLine("Gráficos actualizados");

                // Configurar el gráfico inicial
                ConfigurarGraficoTempInicial();
                ConfigurarGraficoEntradas();
                ConfigurarGraficoEntradas2();
            }
            catch (Exception) { }

            cronometro.Restart();
        }

        // Control de Entradas en Lazo Cerrado Mediante un Controlador PID

        private void calculoComando(float Kp, float Ki, float Kd)
        {
            int comando;
            float temperatura = 0;
            int dt = 10; // ms
            float dtSec = dt / 1000f;
            float error = 0;
            float errorPrev = 0;
            float integralTotal = 0f;

            const float outMin = 0f;
            const float outMax = 85f;

            for (double i = 0; i < (double)numericTEjecucionLC.Value; i += (dt/1000D)) 
            {
                PuertoArduino.WriteLine($"t{i}T");
                PuertoArduino.ReadTimeout = 200;
                try
                {
                    string lectura = PuertoArduino.ReadLine().Trim();
                    Debug.WriteLine($"Datos recibidos: {lectura}");
                    // Extraer y parsear el dato de temperatura
                    lectura = lectura.Substring(1, 5);
                    temperatura = float.Parse(lectura, CultureInfo.InvariantCulture);
                    Debug.WriteLine($"Temperatura actual: {temperatura}°C");

                    // Cálculo del error
                    error = (float)numericConsgTemp.Value - temperatura;
                    Debug.WriteLine($"Error actual: {error}");

                    // Cálculo de la integral del error
                    integralTotal += error * dtSec;

                    // Anti-windup: limitar integral
                    if (Math.Abs(Ki) > 1e-6f)
                    {
                        float integralLimit = outMax / Math.Abs(Ki);
                        integralTotal = Math.Max(-integralLimit, Math.Min(integralTotal, integralLimit));
                    }
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine("Timeout");
                }

                float gananciaProporcional = Kp * error;
                float gananciaIntegral = Ki * integralTotal;
                float gananciaDerivativa = Kd * (error - errorPrev) / dtSec;

                errorPrev = error;

                // Salida sin saturar (valor calculado por PID)
                float comandoRaw = gananciaProporcional + gananciaIntegral + gananciaDerivativa;

                // Se limita el comando al rango permitido (0-85) y se envía al calefactor
                comando = (int)Math.Round(Math.Max(outMin, Math.Min(comandoRaw, outMax)));

                EnviarDatos($"n{comando}N"); 
                RecibirDatos(1, comando, dt);
            }
        }

        private void buttonCargarLazoCerrado_Click(object sender, EventArgs e)
        {
            checkKp.Enabled = false; // Deshabilitar el checkbox de Kp
            checkKi.Enabled = false; // Deshabilitar el checkbox de Ki
            checkKd.Enabled = false; // Deshabilitar el checkbox de Kd
            checkCrtlManual.Enabled = false; // Deshabilitar el control manual durante la operación

            // Limpiar gráfico
            tempChart.Series.Clear();
            temperaturas.Clear();
            tiempos.Clear();
            tempChart.Update(true, true);
            Debug.WriteLine("Gráfico actualizado");

            // Configurar el gráfico inicial
            ConfigurarGraficoTempInicial();
            // Crear o actualizar la línea horizontal de la consigna
            double consigna = (double)numericConsgTemp.Value;
            setpointSection = new AxisSection
            {
                Value = consigna,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2,
                SectionWidth = 0 // 0 para una línea en lugar de una franja
            };

            // Asignar la sección al eje Y (reemplaza secciones previas)
            tempChart.AxisY[0].Sections = new SectionsCollection { setpointSection };

            ConfigurarGraficoEntradas();

            cronometro.Restart();
            EnviarDatos("v40V"); // Velocidad fija del ventilador al 40%

            try
            {
                if (checkKp.Checked && !checkKi.Checked && !checkKd.Checked) // Se ha seleccionado el control P
                {
                    calculoComando((float)numericKp.Value, 0, 0);
                }
                else if (!checkKp.Checked && checkKi.Checked && !checkKd.Checked) // Se ha seleccionado el control I
                {
                    calculoComando(0, (float)numericKi.Value, 0);
                }
                else if (!checkKp.Checked && !checkKi.Checked && checkKd.Checked) // Se ha seleccionado el control D
                {
                    calculoComando(0, 0, (float)numericKd.Value);
                }
                else if (checkKp.Checked && checkKi.Checked && !checkKd.Checked) // Se ha seleccionado el control PI
                {
                    calculoComando((float)numericKp.Value, (float)numericKi.Value, 0);
                }
                else if (checkKp.Checked && !checkKi.Checked && checkKd.Checked) // Se ha seleccionado el control PD
                {
                    calculoComando((float)numericKp.Value, 0, (float)numericKd.Value);
                }
                else if (!checkKp.Checked && checkKi.Checked && checkKd.Checked) // Se ha seleccionado el control ID
                {
                    calculoComando(0, (float)numericKi.Value, (float)numericKd.Value);
                }
                else if (checkKp.Checked && checkKi.Checked && checkKd.Checked) // Se ha seleccionado el control PID
                {
                    calculoComando((float)numericKp.Value, (float)numericKi.Value, (float)numericKd.Value);
                }
                else
                {
                    MessageBox.Show("Seleccione un tipo de control en lazo cerrado válido");
                }
            }
            catch (Exception ex)
            {
                // Registrar para depuración
                Debug.WriteLine($"Error en lazo cerrado: {ex}");

                // Informar al usuario (mensaje breve y amigable)
                MessageBox.Show($"Se produjo un error durante el control en lazo cerrado:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restaurar estado de la interfaz
                checkKp.Enabled = true;
                checkKi.Enabled = true;
                checkKd.Enabled = true;
                buttonCargarLazoCerrado.Enabled = true;
                checkCrtlManual.Enabled = true;
            }
        }
    }
}
