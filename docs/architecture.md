# Arquitectura del Proyecto

## Estructura del repositorio
```
TFG-Interfaz-en-CSharp-para-control-de-maqueta-de-temperatura/
├── InterfazPlantaCtrlTemp/        # Aplicación de escritorio en C#
│   ├── Form1.cs                   # Lógica principal de la interfaz
│   ├── Form1.Designer.cs          # Diseño del formulario
│   ├── Ventilador.cs              # Control del ventilador
│   ├── Calefactor.cs              # Control del calefactor
│   ├── Program.cs                 # Punto de entrada de la aplicación
│   └── Resources/                 # Recursos gráficos
│
├── todov5/                        # Firmware de Arduino
│   └── todov5.ino
```
## Componentes principales

### Interfaz gráfica

Implementada en **Windows Forms**, permite:

- Visualizar la temperatura en tiempo real
- Controlar los actuadores
- Configurar experimentos

### Comunicación con Arduino

La comunicación se realiza mediante puerto serie USB a **115200 baudios**.

### Protocolo de comandos

| Comando | Descripción | Ejemplo |
|--------|-------------|--------|
| vXXV | Velocidad ventilador | v75V |
| nXXN | Potencia calefactor | n50N |
| tXT | Solicitud de temperatura | t1T |

## Pines de Arduino

| Pin | Función |
|----|----|
| A0 | Sensor ventilador |
| A1 | Sensor calefactor |
| A2 | Sensor PT100 |
| D8 | Conmutador de modo |
| D9 | PWM ventilador |
| D10 | PWM calefactor |
