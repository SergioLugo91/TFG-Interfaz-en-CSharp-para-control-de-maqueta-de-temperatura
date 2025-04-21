
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

void loop() {

   ValorConmutador = digitalRead(PinConmutador);
   
   if(ValorConmutador == LOW){          // Se ha seleccionado el control por USB
      if (Serial.available() > 0) {
         cadena = Serial.readString();  // Leemos la cadena por serial

         int j = 0;
         for (int i = 0 ; i < cadena.length() ; i++) {      // Recorremos las posiciones de la cadena

            switch (cadena[i]) {
               case 'v':   // Entre v y V estará la velocidad del ventilador por serial
               posInicio = i + 1;
               break;

               case 'V': 
               posFinal = i;    // La posicion final será lo que recorra el bucle
               numcadena[j] = cadena.substring(posInicio, posFinal);  // Numcadena almacena en un string lo que va despues de v, PE(v43V) pues coge el 43
               valorSerialVentilador = numcadena[j].toInt();  // El valor del ventilador será el valor anterior convertido en entero
               j++;

               // Condiciones del ventilador serial
               if(valorSerialVentilador > 40 && valorSerialVentilador < 100){
                  valorPWMVentilador = (255*valorSerialVentilador)/100; // Regla de tres para pasar el porcentaje al rango 0-255
               }
               if(valorSerialVentilador <= 40){
                  valorPWMVentilador = (255*40)/100; 
               }
               if(valorSerialVentilador >= 100){
                  valorPWMVentilador = (255*100)/100; 
               }
               analogWrite(VentiladorPWM, valorPWMVentilador);          // Escribir en el pin analógico el valor PWM

               posInicio = i + 1;
               break;

               case 'n': // Entre n y N estará la intensidad del nicrom por serial
               posInicio = i + 1;
               break;

               case 'N': 
               posFinal = i;    // La posicion final será lo que recorra el bucle
               numcadena[j] = cadena.substring(posInicio, posFinal);  // Numcadena almacena en un string lo que va despues de n, PE(n43N) pues coge el 43
               valorSerialNicrom = numcadena[j].toInt();  // El valor del Nicrom será el valor anterior convertido en entero
               j++;
               
               // Condicionces del nicrom serial
               if(valorSerialNicrom >= 0 && valorSerialNicrom < 85){
                  valorPWMNicrom= (255*valorSerialNicrom)/100; // Regla de tres para pasar el porcentaje al rango 0-255
               }
               if(valorSerialNicrom >= 85){                    // Establecemos límite del Nicrom
                  valorPWMNicrom = (255*85)/100; 
               }
               analogWrite(NicromPWM, valorPWMNicrom);         // Escribir en el pin analógico el valor PWM

               posInicio = i + 1;
               break;

               case 't':   // Entre t y T estará el índice de la lectura 
               posInicio = i + 1;
               break;

               case 'T': 
               posFinal = i;    // La posicion final será lo que recorra el bucle
               numcadena[j] = cadena.substring(posInicio, posFinal);  // Numcadena almacena en un string lo que va despues de t, PE(t43T) pues coge el 43
               valorSerialVentilador = numcadena[j].toInt();  // El valor del ventilador será el valor anterior convertido en entero
               j++;
               
               lecturaTemperatura();

               posInicio = i + 1;
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
      if(VoltajeVentilador <= 2){
         valorPWMVentilador = (255*2)/5; 
      }

      if(VoltajeVentilador >= 5){
         valorPWMVentilador = (255*5)/5; 
      }
  
      analogWrite(VentiladorPWM, valorPWMVentilador);       // Escribir en el pin analógico el valor PWM
   
      ValorEntradaNicrom = analogRead(PinEntradaNicrom);          // realizar la lectura
      VoltajeNicrom = fmap(ValorEntradaNicrom, 0, 1023, 0.0, 25.0);   // cambiar escala a 0.0 - 25.0
      VoltajeNicrom = VoltajeNicrom + 0.037; // calibración con respecto al tester
      if(VoltajeNicrom >= 0 && VoltajeNicrom < 4.25){
         valorPWMNicrom= (255*VoltajeNicrom)/5; // Regla de tres para pasar el porcentaje al rango 0-255
      }
      if(VoltajeNicrom >= 4.25){
         valorPWMNicrom = (255*4.25)/5; 
      }
    
      analogWrite(NicromPWM, valorPWMNicrom);       // Escribir en el pin analógico el valor PWM

      //Serial.println(VoltajeNicrom,3);            // mostrar el valor por serial
   }
}

// Funcion para la lectura y envio del valor de temperatura
void lecturaTemperatura() {
   sensorValue = analogRead(sensorPin);
   VoltajeINA = ((sensorValue/ 1023) * 5);

   //VoltajeTemp[indexT] = VoltajeINA;
   //indexT = ++indexT % 100;

   //float MediaVoltajeTemp = 0;
   //for (int i = 0 ; i < 100 ; i++)
   //MediaVoltajeTemp = MediaVoltajeTemp + VoltajeTemp[i] ;

   //MediaVoltajeTemp = MediaVoltajeTemp / 100;            
   //VS = (MediaVoltajeTemp/gananciaINA)+(1.05463/1000);   
   //T = (VS*(k+1)*(k+1))/(Vin*k*alpha);

   VS = (VoltajeINA/gananciaINA)+(1.05463/1000);         // Esto sirve para promediar la muestra de temperatura en 100 muestras
   T = (VS*(k+1)*(k+1))/(Vin*k*alpha);                   // el 0.681818 es un offset calculado con diferentes valores de temperatura

   temperatura = String(T);
   Serial.print("t" + temperatura + "T");

   unsigned long currentMillis = millis(); // tiempo actual
   
   /*
   if (currentMillis - previousMillis > interval) {      // si el tiempo actual - el anterior es menor que el intervalo entra aqui
      previousMillis = currentMillis;
      //Serial.println("");
      //Serial.print("Vo(V): ");
      //Serial.print(MediaVoltajeTemp,3);
      //Serial.print("    VS(mV): ");
      //Serial.print(VS*1000,5);
      Serial.print("t");
      Serial.print( T,1 );
      Serial.println("T");
   }
   */
}

// cambio de escala entre floats
float fmap(float x, float in_min, float in_max, float out_min, float out_max)
{
   return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}
