using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfazPlantaCtrlTemp
{
    internal class Calefactor
    {
        private int _potencia;

        public event Action<int> PotenciaChanged;

        public Calefactor(int potenciaInicial = 0)
        {
            _potencia = potenciaInicial;
        }

        public int GetPotencia()
        {
            return _potencia;
        }

        public void SetPotencia(int nuevaPotencia)
        {
            nuevaPotencia = Math.Max(0, Math.Min(85, nuevaPotencia));

            if (_potencia == nuevaPotencia) return;

            _potencia = nuevaPotencia;
            PotenciaChanged?.Invoke(_potencia);
        }
    }
}
