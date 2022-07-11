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
        public static int[] indexPeak = new int[2];
        internal static float FindFundamentalFrequency(float[] buffer, int sampleRate, float minFreq, float maxFreq)
        {
            float[] spectr = FftAlgorithm.Calculate(buffer);
            int usefulMinSpectr = Math.Max(0, (int)(minFreq * spectr.Length / sampleRate));
            int usefulMaxSpectr = Math.Max(0, (int)(maxFreq * spectr.Length / sampleRate) + 1);

            const int PeakCount = 5;
            int[] peakIndices;
            peakIndices = FindPeaks(spectr, usefulMinSpectr, usefulMaxSpectr - usefulMinSpectr, PeakCount);
            if(Array.IndexOf(peakIndices, usefulMinSpectr) >= 0)
            {
                return 0;
            }

            const int verifyFragmentOffset = 0;
            int verifyFragmentLength = (int)(sampleRate / minFreq);

            float minPeakValue = float.PositiveInfinity;
            int minPeakIndex = 0;
            int minOptimalInterval = 0;
            for(int i = 0; i < peakIndices.Length; i++)
            {
                int index = peakIndices[i];
                int binIntervalStart = spectr.Length / (index + 1), binIntervalEnd = spectr.Length / index;
                int interval;
                float peakValue;
                //сканирование частот/интервалов
                ScanSignalIntervals(buffer, verifyFragmentOffset, verifyFragmentLength, binIntervalStart, binIntervalEnd, out interval, out peakValue);

                if(peakValue < minPeakValue)
                {
                    minPeakValue = peakValue;
                    minPeakIndex = index;
                    minOptimalInterval = interval;
                }

            }
            return sampleRate / minOptimalInterval;
        }

        private static void ScanSignalIntervals(float[] buffer, int index, int length, int intervalMin, int intervalMax, out int optimalInterval, out float optimalValue)
        {
            optimalValue = float.PositiveInfinity;
            optimalInterval = 0;

            // интервал между самым маленьким и большим значением может быть большим
            // ограничиваем его фиксорованным значением
            const int MaxAmountOfSteps = 30;
            int steps = intervalMax - intervalMin;
            if(steps > MaxAmountOfSteps)
            {
                steps = MaxAmountOfSteps;
            }
            else if(steps <= 0)
            {
                steps = 1;
            }
            
            // ищем в интервале волну с наименьшим различием длины
            for(int i = 0; i < steps; i++)
            {
                int interval = intervalMin + (intervalMax - intervalMin) * i / steps;

                float sum = 0;
                for(int j = 0; j < length; j++)
                {
                    float diff = buffer[index + j] - buffer[index + j + interval];
                    sum += diff * diff;
                }
                if (optimalValue > sum)
                {
                    optimalValue = sum;
                    optimalInterval = interval;
                }
            }
        }

        public static int[] FindPeaks(float[] buffer, int index, int length, int peaksCount)
        {
            float[] peakValues = new float[peaksCount];
            int[] peakIndices = new int[peaksCount];
            //int len = buffer.Length;

            for(int i = 0; i < peaksCount; i++)
            {
                //-if (buffer[i] < 0 && buffer[i + 1] > 0) { }
                peakValues[i] = buffer[peakIndices[i] = i + index];
                indexPeak[i] = peakIndices[i];

            }
            
            //принимаем за максимальное значение начальное значение массива
            float minStoredPeak = peakValues[0];
            int minIndex = 0;
            for(int i = 1; i < peaksCount; i++)
            {
                if(minStoredPeak > peakValues[i])
                {
                    minStoredPeak = peakValues[minIndex = i];
                }
            }

            for(int i = peaksCount; i < length; i++)
            {
                if (minStoredPeak < buffer[i + index])
                {
                    peakValues[minIndex] = buffer[peakIndices[minIndex] = i + index];

                    minStoredPeak = peakValues[minIndex = 0];
                    for (int j = 1; j < peaksCount; j++)
                    {
                        if(minStoredPeak > peakValues[j])
                        {
                            minStoredPeak = peakValues[minIndex = j];
                        }
                    }
                }
            }
            return peakIndices;
        }
    }
}
