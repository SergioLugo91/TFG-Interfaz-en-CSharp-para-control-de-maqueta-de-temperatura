# Interfaz Gráfica para Control de Maqueta de Temperatura

![C#](https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)
![Arduino](https://img.shields.io/badge/Arduino-00979D?style=flat&logo=arduino&logoColor=white)
![Windows Forms](https://img.shields.io/badge/Windows%20Forms-0078D4?style=flat&logo=windows&logoColor=white)

## 📋 Descripción

Este proyecto consiste en el desarrollo de una **interfaz gráfica en C# (Windows Forms)** para el control de una maqueta de control de temperatura utilizada en prácticas de laboratorio.

La interfaz permite:
- Manejar dos actuadores (**ventilador** y **resistencia calefactora**) conectados a una placa Arduino a través de comunicación serie por USB
- Visualizar en tiempo real la temperatura registrada por un sensor PT100
- Aplicar señales de entrada tipo **escalón** o **rampa**
- Observar la respuesta del sistema en gráficos dinámicos
- Implementar control en lazo cerrado mediante controlador **PID**

---

## ✨ Características

### Modos de Control
- **Control Manual**: Ajuste directo de los valores del ventilador (40-100%) y calefactor (0-85%)
- **Control Automático**: Aplicación de señales de entrada tipo escalón o rampa con tiempos configurables
- **Control PID**: Control en lazo cerrado con ajuste de parámetros Kp, Ki y Kd

### Visualización
- Gráficos en tiempo real de temperatura y señales de entrada usando **LiveCharts**
- Exportación de datos a formato CSV para análisis posterior
- Ventana de visualización configurable

### Comunicación
- Comunicación serie por USB con Arduino a 115200 baudios
- Detección automática de puertos serie disponibles
- Protocolo de comunicación simple y robusto

---

## 📁 Estructura del Proyecto

```
TFG-Interfaz-en-CSharp-para-control-de-maqueta-de-temperatura/
├── InterfazPlantaCtrlTemp/          # Código fuente de la aplicación C#
│   ├── Form1.cs                     # Formulario principal y lógica de la interfaz
│   ├── Form1.Designer.cs            # Diseño de la interfaz gráfica
│   ├── Ventilador.cs                # Clase para el control del ventilador
│   ├── Calefactor.cs                # Clase para el control del calefactor
│   ├── Program.cs                   # Punto de entrada de la aplicación
│   ├── InterfazPlantaCtrlTemp.sln   # Solución de Visual Studio
│   ├── InterfazPlantaCtrlTemp.csproj # Proyecto de C#
│   ├── Resources/                   # Recursos gráficos
│   └── bin/                         # Ejecutables compilados
├── todov5/                          # Código Arduino
│   └── todov5.ino                   # Firmware para la placa Arduino
└── README.md                        # Este archivo
```

---

## 🔧 Requisitos

### Software
- **Visual Studio 2019** o superior (con soporte para .NET Framework)
- **.NET Framework 4.7.2**
- **Arduino IDE** (para cargar el firmware en la placa)

### Hardware
- Placa **Arduino** (compatible con comunicación serie)
- Maqueta de control de temperatura con:
  - Ventilador controlado por PWM
  - Resistencia calefactora (Nicrom) controlada por PWM
  - Sensor de temperatura PT100 con amplificador INA
  - Conmutador para selección de modo (USB/Voltaje)

### Dependencias NuGet
- `LiveCharts` y `LiveCharts.WinForms` - Gráficos en tiempo real
- `Microsoft.Data.Analysis` - Manejo de DataFrames para almacenamiento de datos
- `Apache.Arrow` - Soporte para estructuras de datos columnar

---

## 🚀 Instalación

### 1. Clonar el repositorio
```bash
git clone https://github.com/SergioLugo91/TFG-Interfaz-en-CSharp-para-control-de-maqueta-de-temperatura.git
```

### 2. Configurar el Arduino
1. Abrir el archivo `todov5/todov5.ino` en Arduino IDE
2. Conectar la placa Arduino al ordenador
3. Seleccionar el puerto y tipo de placa correctos
4. Cargar el sketch en la placa

### 3. Compilar la aplicación
1. Abrir `InterfazPlantaCtrlTemp/InterfazPlantaCtrlTemp.sln` en Visual Studio
2. Restaurar los paquetes NuGet (automático o mediante `Herramientas > Administrador de paquetes NuGet`)
3. Compilar la solución (`Ctrl + Shift + B`)

---

## 📖 Uso de la Aplicación

### Inicio
1. Asegurarse de que la maqueta tiene cargado el código Arduino correcto (`todov5.ino`)
2. Ejecutar el archivo de la interfaz (`InterfazPlantaCtrlTemp.exe`)
3. Seleccionar el puerto USB correspondiente y pulsar **Conectar**
4. Configurar el tiempo de ejecución deseado

### Control Manual
1. Activar la casilla **Control Manual**
2. Ajustar los valores de los actuadores:
   - **Ventilador**: Usar la barra deslizante o la casilla numérica (40-100%)
   - **Calefactor**: Usar la barra deslizante o la casilla numérica (0-85%)
3. Los cambios se aplican automáticamente en tiempo real

### Control Automático (Entradas del Sistema)
1. Seleccionar el tipo de entrada: **Escalón** o **Rampa**
2. Seleccionar el actuador a controlar (Ventilador, Calefactor o ambos)
3. Configurar los parámetros:
   - **Consigna**: Valor objetivo del actuador
   - **Tiempo inicial**: Momento de inicio de la señal
   - **Tiempo final** (solo rampa): Momento en que la señal alcanza la consigna
4. Pulsar el botón **Cargar** para iniciar el experimento

### Control PID (Lazo Cerrado)
1. Seleccionar los parámetros del controlador (Kp, Ki, Kd)
2. Establecer la consigna de temperatura deseada
3. Pulsar **Cargar** para iniciar el control en lazo cerrado

### Guardar Datos
- Pulsar el botón **Guardar Datos** para exportar los resultados a un archivo CSV

---

## ⚙️ Detalles Técnicos

### Comunicación Serie
El protocolo de comunicación entre la interfaz y Arduino utiliza comandos simples:

| Comando | Descripción | Ejemplo |
|---------|-------------|---------|
| `vXXV` | Establecer velocidad del ventilador (40-100%) | `v75V` |
| `nXXN` | Establecer potencia del calefactor (0-85%) | `n50N` |
| `tXT` | Solicitar lectura de temperatura | `t1T` |

### Pines Arduino
| Pin | Función |
|-----|---------|
| A0 | Entrada sensor ventilador (modo voltaje) |
| A1 | Entrada sensor Nicrom (modo voltaje) |
| A2 | Sensor de temperatura PT100 |
| D8 | Conmutador de modo (USB/Voltaje) |
| D9 | Salida PWM Ventilador |
| D10 | Salida PWM Calefactor |

### Límites de Seguridad
- **Ventilador**: Mínimo 40%, Máximo 100%
- **Calefactor**: Mínimo 0%, Máximo 85%

---

## 📊 Exportación de Datos

Los datos se exportan en formato CSV con las siguientes columnas:
- `Tiempo_s`: Tiempo en segundos desde el inicio del experimento
- `Temperatura_C`: Temperatura medida en grados Celsius
- `Entrada_Vent`: Valor de entrada del ventilador (%)
- `Entrada_Cal`: Valor de entrada del calefactor (%)
- `Consigna` (solo modo PID): Valor de temperatura objetivo

---

## 👤 Autor

**Sergio Lugo**

Este proyecto es un Trabajo Fin de Grado (TFG) desarrollado para prácticas de laboratorio de sistemas de control.

---

## 📄 Licencia

Este proyecto está disponible para uso educativo y académico.
