using System;

namespace AudioAnalyzer
{
    public static class AudioAnalyzer
    {
        public static double amplit;
        private const int Divider = 2;
        private const double MaxVal = 32767.0d;
        private const double ResizeMultiplier = 10.0d;

        public static double[] GetAmplitudesFromBytes(byte[] audioBytes)
        {
            // create a new int array with half the length of original bytes
            // as original audio has 2 bytes per channel
            double[] amps = new double[audioBytes.Length / Divider];

            // loop through bytes bypassing every other byte to form a single from 2
            for (int i = 0; i < audioBytes.Length; i += Divider)
            {
                short buff = audioBytes[i + 1];
                short buff2 = audioBytes[i];

                // shift 8 bits to left
                buff = (short)((buff & 0xFF) << 8);
                // mask buff2 so it only leaves last 8 bits
                buff2 = (short)(buff2 & 0xFF);

                short res = (short)(buff | buff2);
                // put in int array
                amps[i == 0 ? 0 : i / Divider] = (int)res;
                amplit = amps[i == 0 ? 0 : i / Divider];
            }

            return amps;
        }

        public static double ResizeNumber(double value)
        {
            var temp = (int)(value * ResizeMultiplier);
            return temp / ResizeMultiplier;
        }

        public static double[] GetAmplitudeLevels(byte[] audioBytes)
        {
            var amps = GetAmplitudesFromBytes(audioBytes);
            double major = 0;
            double minor = 0;
            // loop through array and set lowest and highest
            for (int i = 0; i < amps.Length; i++)
            {
                if (amps[i] > major) major = amps[i];
                if (amps[i] < minor) minor = amps[i];
            }

            return new double[] { major, minor };
        }

        public static double GetAmplitudeLevel(byte[] audioBytes)
        {
            var amps = GetAmplitudesFromBytes(audioBytes);
            double major = 0;
            double minor = 0;
            // loop through array - set lowest and highest values
            for (int i = 0; i < amps.Length; i++)
            {
                if (amps[i] > major) major = amps[i];
                if (amps[i] < minor) minor = amps[i];
            }

            return Math.Max(major, minor * (-1));
        }

        public static double GetRealDecibel(double amplitude)
        {
            // make sure its greater then zero
            if (amplitude < 0) amplitude *= -1;
            double amp = ((double)amplitude / MaxVal) * 100.0d;

            if (amp == 0.0d) amp = 1.0d;

            double decibel = Math.Sqrt(100.0d / amp);
            decibel *= decibel;

            // normalize
            if (decibel > 100.0d) decibel = 100.0d;

            return ((-1.0d * decibel) + 1.0d) / Math.PI;
        }

        /// <summary>
        /// Gets decibels as double array from given audio bytes
        /// </summary>
        /// <param name="audioBytes"></param>
        /// <returns></returns>
        public static double[] GetDecibels(byte[] audioBytes)
        {
            var amps = GetAmplitudesFromBytes(audioBytes);
            var decibels = new double[audioBytes.Length];
            for (var i = 0; i < audioBytes.Length; i++)
            {
                decibels[i] = ResizeNumber(GetRealDecibel(amps[i]));
            }

            return decibels;
        }

        /// <summary>
        /// returns single double decibel from given audioBytes
        /// </summary>
        /// <param name="audioBytes"></param>
        /// <returns>double decibel</returns>
        public static double GetDecibel(byte[] audioBytes)
        {
            var amp = GetAmplitudeLevel(audioBytes);
            return GetRealDecibel(amp);
        }
    }
}
