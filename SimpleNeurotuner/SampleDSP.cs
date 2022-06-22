﻿using System;
using CSCore;
using System.IO;
using CSCore.Codecs.WAV;

namespace SimpleNeurotuner
{
    class SampleDSP: ISampleSource
    {
        public string freq;
        ISampleSource mSource;
        public SampleDSP(ISampleSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            mSource = source;
            PitchShift = 1;
        }
        public int Read(float[] buffer, int offset, int count)
        {
            double[] buffer1 = new double[count];
            float gainAmplification = (float)(Math.Pow(10.0, GainDB / 20.0));//получить Усиление
            int samples = mSource.Read(buffer, offset, count);//образцы
            //if (gainAmplification != 1.0f) 
            //{
                for (int i = offset; i < offset + samples; i++)
                {
                    buffer[i] = Math.Max(Math.Min(buffer[i] * gainAmplification, 1), -1);
                    buffer1[i] = (double)buffer[i];
                    
                }
            
            //}
            if (PitchShift != 1.0f)
            {
                //FrequencyUtils.FindFundamentalFrequency(buffer1, mSource.WaveFormat.SampleRate, 60, 22050);
                //PitchShifter.PitchShift(PitchShift, offset, count, 4096, 4, mSource.WaveFormat.SampleRate, buffer);
                FrequencyUtils.FindFundamentalFrequency(buffer1, mSource.WaveFormat.SampleRate, 60, 24000);
                freq = FrequencyUtils.FindFundamentalFrequency(buffer1, mSource.WaveFormat.SampleRate, 60, 24000).ToString();

            }
          
            return samples;
        }

        public float GainDB { get; set; }

        public float PitchShift { get; set; }

        public bool CanSeek
        {
            get { return mSource.CanSeek; }
        }

        public WaveFormat WaveFormat
        {
            get { return mSource.WaveFormat; }
        }

        public long Position
        {
            get
            {
                return mSource.Position;
            }
            set
            {
                mSource.Position = value;
            }
        }

        public long Length
        {
            get { return mSource.Length; }
        }

        public void Dispose()
        {
            if (mSource != null) mSource.Dispose();
        }
    }
}
