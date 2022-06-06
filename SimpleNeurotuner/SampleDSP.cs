using System;
using CSCore;

namespace SimpleNeurotuner
{
    class SampleDSP: ISampleSource
    {
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
            byte[] buffer1 = new byte[32000];
            float gainAmplification = (float)(Math.Pow(10.0, GainDB / 20.0));//получить Усиление
            int samples = mSource.Read(buffer, offset, count);//образцы 
            if (gainAmplification != 1.0f) 
            {
                for (int i = offset; i < offset + samples; i++)
                {
                    buffer[i] = Math.Max(Math.Min(buffer[i] * gainAmplification, 1), -1);
                    buffer1[i] = (byte)buffer[i];
                }
            }

            if (PitchShift != 1.0f)
            {
                 
                PitchShifter.PitchShift((byte)PitchShift, offset, count, 4096, 4, (byte)mSource.WaveFormat.SampleRate, buffer1);
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
