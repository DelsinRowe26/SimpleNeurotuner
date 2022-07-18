﻿/****************************************************************************
*
* NAME: PitchShift.cs
* VERSION: 1.0
* HOME URL: http://www.dspdimension.com
* KNOWN BUGS: none
*
* SYNOPSIS: Routine for doing pitch shifting while maintaining
* duration using the Short Time Fourier Transform.
*
* DESCRIPTION: The routine takes a pitchShift factor value which is between 0.5
* (one octave down) and 2. (one octave up). A value of exactly 1 does not change
* the pitch. numSampsToProcess tells the routine how many samples in indata[0...
* numSampsToProcess-1] should be pitch shifted and moved to outdata[0 ...
* numSampsToProcess-1]. The two buffers can be identical (ie. it can process the
* data in-place). fftFrameSize defines the FFT frame size used for the
* processing. Typical values are 1024, 2048 and 4096. It may be any value <=
* MAX_FRAME_LENGTH but it MUST be a power of 2. osamp is the STFT
* oversampling factor which also determines the overlap between adjacent STFT
* frames. It should at least be 4 for moderate scaling ratios. A value of 32 is
* recommended for best quality. sampleRate takes the sample rate for the signal 
* in unit Hz, ie. 44100 for 44.1 kHz audio. The data passed to the routine in 
* indata[] should be in the range [-1.0, 1.0), which is also the output range 
* for the data, make sure you scale the data accordingly (for 16bit signed integers
* you would have to divide (and multiply) by 32768). 
*
* COPYRIGHT 1999-2006 Stephan M. Bernsee <smb [AT] dspdimension [DOT] com>
*
* 						The Wide Open License (WOL)
*
* Permission to use, copy, modify, distribute and sell this software and its
* documentation for any purpose is hereby granted without fee, provided that
* the above copyright notice and this license appear in all source copies. 
* THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED WARRANTY OF
* ANY KIND. See http://www.dspguru.com/wol.htm for more information.
*
*****************************************************************************/

/****************************************************************************
*
* This code was converted to C# by Michael Knight
* madmik3 at gmail dot com. 
* http://sites.google.com/site/mikescoderama/
*
*****************************************************************************/

