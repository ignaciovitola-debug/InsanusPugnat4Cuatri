using System;
using UnityEngine;

namespace GladiusAI
{
    // Guarda los resultados de una funcion ya calculados para no repetir la cuenta.
    public class LookUpTable
    {
        private readonly float[] samples;
        private readonly float domainStart;
        private readonly float step;

        // Calcula la funcion una sola vez en sampleCount puntos entre domainStart y domainEnd.
        public LookUpTable(Func<float, float> function, float domainStart, float domainEnd, int sampleCount)
        {
            if (sampleCount < 2) throw new ArgumentException("Hacen falta al menos 2 muestras.");

            this.domainStart = domainStart;
            step = (domainEnd - domainStart) / (sampleCount - 1);

            samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                samples[i] = function(domainStart + i * step);
        }

        // Busca el valor de x entre las dos muestras mas cercanas (sin recalcular la funcion).
        public float ReturnValue(float x)
        {
            float position = (x - domainStart) / step;
            int index = Mathf.Clamp(Mathf.FloorToInt(position), 0, samples.Length - 1);
            int nextIndex = Mathf.Min(index + 1, samples.Length - 1);

            return Mathf.Lerp(samples[index], samples[nextIndex], position - index);
        }
    }
}
