﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CSCore;
using CSCore.SoundIn;//Вход звука
using CSCore.SoundOut;//Выход звука
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SimpleNeurotuner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileInfo fileInfo = new FileInfo("window.tmp");
        private SimpleMixer mMixer;
        private int SampleRate = 44100;
        private WasapiOut mSoundOut;
        private WasapiCapture mSoundIn;
        private SampleDSP mDsp;
        string[] file1 = File.ReadAllLines("window.tmp");
        string folder = "Record";
        private IWaveSource _source;
        private MMDeviceCollection mOutputDevices;
        private MMDeviceCollection mInputDevices;
        string start = "00:00:02,0";
        string end = "00:00:03,0";
        //private PitchShifter _pitchShifter;

        TimeSpan cutFromStart = new TimeSpan(0, 0, 1);
        TimeSpan cutFromEnd = new TimeSpan(0, 0, 2);
        private ISampleSource mMp3;
        private string file, filename;
        private string record;
        private string[] allfile;
        private int click;

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

        void CutFromWave(string WavFileName, string NewFileName, string tstart, string tend)
        {
            var header = new WavHeader();
            //var headerSize = Marshal.SizeOf(headerr);
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

            byte[] buffer = new byte[headerSize];
            fileStream.Read(buffer, 0, headerSize);
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);

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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Mixer()
        {
            mMixer = new SimpleMixer(2, SampleRate) //стерео, 44,1 КГц
            {
                FillWithZeros = true,
                DivideResult = true, //Для этого установлено значение true, чтобы избежать звуков тиков из-за превышения -1 и 1.
            };
        }

        private void SimpleNeurotuner_Loaded(object sender, RoutedEventArgs e)
        {
            if (file1.Length == 0)
            {
                WelcomeWindow window = new WelcomeWindow();
                window.Show();
                File.AppendAllText(fileInfo.FullName, "1");
            }
            //Находит устройства для захвата звука и заполнияет комбобокс
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            mInputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
            MMDevice activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            foreach (MMDevice device in mInputDevices)
            {
                cmbInput.Items.Add(device.FriendlyName);
                if (device.DeviceID == activeDevice.DeviceID) cmbInput.SelectedIndex = cmbInput.Items.Count - 1;
            }

            //Находит устройства для вывода звука и заполняет комбобокс
            activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            mOutputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            foreach (MMDevice device in mOutputDevices)
            {
                cmbOutput.Items.Add(device.FriendlyName);
                if (device.DeviceID == activeDevice.DeviceID) cmbOutput.SelectedIndex = cmbOutput.Items.Count - 1;
            }

            cmbRecord.Items.Add("Select a record");
            cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
            Filling();
            cmbLanguage.Items.Add("en");
            cmbLanguage.SelectedIndex = cmbLanguage.Items.Count - 1;
            //cmbModes.Items.Add("Audition");
            //cmbModes.SelectedIndex = cmbModes.Items.Count - 1;
            //cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
        }

        private void Filling()
        {
            allfile = Directory.GetFiles(folder);
            foreach (string filename in allfile)
            {
                //record = filename.Replace(@"Record\", "");
                record = filename.Remove(0, 7);
                cmbRecord.Items.Add(record);
            }
        }

        private async void btnStart_Open_Click(object sender, RoutedEventArgs e)
        {
            if (cmbModes.SelectedIndex == 0)
            {
                Recording();
            }
            else
            {
                click = 1;
                await Task.Run(() => Sound(file));
                StartFullDuplex();
            }
        }

        private void Stop()
        {
            try
            {
                if (mMixer != null)
                {
                    mMixer.Dispose();
                    mMp3.ToWaveSource(16).Loop().ToSampleSource().Dispose();
                    mMixer = null;
                }
                if (mSoundOut != null)
                {
                    mSoundOut.Stop();
                    mSoundOut.Dispose();
                    mSoundOut = null;
                }
                if (mSoundIn != null)
                {
                    mSoundIn.Stop();
                    mSoundIn.Dispose();
                    mSoundIn = null;
                }
                if (_source != null)
                {
                    _source.Dispose();
                    _source = null;
                }
                if (mMp3 != null)
                {
                    mMp3.Dispose();
                    mMp3 = null;
                }
            }
            catch
            {

            }
        }

        private async void StartFullDuplex()//запуск пича и громкости
        {
            try
            {
                //Запускает устройство захвата звука с задержкой 1 мс.
                mSoundIn = new WasapiCapture(/*false, AudioClientShareMode.Exclusive, 1*/);
                mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                mSoundIn.Initialize();
               
                var source = new SoundInSource(mSoundIn) { FillWithZeros = true };
                
                //Init DSP для смещения высоты тона
                mDsp = new SampleDSP(source.ToSampleSource().ToStereo());
                mDsp.GainDB = (float)slVolume.Value;
                //SetPitchShiftValue();
                mSoundIn.Start();

                //Инициальный микшер
                Mixer();

                //Добавляем наш источник звука в микшер
                mMixer.AddSource(/*source.ToSampleSource().ToStereo()*/mDsp.ChangeSampleRate(mMixer.WaveFormat.SampleRate));

                //Запускает устройство воспроизведения звука с задержкой 1 мс.
                await Task.Run(() => SoundOut());
                //return true;
            }
            catch (Exception ex)
            {
                string msg = "Error in StartFullDuplex: \r\n" + ex.Message;
                MessageBox.Show(msg);
                Debug.WriteLine(msg);
            }
            //return false;
        }

        private void SoundOut()
        {
            mSoundOut = new WasapiOut();
            //mSoundOut.Device = mOutputDevices[cmbOutput.SelectedIndex];
            mSoundOut.Initialize(mMixer.ToWaveSource(16));
            mSoundOut.Play();
        }

        private async void Sound(string file)
        {
            Stop();
            if (click != 0)
            {
                Mixer();
                mMp3 = CodecFactory.Instance.GetCodec(filename).ToStereo().ToSampleSource();
                mMixer.AddSource(mMp3.ChangeSampleRate(mMixer.WaveFormat.SampleRate).ToWaveSource(16).Loop().ToSampleSource());

                await Task.Run(() => SoundOut());
            } 
            else
            {
                Stop();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            click = 0;
        }

        private void SimpleNeurotuner_Closing(object sender, CancelEventArgs e)
        {
            Stop();
            Environment.Exit(0);
        }

        private void btnStart_Open_MouseMove(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.White) });
            btnStart_Open.Style = style;
        }

        private void btnStart_Open_MouseLeave(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.Black) });
            btnStart_Open.Style = style;
        }

        private void btnStop_MouseMove(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.White) });
            btnStop.Style = style;
        }

        private void btnStop_MouseLeave(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.Black) });
            btnStop.Style = style;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window1 window1 = new Window1();
            window1.Show();
        }

        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLanguage.SelectedIndex == 0)
            {
                cmbLanguage.ToolTip = "Язык";
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Выберите запись");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
                cmbModes.Items.Clear();
                cmbModes.Items.Add("Записи");
                cmbModes.Items.Add("Прослушивание");
                cmbModes.SelectedIndex = cmbModes.Items.Count - 1;
                Title = "Нейрокейс";
                Window1 window1 = new Window1();
                window1.index = cmbLanguage.SelectedIndex;
                btnStart_Open.Content = "Старт";
                btnStop.Content = "Стоп";
                Help.Header = "Помощь";
                lbVersion.Content = "Нейрокейс Версия: 1.1";
            }
            else
            {
                cmbLanguage.ToolTip = "Language";
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Select a record");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
                cmbModes.Items.Clear();
                cmbModes.Items.Add("Record");
                cmbModes.Items.Add("Audition");
                cmbModes.SelectedIndex = cmbModes.Items.Count - 1;
                Title = "Neurokeys";
                Window1 window1 = new Window1();
                window1.index = cmbLanguage.SelectedIndex;
                btnStart_Open.Content = "Start";
                btnStop.Content = "Stop";
                Help.Header = "Help";
                lbVersion.Content = "Neurokeys Version: 1.1";
            }
        }

        private void slVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                mDsp.GainDB = (float)slVolume.Value;
                lbVolValue.Content = (int)slVolume.Value;
            }
            catch
            {
                if (cmbLanguage.SelectedIndex == 1)
                {
                    string msg = "Ошибка в измененном значении объема: \r\n" + "Сначала начните запись, затем переместите ползунок громкости.";
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Volume Value Changed: \r\n" + "Start recording first, then move the volume slider.";
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        private void btnRecord_MouseMove(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.White) });
            btnRecord.Style = style;
        }

        private void btnRecord_MouseLeave(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.Black) });
            btnRecord.Style = style;
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
           Recording();
        }

        private void Recording()
        {
            try
            {
                using (mSoundIn = new WasapiCapture())
                {
                    mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                    mSoundIn.Initialize();
                    mSoundIn.Start();
                    using (WaveWriter record = new WaveWriter("my.wav", mSoundIn.WaveFormat))
                    {
                        mSoundIn.DataAvailable += (s, data) => record.Write(data.Data, data.Offset, data.ByteCount);
                        Thread.Sleep(5000);
                        mSoundIn.Stop();
                    }
                    Thread.Sleep(2000);
                    CutFromWave("my.wav", "cutmy.wav", start, end);
                }
                if (cmbLanguage.SelectedIndex == 0)
                {
                    string msg = "Запись и обработка завершена.";
                    MessageBox.Show(msg);
                }
                else
                {
                    string msg = "Recording and processing completed.";
                    MessageBox.Show(msg);
                }
            }
            catch
            {
                if (cmbLanguage.SelectedIndex == 0)
                {
                    string msg = "Произошла ошибка, если она выскочила,\nзначит что-то сломалось,\nлибо вы удалили что-то нужное.";
                    MessageBox.Show(msg);
                }
                else
                {
                    string msg = "An error occurred, if it popped up,\nsomething is broken,\nor you deleted something you needed.";
                    MessageBox.Show(msg);
                }
            }
        }



        private void button_Click(object sender, RoutedEventArgs e)
        {

            //WavFileUtils.TrimWavFile("mymp.mp3", "cutmymp.mp3", cutFromStart, cutFromEnd);

            //CutFromWave("my.wav", "cutmy.wav", start, end);
        }

        private void cmbModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cmbModes.SelectedIndex == 0)
            {

            }
        }

        private void cmbRecord_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRecord.SelectedItem != null)
            {
                filename = @"Record\" + cmbRecord.SelectedItem.ToString();
            }
        }
    }
}
