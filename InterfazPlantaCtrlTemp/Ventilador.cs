using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfazPlantaCtrlTemp
{
    internal class Ventilador
    {
        private int _velocidad;

        public event Action<int> VelocidadChanged;

        public Ventilador(int velocidadInicial = 40)
        {
            _velocidad = velocidadInicial;
        }

        public int GetVelocidad()
        {
            return _velocidad;
        }

        public void SetVelocidad(int nuevaVelocidad)
        {
            nuevaVelocidad = Math.Max(40, Math.Min(100, nuevaVelocidad));

            if (_velocidad == nuevaVelocidad) return;

            _velocidad = nuevaVelocidad;
            VelocidadChanged?.Invoke(_velocidad);
        }
    }
}
