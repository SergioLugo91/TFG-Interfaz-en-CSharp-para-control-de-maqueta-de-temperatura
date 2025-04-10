using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfazPlantaCtrlTemp
{
    internal class datoEntrada
    {
        public double Tiempo { get; set; }
        public double Dato { get; set; }

        public datoEntrada(double tiempo, double dato)
        {
            Tiempo = tiempo;
            Dato = dato;
        }
    }
}
