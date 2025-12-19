using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.Analysis;
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
        private CancellationTokenSource pidModeCts;
        private CancellationTokenSource entradasModeCts;

        private DataFrame dfAuto;
        private DataFrame dfManual;
        private DataFrame dfPID;

        private readonly object dfManualLock = new object();
        private readonly object dfAutoLock = new object();
        private readonly object dfPIDLock = new object();

        public Form1()
        {
            InitializeComponent();

            ventilador = new Ventilador();
            InitializeVentDebounce();
            calefactor = new Calefactor();
            InitializeCalDebounce();

            // Inicializar el cronómetro
            cronometro = new Stopwatch();

            // Inicializar los dataFrame para almacenar los datos
            InitializeDataFrameAuto();
            InitializeDataFrameMan();
            InitializeDataFramePID();

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
            checkEscalon.Enabled = false;
            checkRampa.Enabled = false;
            checkPID.Enabled = false;
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
            // Detener cualquier modo en ejecución
            if (pidModeCts != null)
            {
                StopModoPID();
            }

            if (manualModeCts != null)
            {
                StopModoManual();
            }

            // Enviar comandos para apagar Ventilador y Calefactor
            try
            {
                ventilador.SetVelocidad(40);
                calefactor.SetPotencia(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enviando datos al cerrar: {ex.Message}");
            } finally {
                Thread.Sleep(200); // Esperar a que se envíen los datos antes de cerrar el puerto
            }

            // Cerrar puerto
            if (PuertoArduino != null && PuertoArduino.IsOpen)
            {
                try { PuertoArduino.Close(); }
                catch (Exception ex) { Debug.WriteLine($"Error al cerrar el puerto: {ex.Message}"); }
                Debug.WriteLine("Puerto Cerrado");
            }
        }

        // Método para conectar el puerto serial con el botón conectar
        private async void BtnConectar_Click(object sender, EventArgs e)
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
                    checkEscalon.Enabled = true;
                    checkRampa.Enabled = true;
                    checkPID.Enabled = true;

                    // Detener el timer si lo encontramos
                    portCheckTimer.Stop();
                    portCheckTimer.Dispose();
                }
            }
            catch (IOException)
            {
                MessageBox.Show("No se puede acceder al puerto seleccionado. Verifique la conexión y el puerto.", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            await Task.Delay(200); // Esperar a que el puerto esté listo
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
            double maxEje = (checkCrtlManual.Checked || checkPID.Checked) ? (double)numericTamVentana.Value : (double)numericTEjecucionAuto.Value;
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
            var fillBrushVent = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightSkyBlue);
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
                    Stroke = System.Windows.Media.Brushes.LightSkyBlue,
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
        private void TrimTemperatureWindow()
        {
            // Asegurar ejecución en hilo UI
            tempChart.Invoke((MethodInvoker)delegate
            {
                double windowSeconds = (double)numericTamVentana.Value;
                windowSeconds = Math.Max((double)numericTamVentana.Value, 5);
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
        private void TrimEntradaWindow()
        {
            // Asegurar ejecución en hilo UI
            entradaChart.Invoke((MethodInvoker)delegate
            {
                double windowSeconds = (double)numericTamVentana.Value;
                windowSeconds = Math.Max((double)numericTamVentana.Value, 5);

                if (tiempos == null || entradas == null || tiempos.Count == 0) return;
                double now = cronometro.Elapsed.TotalSeconds;
                double minAllowed = Math.Max(0.0, now - windowSeconds);
                // Ajustar ejes X para mostrar la ventana
                if (entradaChart.AxisX != null && entradaChart.AxisX.Count > 0)
                {
                    entradaChart.AxisX[0].MinValue = minAllowed;
                    entradaChart.AxisX[0].MaxValue = Math.Max(now, (double)numericTamVentana.Value);
                }
            });
        }

        // Métodos para controlar el DataFrame del modo automático
        private void InitializeDataFrameAuto()
        {
            // Columnas: tiempo, temperatura, entrada única, entrada ventilador, entrada calefactor
            var colTiempoAuto = new DoubleDataFrameColumn("Tiempo_s");
            var colTempAuto = new DoubleDataFrameColumn("Temperatura_C");
            var colEntradaVentAuto = new DoubleDataFrameColumn("Entrada_Vent");
            var colEntradaCalAuto = new DoubleDataFrameColumn("Entrada_Cal");

            // Inicializar el DataFrame para almacenar los datos
            dfAuto = new DataFrame(colTiempoAuto, colTempAuto, colEntradaVentAuto, colEntradaCalAuto);
        }
        private void AppendRowToDataFrameAuto(double tiempo, double temperatura, double EntradaVent, double EntradaCal)
        {
            lock (dfAutoLock)
            {
                if (dfAuto == null)
                {
                    Debug.WriteLine("DataFrame auto nulo: inicializando antes de añadir fila.");
                    InitializeDataFrameAuto();
                }

                var colTiempo = dfAuto.Columns["Tiempo_s"] as DoubleDataFrameColumn;
                var colTemp = dfAuto.Columns["Temperatura_C"] as DoubleDataFrameColumn;
                var colEntradaVentAuto = dfAuto.Columns["Entrada_Vent"] as DoubleDataFrameColumn;
                var colEntradaCalAuto = dfAuto.Columns["Entrada_Cal"] as DoubleDataFrameColumn;

                if (colTiempo == null || colTemp == null || colEntradaVentAuto == null || colEntradaCalAuto == null)
                {
                    Debug.WriteLine("Error: columnas dfAuto no encontradas.");
                    return;
                }

                // Append por columna (mantiene las longitudes sincronizadas)
                colTiempo.Append(tiempo);
                colTemp.Append(temperatura);
                colEntradaVentAuto.Append(EntradaVent);
                colEntradaCalAuto.Append(EntradaCal);

                // Obtener recuento fiable usando la longitud de las columnas
                long filasTiempo = colTiempo.Length;
                long filasTemp = colTemp.Length;
                long filasEntVent = colEntradaVentAuto.Length;
                long filasEntCal = colEntradaCalAuto.Length;
                long filas1 = Math.Min(filasTiempo, filasTemp);
                long filas2 = Math.Min(filasEntVent, filasEntCal);
                long filas = Math.Min(filas1, filas2);

                //Debug.WriteLine($"Fila añadida. Filas (por columna): Tiempo_s={filasTiempo}, Temperatura_C={filasTemp}, Entrada_Vent={filasEntVent}, Entrada_Cal{filasEntCal}");

                // Mostrar la última fila añadida para verificar contenido
                if (filas > 0)
                {
                    var ultimoTiempo = colTiempo[filas - 1];
                    var ultimaTemp = colTemp[filas - 1];
                    var ultimaEntVent = colEntradaVentAuto[filas - 1];
                    var ultimaEntCal = colEntradaCalAuto[filas - 1];
                    //Debug.WriteLine($"Última fila -> Tiempo_s: {ultimoTiempo}, Temperatura_C: {ultimaTemp}, Entrada_Vent: {ultimaEntVent}, Entrada_Cal: {ultimaEntCal}");
                }
            }
        }
        // Métodos para controlar el DataFrame del modo manual
        private void InitializeDataFrameMan()
        {
            // Columnas: tiempo, temperatura
            var colTiempoMan = new DoubleDataFrameColumn("Tiempo_s");
            var colTempMan = new DoubleDataFrameColumn("Temperatura_C");
            var colEntradaVentMan = new DoubleDataFrameColumn("Entrada_Vent");
            var colEntradaCalMan = new DoubleDataFrameColumn("Entrada_Cal");

            // Inicializar el DataFrame para almacenar los datos
            dfManual = new DataFrame(colTiempoMan, colTempMan, colEntradaVentMan, colEntradaCalMan);
            Debug.WriteLine("DataFrame manual inicializado.");
            Debug.WriteLine(dfManual);
        }
        private void AppendRowToDataFrameMan(double tiempo, double temperatura, double EntradaVent, double EntradaCal)
        {
            lock (dfManualLock)
            {
                if (dfManual == null)
                {
                    Debug.WriteLine("DataFrame manual nulo: inicializando antes de añadir fila.");
                    InitializeDataFrameMan();
                }

                var colTiempo = dfManual.Columns["Tiempo_s"] as DoubleDataFrameColumn;
                var colTemp = dfManual.Columns["Temperatura_C"] as DoubleDataFrameColumn;
                var colEntradaVentMan = dfManual.Columns["Entrada_Vent"] as DoubleDataFrameColumn;
                var colEntradaCalMan = dfManual.Columns["Entrada_Cal"] as DoubleDataFrameColumn;

                if (colTiempo == null || colTemp == null || colEntradaVentMan == null || colEntradaCalMan == null)
                {
                    Debug.WriteLine("Error: columnas dfManual no encontradas.");
                    return;
                }

                // Append por columna (mantiene las longitudes sincronizadas)
                colTiempo.Append(tiempo);
                colTemp.Append(temperatura);
                colEntradaVentMan.Append(EntradaVent);
                colEntradaCalMan.Append(EntradaCal);

                // Obtener recuento fiable usando la longitud de las columnas
                long filasTiempo = colTiempo.Length;
                long filasTemp = colTemp.Length;
                long filasEntVent = colEntradaVentMan.Length;
                long filasEntCal = colEntradaCalMan.Length;
                long filas1 = Math.Min(filasTiempo, filasTemp);
                long filas2 = Math.Min(filasEntVent, filasEntCal);
                long filas = Math.Min(filas1, filas2);

                Debug.WriteLine($"Fila añadida. Filas (por columna): Tiempo_s={filasTiempo}, Temperatura_C={filasTemp}, Entrada_Vent={filasEntVent}, Entrada_Cal{filasEntCal}");

                // Mostrar la última fila añadida para verificar contenido
                if (filas > 0)
                {
                    var ultimoTiempo = colTiempo[filas - 1];
                    var ultimaTemp = colTemp[filas - 1];
                    var ultimaEntVent = colEntradaVentMan[filas - 1];
                    var ultimaEntCal = colEntradaCalMan[filas - 1];
                    Debug.WriteLine($"Última fila -> Tiempo_s: {ultimoTiempo}, Temperatura_C: {ultimaTemp}, Entrada_Vent: {ultimaEntVent}, Entrada_Cal: {ultimaEntCal}");
                }
            }
        }

        // Métodos para controlar el DataFrame del modo PID
        private void InitializeDataFramePID()
        {
            // Columnas: tiempo, temperatura, consigna
            var colTiempoPID = new DoubleDataFrameColumn("Tiempo_s");
            var colTempPID = new DoubleDataFrameColumn("Temperatura_C");
            var colEntradaVentPID = new DoubleDataFrameColumn("Entrada_Vent");
            var colEntradaCalPID = new DoubleDataFrameColumn("Entrada_Cal");
            var colConsignaPID = new DoubleDataFrameColumn("Consigna");
            var colErrorPID = new DoubleDataFrameColumn("Error");

            // Inicializar el DataFrame para almacenar los datos
            dfPID = new DataFrame(colTiempoPID, colTempPID, colEntradaVentPID, colEntradaCalPID, colConsignaPID, colErrorPID);
        }
        private void AppendRowToDataFramePID(double tiempo, double temperatura, double entradaVent, double entradaCal, double consigna, double error)
        {
            lock (dfPIDLock)
            {
                if (dfPID == null)
                {
                    Debug.WriteLine("DataFrame PID nulo: inicializando antes de añadir fila.");
                    InitializeDataFramePID();
                }

                var colTiempo = dfPID.Columns["Tiempo_s"] as DoubleDataFrameColumn;
                var colTemp = dfPID.Columns["Temperatura_C"] as DoubleDataFrameColumn;
                var colEntradaVentPID = dfPID.Columns["Entrada_Vent"] as DoubleDataFrameColumn;
                var colEntradaCalPID = dfPID.Columns["Entrada_Cal"] as DoubleDataFrameColumn;
                var colConsigna = dfPID.Columns["Consigna"] as DoubleDataFrameColumn;
                var colError = dfPID.Columns["Error"] as DoubleDataFrameColumn;

                if (colTiempo == null || colTemp == null || colConsigna == null)
                {
                    Debug.WriteLine("Error: columnas dfPID no encontradas.");
                    return;
                }

                // Append por columna (mantiene las longitudes sincronizadas)
                colTiempo.Append(tiempo);
                colTemp.Append(temperatura);
                colEntradaVentPID.Append(entradaVent);
                colEntradaCalPID.Append(entradaCal);
                colConsigna.Append(consigna);
                colError.Append(error);

                // Obtener recuento fiable usando la longitud de las columnas
                long filasTiempo = colTiempo.Length;
                long filasTemp = colTemp.Length;
                long filasConsigna = colConsigna.Length;
                long filas1 = Math.Min(filasTiempo, filasTemp);
                long filas = Math.Min(filas1, filasConsigna);

                Debug.WriteLine($"Fila añadida. Filas (por columna): Tiempo_s={filasTiempo}, Temperatura_C={filasTemp}, Consigna={filasConsigna}");

                // Mostrar la última fila añadida para verificar contenido
                if (filas > 0)
                {
                    var ultimoTiempo = colTiempo[filas - 1];
                    var ultimaTemp = colTemp[filas - 1];
                    var ultimaEntVent = colEntradaVentPID[filas - 1];
                    var ultimaEntCal = colEntradaCalPID[filas - 1];
                    var ultimaConsigna = colConsigna[filas - 1];
                    var ultimoError = colError[filas - 1];
                    Debug.WriteLine($"Última fila -> Tiempo_s: {ultimoTiempo}, Temperatura_C: {ultimaTemp}, EntradaVent: {ultimaEntVent}, EntradaCal: {ultimaEntCal}, Consigna: {ultimaConsigna}, Comando: {ultimoError}");
                }
            }

        }


        // Métodos para exportar los dataFrames a un archivo .csv
        private void buttonGuardarDatos_Click(object sender, EventArgs e)
        {
            if (checkCrtlManual.Checked)
            {
                checkCrtlManual.Checked = false; // Desactivar el modo manual para asegurar que se guarden todos los datos
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV|*.csv";
                    sfd.FileName = "datos_temperaturaCtrlManual.csv";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            SaveDataFrameToCsvMan(sfd.FileName);
                            MessageBox.Show("CSV guardado correctamente.", "Exportar CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error guardando CSV:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else if (checkPID.Checked)
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV|*.csv";
                    sfd.FileName = "datos_temperaturaCtrlPID.csv";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            SaveDataFrameToCsvPID(sfd.FileName);
                            MessageBox.Show("CSV guardado correctamente.", "Exportar CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error guardando CSV:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV|*.csv";
                    sfd.FileName = "datos_temperaturaCtrlAuto.csv";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            SaveDataFrameToCsvAuto(sfd.FileName);
                            MessageBox.Show("CSV guardado correctamente.", "Exportar CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error guardando CSV:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        private void SaveDataFrameToCsvMan(string path)
        {
            if (dfManual == null || dfManual.Columns.Count == 0) throw new InvalidOperationException("DataFrame vacío.");
            SaveDataFrameToCsv(dfManual, path);
        }
        private void SaveDataFrameToCsvAuto(string path)
        {
            if (dfAuto == null || dfAuto.Columns.Count == 0) throw new InvalidOperationException("DataFrame vacío.");
            SaveDataFrameToCsv(dfAuto, path);
        }
        private void SaveDataFrameToCsvPID(string path)
        {
            if (dfPID == null || dfPID.Columns.Count == 0) throw new InvalidOperationException("DataFrame vacío.");
            SaveDataFrameToCsv(dfPID, path);
        }
        private void SaveDataFrameToCsv(DataFrame df, string path)
        {
            if (df == null || df.Columns.Count == 0) throw new InvalidOperationException("DataFrame vacío.");

            var culture = CultureInfo.GetCultureInfo("es-ES");

            // Comprobar y obtener número de filas fiable (longitud mínima entre columnas)
            long rowCount = long.MaxValue;
            foreach (var c in df.Columns) rowCount = Math.Min(rowCount, c.Length);
            if (rowCount == long.MaxValue) rowCount = 0;

            Debug.WriteLine($"Guardando DataFrame a CSV: columnas={df.Columns.Count}, filas={rowCount}");
            foreach (var c in df.Columns) Debug.WriteLine($"Columna '{c.Name}' length={c.Length}");

            using (var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                // Cabecera con nombres de columna
                var header = string.Join(";", df.Columns.Select(c => EscapeCsv(c.Name ?? string.Empty)));
                sw.WriteLine(header);

                // Filas: iterar por índice y leer cada columna
                for (long i = 0; i < rowCount; i++)
                {
                    var values = new string[df.Columns.Count];
                    for (int j = 0; j < df.Columns.Count; j++)
                    {
                        object val = df.Columns[j][i];
                        string formatted;

                        if (val == null)
                        {
                            formatted = string.Empty;
                        }
                        else if (val is double d)
                        {
                            formatted = d.ToString("F2", culture);
                        }
                        else if (val is float f)
                        {
                            formatted = f.ToString("F2", culture);
                        }
                        else if (val is decimal m)
                        {
                            formatted = m.ToString("F2", culture);
                        }
                        else if (val is IFormattable formattable)
                        {
                            // Otros tipos numéricos (int, long, ...) -> formatear sin decimales
                            formatted = formattable.ToString(null, culture);
                        }
                        else
                        {
                            formatted = Convert.ToString(val, culture) ?? string.Empty;
                        }

                        values[j] = EscapeCsv(formatted);
                    }
                    sw.WriteLine(string.Join(";", values));
                }
            }
        }

        private string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool mustQuote = s.Contains(";") || s.Contains("\"") || s.Contains("\r") || s.Contains("\n");
            if (mustQuote) return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
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


        // Método de enviar datos al arduino
        private void EnviarDatos(string datos)
        {
            try
            {
                if (PuertoArduino != null && PuertoArduino.IsOpen)
                {
                    PuertoArduino.WriteLine(datos);
                    Debug.WriteLine($"Datos enviados: {datos}");
                }
                else
                {
                    Debug.WriteLine($"Puerto no abierto. No se envió: {datos}");
                }
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"IOException enviando datos '{datos}': {ioEx.Message}");
            }
            catch (InvalidOperationException invEx)
            {
                Debug.WriteLine($"InvalidOperationException enviando datos '{datos}': {invEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enviando datos '{datos}': {ex.Message}");
            }
        }

        // Método de recibir datos del arduino
        private void RecibirDatos(int i, float entradavent, float entradacal, float consigna, float error)
        {
            Debug.WriteLine($"Leyendo dato: {i}");
            PuertoArduino.WriteLine($"t{i}T");
            PuertoArduino.ReadTimeout = 200;
            try
            {
                string lectura = PuertoArduino.ReadLine().Trim();
                Debug.WriteLine($"Datos recibidos: {lectura}");

                // Extraer y parsear el dato de temperatura
                string numero = new string(lectura.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
                double temperatura = double.Parse(numero, CultureInfo.InvariantCulture);
                double tiempoActual = cronometro.Elapsed.TotalSeconds;

                Debug.WriteLine($"Tiempo: {tiempoActual}s, Temperatura: {temperatura}°C");
                Debug.WriteLine($"Consigna: {consigna}°C, Error: {error}°C");

                // Guardar datos en el DataFrame
                if (checkPID.Checked)
                    AppendRowToDataFramePID(tiempoActual, temperatura, entradavent, entradacal, consigna, error);

                // Añadir valores al gráfico de temperatura (Actualizar UI)
                tempChart.Invoke((MethodInvoker)delegate
                    {
                        Debug.WriteLine("Actualizando gráfico de temperatura...");
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

                // Recortar la serie de temperatura
                TrimTemperatureWindow();

                // Añadir valores al gráfico de entradas (Actualizar UI) (Para el modo de entradas del sistema)
                entradaChart.Invoke((MethodInvoker)delegate
                {
                    Debug.WriteLine("Actualizando gráfico de entradas...");
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

                // Recortar la serie de entradas
                TrimEntradaWindow();
            }
            catch (TimeoutException) { Debug.WriteLine("Timeout"); }
        }

        // Método de recibir datos para dos entradas de control automático
        private void RecibirDatos2(int i, float entradavent, float entradacal)
        {
            Debug.WriteLine($"Leyendo dato: {i}");
            PuertoArduino.WriteLine($"t{i}T");
            PuertoArduino.ReadTimeout = 200;

            try
            {
                string lectura = PuertoArduino.ReadLine().Trim();
                Debug.WriteLine($"Datos recibidos: {lectura}");

                // Extraer y parsear el dato de temperatura
                string numero = new string(lectura.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
                double temperatura = double.Parse(numero, CultureInfo.InvariantCulture);
                double tiempoActual = cronometro.Elapsed.TotalSeconds;

                Debug.WriteLine($"Tiempo: {tiempoActual}s, Temperatura: {temperatura}°C");

                // Guardar los datos en el dataFrame correspondiente
                if (checkCrtlManual.Checked)
                    AppendRowToDataFrameMan(tiempoActual, temperatura, entradavent, entradacal);
                else if (checkEscalon.Checked || checkRampa.Checked)
                    AppendRowToDataFrameAuto(tiempoActual, temperatura, entradavent, entradacal);

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
                    TrimTemperatureWindow();
                }

                // Añadir valores al gráfico de entradas (Actualizar UI) (Para el modo de entradas del sistema)
                // Solo mostrar la serie correspondiente a la entrada seleccionada
                if (!checkCrtlManual.Checked && (checkEscalon.Checked || checkRampa.Checked))
                {
                    if ((checkEscVent.Checked && !checkEscCal.Checked) || (checkRampVent.Checked && !checkRampCal.Checked))
                    {
                        entradaChart.Invoke((MethodInvoker)delegate
                        {
                            // Añadir los valores a las series
                            entradaVent.Add(entradavent);
                            tiempos.Add(tiempoActual);

                            // Ajustar eje Y dinámicamente
                            if (entradaVent.Count > 0)
                            {
                                double margen = Math.Max(5, entradaVent.Max() * 0.1); // 10% de margen o 5 unidades
                                entradaChart.AxisY[0].MinValue = Math.Floor(entradaVent.Min() - margen);
                                entradaChart.AxisY[0].MaxValue = Math.Ceiling(entradaVent.Max() + margen);
                            }
                        });
                    }
                    else if ((!checkEscVent.Checked && checkEscCal.Checked) || (!checkRampVent.Checked && checkRampCal.Checked))
                    {
                        entradaChart.Invoke((MethodInvoker)delegate
                        {
                            // Añadir los valores a las series
                            entradaCal.Add(entradacal);
                            tiempos.Add(tiempoActual);

                            // Ajustar eje Y dinámicamente
                            if (entradaCal.Count > 0)
                            {
                                double margen = Math.Max(5, entradaCal.Max() * 0.1); // 10% de margen o 5 unidades
                                entradaChart.AxisY[0].MinValue = Math.Floor(entradaCal.Min() - margen);
                                entradaChart.AxisY[0].MaxValue = Math.Ceiling(entradaCal.Max() + margen);
                            }
                        });
                    }
                    else
                    {
                        // Si están seleccionadas ambas entradas, añadir ambas
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
                    }
                }
                else
                {
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
                }
                // Recortar la serie de entradas si está activado el control manual
                if (checkCrtlManual.Checked)
                {
                    TrimEntradaWindow();
                }
            }
            catch (TimeoutException) { Debug.WriteLine("Timeout"); }
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

            Task.Run(async () =>
            {
                const int periodoMs = 1000;

                while (!token.IsCancellationRequested)
                {
                    var iterWatch = Stopwatch.StartNew();
                    try
                    {
                        // Ejecutar la lectura/control (bloqueante)
                        RecibirDatos2(1, ventilador.GetVelocidad(), calefactor.GetPotencia());
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // cancelado, salir
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error en StartModoManual iteration: {ex.Message}");
                    }
                    finally
                    {
                        iterWatch.Stop();
                        Debug.WriteLine("Iteración del loop manual completada");
                    }

                    // Esperar el resto del periodo respetando cancelación
                    int delay = periodoMs - (int)iterWatch.ElapsedMilliseconds;
                    if (delay > 0)
                    {
                        try
                        {
                            await Task.Delay(delay, token);
                        }
                        catch (TaskCanceledException) { break; }
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
                // Deshabilitar controles de control lazo cerrado
                checkPID.Enabled = false;
                buttonCargarLazoCerrado.Enabled = false;

                // Enviar valores iniciales
                ventilador.SetVelocidad((int)numericVelVent.Value);
                calefactor.SetPotencia((int)numericPotCal.Value);
                try 
                {
                    EnviarDatos($"v{ventilador.GetVelocidad().ToString()}V");
                    EnviarDatos($"n{calefactor.GetPotencia().ToString()}N");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error enviando datos iniciales: {ex.Message}");
                }


                try
                {
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

                    // Limpiar el dataFrame
                    InitializeDataFrameMan();

                    // Configurar el gráfico inicial
                    ConfigurarGraficoTempInicial();
                    ConfigurarGraficoEntradas2();
                }
                finally
                {
                    Debug.WriteLine("Gráfico actualizado");
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
                // Habilitar controles de control lazo cerrado
                checkPID.Enabled = true;
                buttonCargarLazoCerrado.Enabled = true;
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
                checkRampVent.Checked = false;
                checkRampCal.Checked = false;
                buttonCargarEntradas.Enabled = true;

                // Habilitar los controles de entrada escalón
                numericEscVentConsg.Enabled = true;
                numericEscVentTInicio.Enabled = true;
                numericEscCalConsg.Enabled = true;
                numericEscCalTInicio.Enabled = true;
                checkEscVent.Enabled = true;
                checkEscCal.Enabled = true;

                // Deshabilitar los controles de entrada rampa
                numericRampVentConsg.Enabled = false;
                numericRampVentTInicio.Enabled = false;
                numericRampVentTFinal.Enabled = false;
                numericRampCalConsg.Enabled = false;
                numericRampCalTInicio.Enabled = false;
                numericRampCalTFinal.Enabled = false;
                checkRampVent.Enabled = false;
                checkRampCal.Enabled = false;
            }
            else
            {
                // Deshabilitar los controles de entrada escalón
                numericEscVentConsg.Enabled = false;
                numericEscVentTInicio.Enabled = false;
                numericEscCalConsg.Enabled = false;
                numericEscCalTInicio.Enabled = false;
                checkEscVent.Enabled = false;
                checkEscCal.Enabled = false;
            }
        }

        private void checkRampa_CheckedChanged(object sender, EventArgs e)
        {
            if (checkRampa.Checked)
            {
                checkEscalon.Checked = false; // Desmarcar la entrada escalón si se selecciona rampa
                checkEscVent.Checked = false;
                checkEscCal.Checked = false;
                buttonCargarEntradas.Enabled = true;

                // Habilitar los controles de entrada rampa
                numericRampVentConsg.Enabled = true;
                numericRampVentTInicio.Enabled = true;
                numericRampVentTFinal.Enabled = true;
                numericRampCalConsg.Enabled = true;
                numericRampCalTInicio.Enabled = true;
                numericRampCalTFinal.Enabled = true;
                checkRampVent.Enabled = true;
                checkRampCal.Enabled = true;

                // Deshabilitar los controles de entrada escalón
                numericEscVentConsg.Enabled = false;
                numericEscVentTInicio.Enabled = false;
                numericEscCalConsg.Enabled = false;
                numericEscCalTInicio.Enabled = false;
                checkEscVent.Enabled = false;
                checkEscCal.Enabled = false;
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
                checkRampVent.Enabled = false;
                checkRampCal.Enabled = false;
            }
        }
        
        // Método para detener el modo de entradas continuas
        private void StopModoEntradas()
        {
            if (entradasModeCts != null)
            {
                try
                {
                    entradasModeCts.Cancel();
                    entradasModeCts.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cancelando loop de entradas: {ex.Message}");
                }
                finally
                {
                    entradasModeCts = null;
                }
            }
        }

        // Método para el control a través de entradas del sistema con el botón Cargar Entradas

        private async void buttonCargarEntradas_Click(object sender, EventArgs e)
        {
            // Si ya está ejecutándose, cancelar
            if (entradasModeCts != null)
            {
                StopModoEntradas();
                
                // Restaurar estado de la interfaz
                buttonCargarEntradas.Text = "Cargar Entradas";
                buttonCargarEntradas.Enabled = true;
                buttonGuardarDatos.Enabled = true;
                checkCrtlManual.Enabled = true;
                checkEscalon.Enabled = true;
                checkRampa.Enabled = true;
                checkEscVent.Enabled = true;
                checkEscCal.Enabled = true;
                checkRampVent.Enabled = true;
                checkRampCal.Enabled = true;
                numericEscVentConsg.Enabled = true;
                numericEscVentTInicio.Enabled = true;
                numericEscCalConsg.Enabled = true;
                numericEscCalTInicio.Enabled = true;
                numericRampVentConsg.Enabled = true;
                numericRampVentTInicio.Enabled = true;
                numericRampVentTFinal.Enabled = true;
                numericRampCalConsg.Enabled = true;
                numericRampCalTInicio.Enabled = true;
                numericRampCalTFinal.Enabled = true;
                
                return;
            }
            
            // Crear el CancellationTokenSource para permitir cancelación
            entradasModeCts = new CancellationTokenSource();
            var token = entradasModeCts.Token;
            
            // Cambiar el texto del botón
            buttonCargarEntradas.Text = "Cancelar";
            buttonGuardarDatos.Enabled = false;
            entradaChart.Visible = true;
            checkCrtlManual.Enabled = false;
            checkEscalon.Enabled = false;
            checkRampa.Enabled = false;
            checkEscVent.Enabled = false;
            checkEscCal.Enabled = false;
            checkRampVent.Enabled = false;
            checkRampCal.Enabled = false;

            // Deshabilitar los controles de entrada durante la operación
            numericEscVentConsg.Enabled = false;
            numericEscVentTInicio.Enabled = false;
            numericEscCalConsg.Enabled = false;
            numericEscCalTInicio.Enabled = false;
            numericRampVentConsg.Enabled = false;
            numericRampVentTInicio.Enabled = false;
            numericRampVentTFinal.Enabled = false;
            numericRampCalConsg.Enabled = false;
            numericRampCalTInicio.Enabled = false;
            numericRampCalTFinal.Enabled = false;

            tempChart.Size = new Size(900, 450);

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

                // Limpiar el dataFrame
                InitializeDataFrameAuto();

                // Configurar el gráfico inicial
                ConfigurarGraficoTempInicial();
                ConfigurarGraficoEntradas2();

                if (checkEscalon.Checked) // Se ha seleccionado la entrada escalón
                {
                    Debug.WriteLine("Se ha seleccionado la entrada escalón");

                    if (checkEscVent.Checked && !checkEscCal.Checked) // Solo entrada escalón para el ventilador
                    {
                        Debug.WriteLine("Entrada escalón para el Ventilador");
                        ventilador.SetVelocidad(40);

                        int valorPreEsc = (int)numericEscVentTInicio.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");

                        for (int i = 0; i < valorPreEsc; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura previa al escalón: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada escalón
                        ventilador.SetVelocidad((int)numericEscVentConsg.Value);
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - (int)numericEscVentTInicio.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPostEsc; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            try
                            {
                                // Ejecutar la lectura en un hilo de fondo
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura posterior al escalón: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }
                    }
                    else if (!checkEscVent.Checked && checkEscCal.Checked) // Solo entrada escalón para el calefactor
                    {
                        Debug.WriteLine("Entrada escalón para el Calefactor");
                        calefactor.SetPotencia(0);

                        int valorPreEsc = (int)numericEscCalTInicio.Value;
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPreEsc; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura previa al escalón: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        calefactor.SetPotencia((int)numericEscCalConsg.Value);
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - (int)numericEscCalTInicio.Value;
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPostEsc; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura posterior al escalón: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }
                    }
                    else if (checkEscVent.Checked && checkEscCal.Checked) // Entradas escalón para ambos componentes
                    {
                        Debug.WriteLine("Entrada escalón para ambos componentes");
                        // Valores iniciales antes de la entrada escalón
                        ventilador.SetVelocidad(40);
                        calefactor.SetPotencia(0);

                        // Iniciar la recepción de datos
                        int valorPreEsc = Math.Min((int)numericEscVentTInicio.Value, (int)numericEscCalTInicio.Value);
                        Debug.WriteLine($"Tiempo previo al escalón: {valorPreEsc}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPreEsc; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura previa al escalón: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        if ((int)numericEscVentTInicio.Value < (int)numericEscCalTInicio.Value)
                        {
                            ventilador.SetVelocidad((int)numericEscVentConsg.Value);
                            int valorFirstEsc = (int)numericEscCalTInicio.Value - (int)numericEscVentTInicio.Value;

                            // Ejecutar lecturas una vez por segundo de forma asíncrona
                            for (int i = 0; i < valorFirstEsc; i++)
                            {
                                token.ThrowIfCancellationRequested();
                                
                                try
                                {
                                    await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                    Debug.WriteLine("Fin de RecibirDatos");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error en lectura: {ex}");
                                }

                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                            calefactor.SetPotencia((int)numericEscCalConsg.Value);
                        }
                        else if ((int)numericEscVentTInicio.Value > (int)numericEscCalTInicio.Value)
                        {
                            calefactor.SetPotencia((int)numericEscCalConsg.Value);
                            int valorFirstEsc = (int)numericEscVentTInicio.Value - (int)numericEscCalTInicio.Value;

                            // Ejecutar lecturas una vez por segundo de forma asíncrona
                            for (int i = 0; i < valorFirstEsc; i++)
                            {
                                token.ThrowIfCancellationRequested();
                                
                                try
                                {
                                    await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                    Debug.WriteLine("Fin de RecibirDatos");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error en lectura: {ex}");
                                }

                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                            ventilador.SetVelocidad((int)numericEscVentConsg.Value);
                        }
                        else // Si ambos tiempos de inicio son iguales
                        {
                            ventilador.SetVelocidad((int)numericEscVentConsg.Value);
                            calefactor.SetPotencia((int)numericEscCalConsg.Value);
                        }

                        // Iniciar la recepción de datos
                        int valorPostEsc = (int)numericTEjecucionAuto.Value + 1 - Math.Max((int)numericEscVentTInicio.Value, (int)numericEscCalTInicio.Value);
                        Debug.WriteLine($"Tiempo posterior al escalón: {valorPostEsc}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPostEsc; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura posterior al escalón: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }
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
                        ventilador.SetVelocidad(40);
                        calefactor.SetPotencia(0);

                        // Iniciar la recepción de datos
                        int valorPreRamp = (int)numericRampVentTInicio.Value;
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPreRamp; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura previa a la rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            float valorMidRamp = 40 + (i * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value)));
                            ventilador.SetVelocidad((int)valorMidRamp);
                            // Ejecutar lecturas una vez por segundo de forma asíncrona
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura de rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        ventilador.SetVelocidad((int)numericRampVentConsg.Value);
                        // Iniciar la recepción de datos
                        int valorPostRamp = (int)numericTEjecucionAuto.Value + 1 - (int)numericRampVentTFinal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPostRamp; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura posterior a la rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }
                    }
                    else if (!checkRampVent.Checked && checkRampCal.Checked) // Entrada rampa para el calefactor
                    {
                        Debug.WriteLine("Entrada rampa para el Calefactor");
                        // Valores iniciales antes de la entrada rampa
                        ventilador.SetVelocidad(40);
                        calefactor.SetPotencia(0);

                        // Iniciar la recepción de datos
                        int valorPreRamp = (int)numericRampCalTInicio.Value;
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPreRamp; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura previa a la rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // Cálculo y envío de los valores de la rampa
                        for (int i = 0; i < (int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            float valorMidRamp = i * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value));
                            calefactor.SetPotencia((int)valorMidRamp);
                            // Ejecutar lecturas una vez por segundo de forma asíncrona
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura de rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para la entrada rampa
                        calefactor.SetPotencia((int)numericRampCalConsg.Value);
                        // Iniciar la recepción de datos
                        int valorPostRamp = (int)numericTEjecucionAuto.Value + 1 - (int)numericRampCalTFinal.Value;
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPostRamp; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura posterior a la rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }
                    }
                    else if (checkRampVent.Checked && checkRampCal.Checked) // Entrada rampa para ambos componentes
                    {
                        Debug.WriteLine("Entrada rampa para ambos componentes");
                        // Valores iniciales antes de la entrada rampa
                        ventilador.SetVelocidad(40);
                        calefactor.SetPotencia(0);

                        // Iniciar la recepción de datos
                        int valorPreRamp = Math.Min((int)numericRampVentTInicio.Value, (int)numericRampCalTInicio.Value);
                        Debug.WriteLine($"Tiempo previo a la rampa: {valorPreRamp}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPreRamp; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura previa a la rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // Enviar el primer valor de rampa
                        if ((int)numericRampVentTInicio.Value < (int)numericRampCalTInicio.Value) // Se envía primero el del ventilador
                        {
                            int j = 0;
                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampVentTInicio.Value; i++)
                            {
                                token.ThrowIfCancellationRequested();
                                
                                float valorMidRampVent = Math.Min(40 + (i * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (float)numericRampVentConsg.Value);
                                ventilador.SetVelocidad((int)valorMidRampVent);

                                // Antes de que se alcance el tiempo de inicio de la rampa del calefactor, su valor se mantiene en 0
                                if (cronometro.Elapsed.TotalSeconds < (int)numericRampCalTInicio.Value + 1)
                                {
                                    int valorMidRampCal = 0;
                                    calefactor.SetPotencia(valorMidRampCal);

                                    // Ejecutar lecturas una vez por segundo de forma asíncrona
                                    try
                                    {
                                        // Ejecutar la lectura en un hilo de fondo
                                        await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()));
                                        Debug.WriteLine("Fin de RecibirDatos");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error en lectura previa a la rampa: {ex}");
                                        // Opcional: break; para abortar si hay error persistente
                                    }

                                    // Esperar 1 segundo antes de la siguiente lectura
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                }
                                // Cuando el tiempo de inicio de la rampa del calefactor se alcanza, se empieza a incrementar su valor
                                else
                                {
                                    j++;
                                    float valorMidRampCal = Math.Min(j * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (float)numericRampCalConsg.Value);
                                    calefactor.SetPotencia((int)valorMidRampCal);
                                }

                                    // Ejecutar lecturas una vez por segundo de forma asíncrona
                                try
                                {
                                    await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                    Debug.WriteLine("Fin de RecibirDatos");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error en lectura de rampa: {ex}");
                                }

                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                        }
                        else if ((int)numericRampVentTInicio.Value > (int)numericRampCalTInicio.Value)
                        {
                            int j = 0;
                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampCalTInicio.Value; i++)
                            {
                                token.ThrowIfCancellationRequested();
                                
                                float valorMidRampCal = Math.Min(i * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (float)numericRampCalConsg.Value);
                                calefactor.SetPotencia((int)valorMidRampCal);

                                // Antes de que se alcance el tiempo de inicio de la rampa del ventilador, su valor se mantiene en 40
                                if (cronometro.Elapsed.TotalSeconds < (int)numericRampVentTInicio.Value + 1)
                                {
                                    int valorMidRampVent = 40;
                                    ventilador.SetVelocidad(valorMidRampVent);

                                    // Ejecutar lecturas una vez por segundo de forma asíncrona
                                    try
                                    {
                                        // Ejecutar la lectura en un hilo de fondo
                                        await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()));
                                        Debug.WriteLine("Fin de RecibirDatos");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error en lectura previa a la rampa: {ex}");
                                        // Opcional: break; para abortar si hay error persistente
                                    }

                                    // Esperar 1 segundo antes de la siguiente lectura
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                }
                                // Cuando el tiempo de inicio de la rampa del ventilador se alcanza, se empieza a incrementar su valor
                                else
                                {
                                    j++;
                                    float valorMidRampVent = Math.Min(40 + (j * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (float)numericRampVentConsg.Value);
                                    ventilador.SetVelocidad((int)valorMidRampVent);
                                }

                                    // Ejecutar lecturas una vez por segundo de forma asíncrona
                                try
                                {
                                    await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                    Debug.WriteLine("Fin de RecibirDatos");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error en lectura de rampa: {ex}");
                                }

                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                        }
                        else // Si ambos tiempos de inicio son iguales
                        {
                            // Se itera hasta el tiempo final de la rampa que termine más tarde y se controla
                            // internamente el valor de cada rampa para no superar el valor de consigna
                            for (int i = 0; i < Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value) - (int)numericRampVentTInicio.Value; i++)
                            {
                                token.ThrowIfCancellationRequested();
                                
                                float valorMidRampVent = Math.Min(40 + (i * (((float)numericRampVentConsg.Value - 40) / ((int)numericRampVentTFinal.Value - (int)numericRampVentTInicio.Value))), (float)numericRampVentConsg.Value);
                                ventilador.SetVelocidad((int)valorMidRampVent);
                                float valorMidRampCal = Math.Min(i * ((float)numericRampCalConsg.Value / ((int)numericRampCalTFinal.Value - (int)numericRampCalTInicio.Value)), (float)numericRampCalConsg.Value);
                                calefactor.SetPotencia((int)valorMidRampCal);

                                // Ejecutar lecturas una vez por segundo de forma asíncrona
                                try
                                {
                                    await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                    Debug.WriteLine("Fin de RecibirDatos");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error en lectura de rampa: {ex}");
                                }

                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                        }

                        // Ajustar los valores enviados a los seleccionados por el usuario para cada entrada rampa
                        ventilador.SetVelocidad((int)numericRampVentConsg.Value);
                        calefactor.SetPotencia((int)numericRampCalConsg.Value);

                        // Envío de valores finales de rampa
                        int valorPostRamp = (int)numericTEjecucionAuto.Value + 1 - Math.Max((int)numericRampVentTFinal.Value, (int)numericRampCalTFinal.Value);
                        Debug.WriteLine($"Tiempo posterior a la rampa: {valorPostRamp}");

                        // Ejecutar lecturas una vez por segundo de forma asíncrona
                        for (int i = 0; i < valorPostRamp; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            try
                            {
                                await Task.Run(() => RecibirDatos2(i, ventilador.GetVelocidad(), calefactor.GetPotencia()), token);
                                Debug.WriteLine("Fin de RecibirDatos");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error en lectura posterior a la rampa: {ex}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }
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
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Ejecución de entradas cancelada por el usuario");
                MessageBox.Show("Ejecución cancelada", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ejecución de entradas: {ex}");
                MessageBox.Show($"Se produjo un error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Debug.WriteLine("Bloque finally modo de entradas...");
                if (entradasModeCts != null)
                {
                    try
                    {
                        Debug.WriteLine("Limpiando CancellationTokenSource en finally");
                        entradasModeCts.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing CancellationTokenSource: {ex.Message}");
                    }
                }
                // Rehabilitar botones al finalizar
                buttonCargarEntradas.Text = "Cargar Entradas";
                buttonCargarEntradas.Enabled = true;
                buttonGuardarDatos.Enabled = true;
                checkCrtlManual.Enabled = true;
                checkEscalon.Enabled = true;
                checkRampa.Enabled = true;
                checkEscVent.Enabled = true;
                checkEscCal.Enabled = true;
                checkRampVent.Enabled = true;
                checkRampCal.Enabled = true;

                numericEscVentConsg.Enabled = true;
                numericEscVentTInicio.Enabled = true;
                numericEscCalConsg.Enabled = true;
                numericEscCalTInicio.Enabled = true;
                numericRampVentConsg.Enabled = true;
                numericRampVentTInicio.Enabled = true;
                numericRampVentTFinal.Enabled = true;
                numericRampCalConsg.Enabled = true;
                numericRampCalTInicio.Enabled = true;
                numericRampCalTFinal.Enabled = true;
            }
        }

        // Método para ocultar o mostrar el gráfico de entradas
        private void buttonOcultar_Click(object sender, EventArgs e)
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

                // Limpiar los dataFrame
                InitializeDataFrameMan();
                InitializeDataFrameAuto();
                InitializeDataFramePID();

                // Configurar el gráfico inicial
                ConfigurarGraficoTempInicial();
                ConfigurarGraficoEntradas();
                ConfigurarGraficoEntradas2();
            }
            catch (Exception) { }

            cronometro.Restart();
        }

        // Control de Entradas en Lazo Cerrado Mediante un Controlador PID
        // Método para iniciar el modo PID continuo
        private void StartModoPID(float Kp, float Ki, float Kd, double consigna)
        {
            // Cancelar cualquier loop previo por seguridad
            StopModoPID();

            pidModeCts = new CancellationTokenSource();
            var token = pidModeCts.Token;

            // Asegurar que el cronómetro está en marcha
            cronometro.Restart();

            // Variables del PID
            float errorPrev = 0;
            float integralTotal = 0f;
            const float outMin = 0f;
            const float outMax = 85f;

            Task.Run(async () =>
            {
                const int periodoMs = 1000; // 1 segundo por iteración
                float dtSec = periodoMs / 1000f;

                while (!token.IsCancellationRequested)
                {
                    var iterWatch = Stopwatch.StartNew();

                    float temperatura = 0;
                    float error = 0;
                    int comando = 0;

                    try
                    {
                        // Leer temperatura del Arduino
                        PuertoArduino.WriteLine("t1T");
                        PuertoArduino.ReadTimeout = 200;

                        try
                        {
                            string lectura = PuertoArduino.ReadLine().Trim();
                            Debug.WriteLine($"Datos recibidos: {lectura}");

                            // Extraer y parsear el dato de temperatura
                            string numero = new string(lectura.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
                            temperatura = float.Parse(numero, CultureInfo.InvariantCulture);
                            Debug.WriteLine($"Temperatura actual: {temperatura}°C");

                            // Calcular el comando usando la función dedicada
                            comando = CalculoComando(Kp, Ki, Kd, (float)consigna, temperatura,ref errorPrev, ref integralTotal, dtSec, outMin, outMax);

                            // Obtener el error actualizado para el DataFrame
                            error = (float)consigna - temperatura;

                            calefactor.SetPotencia(comando);
                            Debug.WriteLine($"Comando enviado al calefactor: {comando}");
                        }
                        catch (TimeoutException)
                        {
                            Debug.WriteLine("Timeout leyendo temperatura");
                        }

                        // Actualizar gráficos y DataFrame
                        RecibirDatos(1, ventilador.GetVelocidad(), calefactor.GetPotencia(), (float)consigna, error);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // cancelado, salir
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error en StartModoPID iteration: {ex.Message}");
                    }
                    finally
                    {
                        iterWatch.Stop();
                        Debug.WriteLine("Iteración del loop PID completada");
                    }

                    // Esperar el resto del periodo respetando cancelación
                    int delay = periodoMs - (int)iterWatch.ElapsedMilliseconds;
                    if (delay > 0)
                    {
                        try
                        {
                            await Task.Delay(delay, token);
                        }
                        catch (TaskCanceledException) { break; }
                    }
                }
            }, token);
        }

        // Función para calcular el comando del controlador PID
        private int CalculoComando(float Kp, float Ki, float Kd, float consigna, float temperatura, ref float errorPrev, ref float integralTotal, float dtSec, float outMin, float outMax)
        {
            // Cálculo del error
            float error = consigna - temperatura;
            Debug.WriteLine($"Error actual: {error}");

            // Cálculo de la integral del error
            integralTotal += error * dtSec;

            // Anti-windup: limitar integral
            if (Math.Abs(Ki) > 1e-6f)
            {
                float integralLimit = outMax / Math.Abs(Ki);
                integralTotal = Math.Max(-integralLimit, Math.Min(integralTotal, integralLimit));
            }

            // Calcular componentes del PID
            float gananciaProporcional = Kp * error;
            float gananciaIntegral = Ki * integralTotal;
            float gananciaDerivativa = Kd * (error - errorPrev) / dtSec;

            errorPrev = error;

            // Salida sin saturar (valor calculado por PID)
            float comandoRaw = gananciaProporcional + gananciaIntegral + gananciaDerivativa;

            // Se limita el comando al rango permitido (0-85) y se envía al calefactor
            int comando = (int)Math.Round(Math.Max(outMin, Math.Min(comandoRaw, outMax)));

            return comando;
        }

        // Método para detener el modo PID continuo
        private void StopModoPID()
        {
            if (pidModeCts != null)
            {
                try
                {
                    pidModeCts.Cancel();
                    pidModeCts.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cancelando loop PID: {ex.Message}");
                }
                finally
                {
                    pidModeCts = null;
                }
            }
        }
        private void buttonCargarLazoCerrado_Click(object sender, EventArgs e)
        {
            // Si ya está en modo PID, detenerlo
            if (pidModeCts != null)
            {
                StopModoPID();

                // Restaurar estado de la interfaz
                checkKp.Enabled = true;
                checkKi.Enabled = true;
                checkKd.Enabled = true;
                buttonCargarLazoCerrado.Text = "Cargar Control PID";
                buttonGuardarDatos.Enabled = true;
                buttonCargarEntradas.Enabled = true;
                checkCrtlManual.Enabled = true;

                return;
            }

            // Deshabilitar controles durante la operación
            checkKp.Enabled = false;
            checkKi.Enabled = false;
            checkKd.Enabled = false;
            checkCrtlManual.Checked = false;
            checkCrtlManual.Enabled = false;
            entradaChart.Visible = true;
            buttonCargarLazoCerrado.Text = "Detener Control PID";
            buttonCargarEntradas.Enabled = false;
            buttonGuardarDatos.Enabled = false;

            tempChart.Size = new Size(900, 450);

            // Limpiar gráficos
            tempChart.Series.Clear();
            temperaturas.Clear();
            tiempos.Clear();
            tempChart.Update(true, true);
            Debug.WriteLine("Gráfico actualizado");

            entradaChart.Series.Clear();
            entradas.Clear();
            entradaCal.Clear();
            entradaVent.Clear();
            tiempos.Clear();
            entradaChart.Update(true, true);
            Debug.WriteLine("Gráfico de entradas actualizado");

            // Limpiar el dataFrame
            InitializeDataFramePID();

            // Configurar el gráfico inicial
            ConfigurarGraficoTempInicial();

            // Crear o actualizar la línea horizontal de la consigna
            double consigna = (double)numericConsgTemp.Value;
            setpointSection = new AxisSection
            {
                Value = consigna,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2,
                SectionWidth = 0
            };

            tempChart.AxisY[0].Sections = new SectionsCollection { setpointSection };

            ConfigurarGraficoEntradas2();

            cronometro.Restart();
            ventilador.SetVelocidad(50);

            try
            {
                // Determinar qué tipo de control se ha seleccionado
                float Kp = checkKp.Checked ? (float)numericKp.Value : 0f;
                float Ki = checkKi.Checked ? (float)numericKi.Value : 0f;
                float Kd = checkKd.Checked ? (float)numericKd.Value : 0f;

                if (!checkKp.Checked && !checkKi.Checked && !checkKd.Checked)
                {
                    MessageBox.Show("Seleccione al menos un tipo de control (P, I o D)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Restaurar estado
                    checkKp.Enabled = true;
                    checkKi.Enabled = true;
                    checkKd.Enabled = true;
                    buttonCargarLazoCerrado.Text = "Cargar Control PID";
                    checkCrtlManual.Enabled = true;
                    buttonCargarEntradas.Enabled = true;
                    buttonGuardarDatos.Enabled = true;

                    return;
                }

                // Iniciar el control PID continuo
                StartModoPID(Kp, Ki, Kd, consigna);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error iniciando control PID: {ex}");
                MessageBox.Show($"Se produjo un error al iniciar el control PID:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Restaurar estado de la interfaz
                checkKp.Enabled = true;
                checkKi.Enabled = true;
                checkKd.Enabled = true;
                buttonCargarLazoCerrado.Text = "Cargar Control PID";
                checkCrtlManual.Enabled = true;
                buttonCargarEntradas.Enabled = true;
                buttonGuardarDatos.Enabled = true;
            }
        }
    }
}
