﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SoundAnalysis;

namespace SimpleNeurotuner
{

    // Утилита которая помогает найти фундаментальную чатсоту.

    public static class FrequencyUtilsRec
    {
        public static int magn;
        public static double fristPeak;
        // Находим фундаментальную частоту: вычисляем спектрограмму, находим пики, анализируем
        //х - звуковые сэмплы
        //sampleRate частота семплирования
        //minFreq минимальная полезная частота
        //maxFreq максимальная полезная частота
        //returns Найденная частота, 0 - в противном случае

        internal static double FindFundamentalFrequency(float[] x, int sampleRate, double minFreq, double maxFreq)
        {
            float[] spectr = FftAlgorithm.Calculate(x);

            /*for(int i = 0; i < spectr.Length; i++)
            {
                /*if (i % 2 == 1)
                {
                    File.AppendAllText("spectr.txt", spectr[i].ToString() + "\n");
                }
                else
                {
                    File.AppendAllText("spectr2.txt", spectr[i].ToString() + "\n");
                }
                File.AppendAllText("spectr.txt", spectr[i].ToString() + "\n");
            }*/
            int len = x.Length;
            int usefullMinSpectr = Math.Max(0,
                (int)(minFreq * spectr.Length / sampleRate));
            int usefullMaxSpectr = Math.Min(spectr.Length,
                (int)(maxFreq * spectr.Length / sampleRate) + 1);

            // находим пики частот по БПФ
            const int PeaksCount = 30;
            int[] peakIndices;
            peakIndices = FindPeaks(spectr, usefullMinSpectr, usefullMaxSpectr - usefullMinSpectr,
                PeaksCount);
            /*for (int i = 0; i < PeaksCount; i++)
            {
                File.AppendAllText("magnRec.txt", peakIndices[i].ToString("f3") + "\n");
            }*/
            /*for (int i = 0; i < 5; i++)
            {
                File.AppendAllText("magnRec.txt", FindPeaks(spectr, usefullMinSpectr, usefullMaxSpectr - usefullMinSpectr, PeaksCount)[i].ToString() + "\n");
            }*/
            if (Array.IndexOf(peakIndices, usefullMinSpectr) >= 0)
            {
                // 
                // если звука не будет - вернет 0
                return 0;
            }

            // выбирает фрагмент для регистрации пиковых значений: смещение данных
            const int verifyFragmentOffset = 0;
            // ... и половина длины данных
            int verifyFragmentLength = (int)(sampleRate / minFreq);

            // перебираем все пики для поиска одного с наименьшим отличием от других
            float minPeakValue = float.PositiveInfinity;
            int minPeakIndex = 0;
            int minOptimalInterval = 0;
            for (int i = 0; i < peakIndices.Length; i++)
            {
                int index = peakIndices[i];
                int binIntervalStart = spectr.Length / (index + 1), binIntervalEnd = spectr.Length / index;
                int interval;
                float peakValue;
                // сканирование частот/интервалов
                ScanSignalIntervals(x, verifyFragmentOffset, verifyFragmentLength,
                    binIntervalStart, binIntervalEnd, out interval, out peakValue);

                if (peakValue < minPeakValue)
                {
                    minPeakValue = peakValue;
                    minPeakIndex = index;
                    minOptimalInterval = interval;
                }
            }

            return (double)sampleRate / minOptimalInterval;
        }

        private static void ScanSignalIntervals(float[] x, int index, int length,
            int intervalMin, int intervalMax, out int optimalInterval, out float optimalValue)
        {
            optimalValue = float.PositiveInfinity;
            optimalInterval = 0;

            // интервал между самым маленьким и большим значением может быть большим
            // ограничиваем его фиксорованным значением
            const int MaxAmountOfSteps = 30;
            int steps = intervalMax - intervalMin;
            if (steps > MaxAmountOfSteps)
                steps = MaxAmountOfSteps;
            else if (steps <= 0)
                steps = 1;

            // ищем в интервале волну с наименьшим различием длины
            for (int i = 0; i < steps; i++)
            {
                int interval = intervalMin + (intervalMax - intervalMin) * i / steps;

                float sum = 0;
                for (int j = 0; j < length; j++)
                {
                    float diff = x[index + j] - x[index + j + interval];
                    sum += diff * diff;
                }
                if (optimalValue > sum)
                {
                    optimalValue = sum;
                    optimalInterval = interval;
                }
            }
        }

        private static int[] FindPeaks(float[] values, int index, int length, int peaksCount)
        {
            //MainWindow mainWindow = new MainWindow();
            //mainWindow.Magn = 0;
            float[] peakValues = new float[peaksCount];
            int[] peakIndices = new int[peaksCount];
            //int len = values.Length;

            for (int i = 0; i < peaksCount; i++)
            {
                peakValues[i] = values[peakIndices[i] = i + index];
                //File.AppendAllText("magnRec.txt", peakValues[i].ToString("f3") + "\n");
                //if (peakValues[i] < 0 && peakValues[i + 1] > 0) { }
            }

            // находим минимальное значение
            double minStoredPeak = peakValues[0];
            int minIndex = 0;
            for (int i = 1; i < peaksCount; i++)
            {
                if (minStoredPeak > peakValues[i])
                {
                    minStoredPeak = peakValues[minIndex = i];
                }
            }

            for (int i = peaksCount; i < length; i++)
            {
                if (minStoredPeak < values[i + index])
                {
                    // заменяем минимальное значение бОльшим
                    peakValues[minIndex] = values[peakIndices[minIndex] = i + index];

                    // и ищем минимальное значение снова
                    minStoredPeak = peakValues[minIndex = 0];
                    for (int j = 1; j < peaksCount; j++)
                    {
                        if (minStoredPeak > peakValues[j])
                        {
                            minStoredPeak = peakValues[minIndex = j];
                            //mainWindow.Magn = peakValues[minIndex = j];
                        }
                    }
                }
            }

            return peakIndices;
        }
    }
}
