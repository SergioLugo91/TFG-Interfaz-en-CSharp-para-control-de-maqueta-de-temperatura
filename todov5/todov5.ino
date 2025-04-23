
const int PinEntradaVentilador = A0;   // Seleccionar la entrada para el sensor
int ValorEntradaVentilador;            // Variable que almacena el valor raw (0 a 1023)
float VoltajeVentilador;               // Variable que almacena el voltaje (0.0 a 25.0)

const int PinEntradaNicrom = A1;       // Seleccionar la entrada para el sensor
int ValorEntradaNicrom;                // Variable que almacena el valor raw (0 a 1023)
float VoltajeNicrom;                   // Variable que almacena el voltaje (0.0 a 25.0)

int PinConmutador =8 ;
int ValorConmutador = 0;

int valorPWMVentilador=0;              // Debe ser entre 0 y 255
long valorSerialVentilador=80;
int VentiladorPWM = 9;

int valorPWMNicrom=0;                  // Debe ser entre 0 y 255
long valorSerialNicrom=0;
int NicromPWM = 10;

String cadena;                         // Lee desde el USB el Labview
String numcadena [50];                 // Array que almacena los datos que les llega desde el Labview
int posInicio = 0;                     // Posiciones para la lectura de la cadena que llega desde serial, supongamos v43V , 'v' sería posInicio y 'V' posFinal
int posFinal;                          // Posicion final




const int sensorPin = A2;              // Seleccionar la entrada para el sensor
float sensorValue;                     // Variable que almacena el valor raw (0 a 1023)
float VoltajeINA;    
float T = 0;                           // Temperatura  
float alpha = 0.00385;                 // Constante alpha de la pt100
float gananciaINA = 109.851177;
float VS=0 ;
float Vin = 12;
float k = 100;




float VoltajeTemp [100];               // Buffer que almacenará los 100 ultimos valores de temperatura en voltaje
int indexT = 0;                        // Posición que se moverá en el bucle
int ActivarTemp = 0;                   // Activa la lectura de la temperatura
long previousMillis = 0;               // Almacenará el último cambio
long interval = 500;                   // Intervalo de envío de temperatura(en milisegundos)
String temperatura;                    // Cadena de caracteres con el valor de temperatura en grados centígrados




 
void setup() {
   Serial.begin(115200);
   Serial.setTimeout(100);
   Serial.read();
}

// Funcion para la lectura y envio del valor de temperatura
void lecturaTemperatura() {
  sensorValue = analogRead(sensorPin);
  VoltajeINA = ((sensorValue / 1023) * 5);
  VS = (VoltajeINA / gananciaINA) + (1.05463/1000);
  T = (VS * (k+1) * (k+1)) / (Vin * k * alpha);   // el 0.681818 es un offset calculado con diferentes valores de temperatura

  temperatura = String(T);
  Serial.println("t" + temperatura + "T");
}

void loop() {
   ValorConmutador = digitalRead(PinConmutador);
   
   if(ValorConmutador == LOW){          // Se ha seleccionado el control por USB
      if (Serial.available() > 0) {
         cadena = Serial.readStringUntil('\n');
         cadena.trim();
         
         switch (cadena[0]) {  // Solo miramos el primer carácter (porque sabemos que la cadena recibida siempre va a estar con el formato correcto)
            case 'v': {
               // Extraemos los números entre v y V (posiciones 1 a length-1) donde está la velocidad del ventilador
               int valorSerialVentilador = cadena.substring(1, cadena.length()-1).toInt();
               
               // Procesamiento del valor enviado al ventilador
               if(valorSerialVentilador > 40 && valorSerialVentilador < 100){
                  valorPWMVentilador = (255*valorSerialVentilador)/100; // Regla de tres para pasar el porcentaje al rango 0-255
               }
               else if(valorSerialVentilador <= 40){
                  valorPWMVentilador = (255*40)/100;                    // Se fija el valor 40 para valores menores de 40
               }
               else {  //(valorSerialVentilador >= 100) 
                  valorPWMVentilador = (255*100)/100;                   // Se fija el valor 255 para valores mayores de 100
               }
               
               analogWrite(VentiladorPWM, valorPWMVentilador);          // Escribir en el pin analógico el valor PWM
               break;
            }
            
            case 'n': {
               // Extraemos los números entre n y N (posiciones 1 a length-1) donde está la potencia del calefactor
               int valorSerialNicrom = cadena.substring(1, cadena.length()-1).toInt();
               
               // Procesamiento del valor enviado al Nicrom
               if(valorSerialNicrom >= 0 && valorSerialNicrom < 85){
                  valorPWMNicrom= (255*valorSerialNicrom)/100;          // Regla de tres para pasar el porcentaje al rango 0-255
               }
               else {  //(valorSerialNicrom >= 85)
                  valorPWMNicrom = (255*85)/100;                        // Se fija el valor 85 para valores mayores de 85
               }
               
               analogWrite(NicromPWM, valorPWMNicrom);         // Escribir en el pin analógico el valor PWM
               break;
            }

            case 't' : {        
               // Leemos el valor de temperatura
               lecturaTemperatura();
               
               break;
            }
         }
      }
   }

   if (ValorConmutador == HIGH){                           // Se ha seleccionado el control por Voltaje
      ValorEntradaVentilador = analogRead(PinEntradaVentilador);          // realizar la lectura
      VoltajeVentilador = fmap(ValorEntradaVentilador, 0, 1023, 0.0, 25.0);   // cambiar escala a 0.0 - 25.0
      VoltajeVentilador = VoltajeVentilador + 0.037;  // calibración con respecto al tester

      if(VoltajeVentilador > 2 && VoltajeVentilador < 5){
         valorPWMVentilador = (255*VoltajeVentilador)/5; // Regla de tres para pasar el porcentaje al rango 0-255
      }
      else if(VoltajeVentilador <= 2){
         valorPWMVentilador = (255*2)/5; 
      }

      else {  //(VoltajeVentilador >= 5)
         valorPWMVentilador = (255*5)/5; 
      }
  
      analogWrite(VentiladorPWM, valorPWMVentilador);       // Escribir en el pin analógico el valor PWM
   
      ValorEntradaNicrom = analogRead(PinEntradaNicrom);          // realizar la lectura
      VoltajeNicrom = fmap(ValorEntradaNicrom, 0, 1023, 0.0, 25.0);   // cambiar escala a 0.0 - 25.0
      VoltajeNicrom = VoltajeNicrom + 0.037; // calibración con respecto al tester

      if(VoltajeNicrom >= 0 && VoltajeNicrom < 4.25){
         valorPWMNicrom= (255*VoltajeNicrom)/5; // Regla de tres para pasar el porcentaje al rango 0-255
      }
      else {  //(VoltajeNicrom >= 4.25)
         valorPWMNicrom = (255*4.25)/5; 
      }
    
      analogWrite(NicromPWM, valorPWMNicrom);       // Escribir en el pin analógico el valor PWM

      //Serial.println(VoltajeNicrom,3);            // mostrar el valor por serial
   }
}

// cambio de escala entre floats
float fmap(float x, float in_min, float in_max, float out_min, float out_max)
{
   return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}
