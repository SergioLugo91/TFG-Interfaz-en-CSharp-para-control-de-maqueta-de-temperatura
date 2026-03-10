# Uso de la Aplicación

## Inicio

1. Ejecutar `InterfazPlantaCtrlTemp.exe`
2. Seleccionar el puerto USB correspondiente
3. Pulsar **Conectar**
4. Configurar el tiempo de ejecución

## Control Manual

Permite controlar directamente los actuadores.

Ventilador:
- Rango de operación: 40% – 100%

Calefactor:
- Rango de operación: 0% – 85%

Los cambios se aplican en tiempo real.

## Control Automático

Permite aplicar señales de entrada al sistema.

Tipos de señal:

- Escalón
- Rampa

Parámetros configurables:

- Consigna
- Tiempo inicial
- Tiempo final (solo rampa)

## Control PID

Permite controlar la temperatura en lazo cerrado.

Parámetros:

- Kp
- Ki
- Kd
- Consigna de temperatura

## Exportación de datos

Los datos experimentales pueden guardarse en formato CSV.

Columnas exportadas:

- Tiempo_s
- Temperatura_C
- Entrada_Vent
- Entrada_Cal
- Consigna
