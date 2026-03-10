# Uso de la Aplicación

## Inicio

1. Ejecutar `InterfazPlantaCtrlTemp.exe`
2. Seleccionar el puerto USB correspondiente
3. Pulsar **Conectar**
4. Configurar el tiempo de ejecución

## Control Manual

Permite controlar directamente los actuadores modificando las barras de selección.

Ventilador:
- Rango de operación: 40% – 100%

Calefactor:
- Rango de operación: 0% – 85%

```{image} images/manual.png
:width: 600px
:align: center
```

## Control Automático

Permite aplicar señales de entrada simples al sistema.

Tipos de señal:

- Escalón
- Rampa

Parámetros configurables:

- Consigna
- Tiempo inicial
- Tiempo final (solo rampa)


```{image} images/entradas.png
:width: 600px
:align: center
```

## Control PID

Permite controlar la temperatura en lazo cerrado.

Parámetros:

- Kp
- Ki
- Kd
- Consigna de temperatura


```{image} images/PID.png
:width: 600px
:align: center
```
