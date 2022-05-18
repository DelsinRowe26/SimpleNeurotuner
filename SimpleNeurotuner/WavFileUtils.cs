using System;
using CSCore;
using CSCore.Codecs.WAV;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNeurotuner
{
    public static class WavFileUtils
    {
        public static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStar, TimeSpan cutFromEnd)
        {
            using(WaveFileReader reader = new WaveFileReader(inPath))
            {
                using(WaveWriter writer = new WaveWriter(outPath, reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.BytesPerSecond / 1000;

                    int startPos = (int)cutFromStar.TotalMilliseconds * bytesPerMillisecond;
                    startPos = startPos - startPos % reader.WaveFormat.BlockAlign;
                }
            }
        }
    }
}