using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SimpleNeurotuner
{
    public class PitchShifter1
    {

        #region Private Static Memebers
        public static double Freq;
        public static string NoteName;
        public static int[] min = new int[10];
        public static int[] max = new int[10];
        public static int[] Vol = new int[10];
        public static int SampleRate2;
        private static float[] KMAX, MMAX;
        public static float MAX, MIN, MAXIN;
        private static long IndexMAX, IndexMAX1, IndexMAX2, IndexMIN;
        private static long IndexSTART, IndexEND;
        private static int MAX_FRAME_LENGTH = 16000;
        private static float[] gInFIFO = new float[MAX_FRAME_LENGTH];
        private static float[] gOutFIFO = new float[MAX_FRAME_LENGTH];
        private static float[] gFFTworksp = new float[2 * MAX_FRAME_LENGTH];
        private static double[] gFFTworksp2 = new double[2 * MAX_FRAME_LENGTH];
        private static float[] gLastPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private static float[] gSumPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private static float[] gOutputAccum = new float[2 * MAX_FRAME_LENGTH];
        private static float[] gAnaFreq = new float[MAX_FRAME_LENGTH];
        private static float[] gAnaMagn = new float[MAX_FRAME_LENGTH];
        private static float[] gSynFreq = new float[MAX_FRAME_LENGTH];
        private static float[] gSynMagn = new float[MAX_FRAME_LENGTH];
        private static string[] NoteNames = { "A", "A#", "B/H", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };
        private static float ToneStep = (float)Math.Pow(2, 1.0 / 12);//рассчет шага тоны
        private static long gRover, gInit;
        #endregion

        #region Public Static  Methods
        public static void PitchShift(float pitchShift, long sampleCount, float sampleRate, float[] indata)
        {
            PitchShift(pitchShift, 0, sampleCount, (long)2048, (long)4, sampleRate, indata);
        }

        public static void FindClosestNote(double frequency, out double closestFrequency/*, out string noteName*/)//нахождение ближайшей ноты
        {
            const float AFrequency = 440.0f;// какое-то значение частоты
            const int ToneIndexOffsetToPositives = 120;//Смещение индекса тона к положительным значениям

            int toneIndex = (int)Math.Round(Math.Log(frequency / AFrequency, ToneStep));//высчитывание индекса тоны
            //noteName = NoteNames[(ToneIndexOffsetToPositives + toneIndex) % NoteNames.Length];//определение ноты из списка
            closestFrequency = (float)Math.Pow(ToneStep, toneIndex) * AFrequency;//ближайшая нота
        }

        public static async void PitchShift(float pitchShift, long offset, long sampleCount, long fftFrameSize,
            long osamp, float sampleRate, float[] indata)
        {
            double magn, phase, tmp, window, real, imag;
            double freqPerBin, expct;
            long i, k, m, qpd, index, inFifoLatency, stepSize, fftFrameSize2;
            double closestFrequency;//ближайшая частота
            string noteName;

            float[] outdata = indata;
            /* set up some handy variables/настроить некоторые удобные переменные */
            fftFrameSize2 = fftFrameSize / 2;
            stepSize = fftFrameSize / osamp;
            freqPerBin = sampleRate / (double)fftFrameSize;
            expct = 2.0 * Math.PI * (double)stepSize / (double)fftFrameSize;
            inFifoLatency = fftFrameSize - stepSize;
            if (gRover == 0) gRover = inFifoLatency;

            /* main processing loop/основной цикл обработки */
            for (i = offset; i < sampleCount; i++)
            {

                /* As long as we have not yet collected enough data just read in/Пока мы еще не собрали достаточно данных, просто читаем в */
                gInFIFO[gRover] = indata[i];
                outdata[i] = gOutFIFO[gRover - inFifoLatency];
                gRover++;

                /* now we have enough data for processing/теперь у нас достаточно данных для обработки */
                if (gRover >= fftFrameSize)
                {
                    gRover = inFifoLatency;

                    /* do windowing and re,im interleave/делать окна и повторно чередовать */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gFFTworksp[2 * k] = (float)(gInFIFO[k] * window);
                        gFFTworksp[2 * k + 1] = 0.0F;
                    }


                    /* ***************** ANALYSIS ******************* */
                    /* do transform */
                    ShortTimeFourierTransform(gFFTworksp, fftFrameSize, -1);

                    /* this is the analysis step/это этап анализа  */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* de-interlace FFT buffer/деинтерлейсный буфер FFT  */
                        real = gFFTworksp[2 * k];
                        imag = gFFTworksp[2 * k + 1];

                        /* compute magnitude and phase/вычислить амплитуду и фазу  */
                        magn = Math.Sqrt(real * real + imag * imag);//амплитуда
                        phase = Math.Atan2(imag, real);//фаза

                        /* compute phase difference/вычислить разность фаз */
                        tmp = phase - gLastPhase[k];//частота
                        gLastPhase[k] = (float)phase;

                        /* subtract expected phase difference/вычесть ожидаемую разность фаз */
                        tmp -= (double)k * expct;

                        /* map delta phase into +/- Pi interval/сопоставить фазу дельты с интервалом +/- Pi */
                        qpd = (long)(tmp / Math.PI);
                        if (qpd >= 0) qpd += qpd & 1;
                        else qpd -= qpd & 1;
                        tmp -= Math.PI * (double)qpd;

                        /* get deviation from bin frequency from the +/- Pi interval/получить отклонение от частоты бина от интервала +/- Pi */
                        tmp = osamp * tmp / (2.0 * Math.PI);

                        /* compute the k-th partials' true frequency/вычислить истинную частоту k-го парциала */
                        tmp = (double)k * freqPerBin + tmp * freqPerBin;

                        FindClosestNote(tmp, out closestFrequency/*, out noteName*/);
                        //NoteName = noteName;
                        //Freq = closestFrequency;

                        /* store magnitude and true frequency in analysis arrays/хранить величину и истинную частоту в массивах анализа */
                        gAnaMagn[k] = (float)magn;
                        //File.AppendAllText("magn.txt", gAnaMagn[k].ToString() + "\n");
                        gAnaFreq[k] = (float)tmp;
                        //File.AppendAllText("tmp.txt", gAnaFreq[k].ToString() + "\n");

                    }

                    int usefulMinSpectr = Math.Max(0, (int)(30 * gAnaMagn.Length / sampleRate));
                    int usefulMaxSpectr = Math.Max(0, (int)(24000 * gAnaMagn.Length / sampleRate) + 1);

                    int[] indexPeak = MyFrequencyUtils.FindPeaks(gAnaMagn, usefulMinSpectr, usefulMaxSpectr - usefulMinSpectr, 2);
                    int[] result = new int[2];
                    float kmu = 0;
                    float mmu = 0;
                    float ksigma = 0;
                    float msigma = 0;
                    int n = 0;

                    for (int j = 0; j < 2; j++)
                    {
                            k = indexPeak[j];
                            KMAX[n] = gAnaMagn[k];
                            k++;
                            while (k < fftFrameSize2 && KMAX[n] < gAnaMagn[k])
                            {
                                KMAX[n] = gAnaMagn[k++];
                            }
                            m = indexPeak[j];
                            MMAX[n] = gAnaMagn[m];
                            m--;
                            while (m >= 0 && MMAX[n] < gAnaMagn[m])
                            {
                                MMAX[n] = gAnaMagn[m--];
                            }
                            if (Math.Abs(indexPeak[j] - k) > Math.Abs(indexPeak[j] - m))
                            {
                                result[j] = (int)m;
                            }
                            else
                            {
                                result[j] = (int)k;
                            }
                            kmu += KMAX[n] / 8;
                            mmu += MMAX[n] / 8;
                            ksigma += (float)Math.Sqrt(Math.Pow(KMAX[n] - kmu, 2));
                            msigma += (float)Math.Sqrt(Math.Pow(MMAX[n] - mmu, 2));
                            if (((Math.Abs(KMAX[n] - kmu)  < ksigma) == (kmu - ksigma < KMAX[n] && KMAX[n] < ksigma + kmu)) && ((Math.Abs(KMAX[n] - kmu) < 2 * ksigma) == (kmu - 2 * ksigma < KMAX[n] && KMAX[n] < 2 * ksigma + kmu)) && ((Math.Abs(KMAX[n] - kmu) < 3 * ksigma) == (kmu - 3 * ksigma < KMAX[n] && KMAX[n] < 3 * ksigma + kmu)))
                            {

                            }
                    }
                    
                    //bufRec[l] = (float)Math.Sqrt(Math.Pow(KMAX - MMAX,2));
                    /*MAX = gAnaMagn[0];
                    MIN = gAnaMagn[0];
                    IndexMAX = 0;
                    IndexMIN = 0;
                    for(k = 0; k <= fftFrameSize2; k++)
                    {
                        MAX = Math.Max(MAX, gAnaMagn[k]);
                        MIN = Math.Min(MIN, gAnaMagn[k]);
                        /*if(MAX < gAnaMagn[k])
                        {
                            MAX = gAnaMagn[k];
                            IndexMAX = k;
                        }
                        if(MIN > gAnaMagn[k])
                        {
                            MIN = gAnaMagn[k];
                            IndexMIN = k;
                        }*
                        MAXIN = Math.Max(Math.Abs(MIN), Math.Abs(MAX));
                    }*/

                    /* ***************** PROCESSING ******************* */
                    /* this does the actual pitch shifting/это делает фактическое изменение высоты тона */

                    for (int zero = 0; zero < fftFrameSize; zero++)
                    {
                        gSynMagn[zero] = 0;
                        gSynFreq[zero] = 0;
                    }

                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        index = (long)(k * pitchShift);
                        if (index <= fftFrameSize2)
                        {
                            gSynMagn[index] += gAnaMagn[k];
                            gSynFreq[index] = gAnaFreq[k] * pitchShift;
                        }
                    }

                    /* ***************** SYNTHESIS ******************* */
                    /* this is the synthesis step/это этап синтеза */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* get magnitude and true frequency from synthesis arrays/получить величину и истинную частоту из массивов синтеза */
                        magn = gSynMagn[k];
                        tmp = gSynFreq[k];

                        /* subtract bin mid frequency/вычесть среднюю частоту бина */
                        tmp -= (double)k * freqPerBin;

                        /* get bin deviation from freq deviation/получить отклонение бина от отклонения частоты */
                        tmp /= freqPerBin;

                        /* take osamp into account/учитывать осамп */
                        tmp = 2.0 * Math.PI * tmp / osamp;

                        /* add the overlap phase advance back in/добавить фазу перекрытия обратно в */
                        tmp += (double)k * expct;


                        /* accumulate delta phase to get bin phase/накапливать дельта-фазу, чтобы получить бин-фазу */
                        gSumPhase[k] += (float)tmp;
                        phase = gSumPhase[k];

                        /* get real and imag part and re-interleave/получить реальную часть и часть изображения и повторно чередовать */
                        gFFTworksp[2 * k] = (float)(magn * Math.Cos(phase));
                        gFFTworksp[2 * k + 1] = (float)(magn * Math.Sin(phase));
                        //File.AppendAllText("AfterSTFT.txt", gFFTworksp[2 *k].ToString() + "\n");
                        //File.AppendAllText("AfterSTFT.txt", gFFTworksp[2 * k + 1].ToString() + "\n");
                    }

                    /* zero negative frequencies/ноль отрицательных частот */
                    for (k = fftFrameSize + 2; k < 2 * fftFrameSize; k++) { gFFTworksp[k] = 0.0F; /*File.AppendAllText("AfterSTFT.txt", gFFTworksp[k].ToString() + "\n");*/ }

                    /* do inverse transform/сделать обратное преобразование */
                    //await Task.Run(() => FrequencyUtils.FindFundamentalFrequency(gFFTworksp, 44100, 60, 22050));
                    ShortTimeFourierTransform(gFFTworksp, fftFrameSize, 1);


                    /* do windowing and add to output accumulator/делать окна и добавлять в выходной аккумулятор */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gOutputAccum[k] += (float)(2.0 * window * gFFTworksp[2 * k] / (fftFrameSize2 * osamp));
                    }
                    for (k = 0; k < stepSize; k++) gOutFIFO[k] = gOutputAccum[k];

                    /* shift accumulator/сменный аккумулятор */
                    //memmove(gOutputAccum, gOutputAccum + stepSize, fftFrameSize * sizeof(float));
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        gOutputAccum[k] = gOutputAccum[k + stepSize];
                    }

                    /* move input FIFO/переместить вход FIFO */
                    for (k = 0; k < inFifoLatency; k++) gInFIFO[k] = gInFIFO[k + stepSize];
                }
            }
        }
        #endregion

        #region Private Static Methods
        public static void ShortTimeFourierTransform(float[] fftBuffer, long fftFrameSize, long sign)
        {
            float wr, wi, arg, temp;//temp для замены, arg для вычисления косинусоида, wr для записи косинусоида, wi для записи синусоида
            float tr, ti, ur, ui;//
            long i, bitm, j, le, le2, k;//le,le2 это длина кажется но не точно. i и j индексы массива.

            for (i = 2; i < 2 * fftFrameSize - 2; i += 2)
            {
                for (bitm = 2, j = 0; bitm < 2 * fftFrameSize; bitm <<= 1)
                {
                    if ((i & bitm) != 0) j++;
                    j <<= 1;
                }
                if (i < j)
                {
                    temp = fftBuffer[i];
                    fftBuffer[i] = fftBuffer[j];
                    fftBuffer[j] = temp;
                    temp = fftBuffer[i + 1];
                    fftBuffer[i + 1] = fftBuffer[j + 1];
                    fftBuffer[j + 1] = temp;
                }
            }
            long max = (long)(Math.Log(fftFrameSize) / Math.Log(2.0) + .5);
            for (k = 0, le = 2; k < max; k++)
            {
                le <<= 1;
                le2 = le >> 1;
                ur = 1.0F;
                ui = 0.0F;
                arg = (float)Math.PI / (le2 >> 1);
                wr = (float)(Math.Cos(arg));//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                wi = (float)(sign * Math.Sin(arg));//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                for (j = 0; j < le2; j += 2)
                {

                    for (i = j; i < 2 * fftFrameSize; i += le)
                    {
                        tr = fftBuffer[i + le2] * ur - fftBuffer[i + le2 + 1] * ui;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                        ti = fftBuffer[i + le2] * ui + fftBuffer[i + le2 + 1] * ur;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                        fftBuffer[i + le2] = fftBuffer[i] - tr;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                        fftBuffer[i + le2 + 1] = fftBuffer[i + 1] - ti;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                        fftBuffer[i] += tr;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                        fftBuffer[i + 1] += ti;//нихуя не умножать и не прибовлять иначе ушам придет пиздец

                    }
                    tr = ur * wr - ui * wi;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                    ui = ur * wi + ui * wr;//нихуя не умножать и не прибовлять иначе ушам придет пиздец
                    ur = tr;
                }
            }
        }
        #endregion
    }
}

