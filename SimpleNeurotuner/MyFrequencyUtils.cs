using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoundAnalysis;

namespace SimpleNeurotuner
{
    public static class MyFrequencyUtils
    {
        internal static float FindFundamentalFrequency(float[] buffer, int sampleRate, float minFreq, float maxFreq)
        {
            float[] spectr = FftAlgorithm.Calculate(buffer);
            return 0;
        }
    }
}
