Nombre del proyecto:

Interfaz gráfica para el control para Control de una maqueta de control de temperatura

Descripción:

Este proyecto consiste en el desarrollo de una interfaz gráfica en C# (Windows Forms) para el control de una maqueta de  control 
de temperatura utilizada en prácticas de laboratorio. La interfaz permite manejar dos actuadores (ventilador y resistencia calefactora) 
conectados a una placa Arduino a través de comunicación serie por USB. También permite visualizar en tiempo real la temperatura registrada 
por un sensor, aplicar señales de entrada tipo escalón o rampa, y observar la respuesta del sistema.

Instalación:

No es necesario instalar ningún software adicional. Para usar la aplicación basta con descargar y ejecutar directamente el archivo ejecutable:

1. Descargar el archivo InterfazPlantaCtrlTemp.exe desde la sección de releases del repositorio.
2. Ejecutar el archivo InterfazPlantaCtrlTemp.exe con doble clic.

Uso de la aplicación:

1. Asegurarse de que la maqueta tiene el código arduino correcto (todov5.ino)
2. Ejecutar el archivo de la interfaz (InterfazPlantaCtrlTemp.exe)
3. Dentro de la interfaz establecer la conexión con la maqueta seleccionando el puerto USB correspondiente.
4. Cambiar el tiempo de ejecución al deseado por el usuario.
5. Modo de control manual:
6. Cambiar los valores de los actuadores haciendo uso de las barras o las casillas numéricas.
7. Pulsar el botón de Cargar situado en la esquina inferior derecha del grupo Control de la Maqueta.
8. Modo de control automático/entradas del sistema:
9. Seleccionar el tipo de entrada entre escalón y rampa.
10. Seleccionar el actuador a controlar.
11. Asignar valores a la consigna, al tiempo inicial y en caso de entrada rampa al tiempo final.
12. Pulsar el botón Cargar situado en la esquina inferior derecha del grupo Control de Entradas del Sistema.
