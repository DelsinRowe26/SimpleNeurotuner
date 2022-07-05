using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CSCore;
using CSCore.Codecs.WAV;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SimpleNeurotuner
{
    internal class CutRecord
    {
        [StructLayout(LayoutKind.Sequential)]
        public partial class WavHeader
        {
            public UInt32 ChunkId;
            public UInt32 ChunkSize;
            public UInt32 Format;
            public UInt32 Subchunk1Id;
            public UInt32 Subchunk1Size;
            public UInt16 AudioFormat;
            public UInt16 NumChannels;
            public UInt32 SampleRate;
            public UInt32 ByteRate;
            public UInt16 BlockAlign;
            public UInt16 BitsPerSample;
            public UInt32 Subchunk2Id;
            public UInt32 Subchunk2Size;
        }

        public void CutFromWave(string WavFileName, string NewFileName, string tstart, string tend)
        {
            var header = new WavHeader();
            var headerSize = Marshal.SizeOf(header);

            string[] arrtime = tstart.Split(new char[] { ':', ',' }, StringSplitOptions.None);//определяет часы минуты секунды и милисекунды если они есть
            var hours = int.Parse(arrtime[0]);//запись часов
            var minitss = int.Parse(arrtime[1]);//запись минут
            var seconds = int.Parse(arrtime[2]);//запись секунд
            var miliseconds = int.Parse(arrtime[3]);//запись милисекунд

            var OSecSt = (hours * 3600000 + minitss * 60000 + seconds * 1000 + miliseconds);
            arrtime = tend.Split(new char[] { ':', ',' }, StringSplitOptions.None);
            hours = int.Parse(arrtime[0]);//запись часов
            minitss = int.Parse(arrtime[1]);//запись минут
            seconds = int.Parse(arrtime[2]);//запись секунд
            miliseconds = int.Parse(arrtime[3]);//запись милисекунд

            var OSecEn = (hours * 3600000 + minitss * 60000 + seconds * 1000 + miliseconds);
            var FileLength = OSecEn - OSecSt;

            FileStream fileStream = new FileStream(WavFileName, FileMode.Open, FileAccess.Read);
            //WaveFileReader waveFileReader = new WaveFileReader("cutMyRecord.wav");

            byte[] buffer = new byte[headerSize];
            fileStream.Read(buffer, 0, headerSize);
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            //ReadWav();
            
            

            //PitchShifter.PitchShift(0, 2, waveFileReader.Chunks.Count, 2048, 4, (byte)waveFileReader.WaveFormat.SampleRate, buffer);

            Marshal.Copy(buffer, 0, headerPtr, headerSize);
            Marshal.PtrToStructure(headerPtr, header);

            const int FFb = 44;

            fileStream.Seek((header.ByteRate / 1000) * OSecSt + FFb, SeekOrigin.Begin);

            var BRDBT = (int)header.ByteRate / 1000;
            byte[] WaveSent = new byte[FileLength * BRDBT];

            fileStream.Read(WaveSent, 0, FileLength * BRDBT);

            byte[] bytes = new byte[4];
            bytes = BitConverter.GetBytes(BRDBT * FileLength);

            buffer[40] = bytes[0];
            buffer[41] = bytes[1];
            buffer[42] = bytes[2];
            buffer[43] = bytes[3];

            IEnumerable<byte> arrays = buffer.Concat(WaveSent);
            string path = string.Format(@"{0}", NewFileName);
            File.WriteAllBytes(path, arrays.ToArray());
        }
    }
}
