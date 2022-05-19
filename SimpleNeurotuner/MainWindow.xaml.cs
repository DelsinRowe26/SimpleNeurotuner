using System;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSCore;
using CSCore.SoundIn;//Вход звука
using CSCore.SoundOut;//Выход звука
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.Streams.Effects;
using CSCore.DSP;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.IO;

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
        //private PitchShifter _pitchShifter;
        private ISampleSource mMp3;
        private OpenFileDialog openFileDialog;
        private string file, filename;
        private string record;
        private string[] allfile;
        private int click;

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
            click = 1;
            /*openFileDialog = new OpenFileDialog()
            {
                Filter = CodecFactory.SupportedFilesFilterEn,
                Title = "Select a file..."
            };
            file = openFileDialog.FileName;
            if (openFileDialog.ShowDialog() == true)
            {
                await Task.Run(() => Sound(file));
                //await Task.Run(() => StartFullDuplex());
                StartFullDuplex();
            }*/
            await Task.Run(() => Sound(file));
            //await Task.Run(() => StartFullDuplex());
            StartFullDuplex();
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
                
                //mMp3.ToWaveSource(16).Loop();
                //open the selected file
                /*ISampleSource source = CodecFactory.Instance.GetCodec(openFileDialog.FileName)
                    .ToSampleSource()
                    .AppendSource(x => new PitchShifter(x), out _pitchShifter);*/
                //play the audio

                await Task.Run(() => SoundOut());
            } 
            else
            {
                Stop();
            }
            /*do
            {
                Mixer();
                mMp3 = CodecFactory.Instance.GetCodec(openFileDialog.FileName).ToStereo().ToSampleSource();
                //mMp3.ToWaveSource(16).Loop();
                mMixer.AddSource(mMp3.ChangeSampleRate(mMixer.WaveFormat.SampleRate).ToWaveSource(16).Loop().ToSampleSource());
                
                //mMp3.ToWaveSource(16).Loop();
                //open the selected file
                /*ISampleSource source = CodecFactory.Instance.GetCodec(openFileDialog.FileName)
                    .ToSampleSource()
                    .AppendSource(x => new PitchShifter(x), out _pitchShifter);

                //play the audio

                await Task.Run(() => SoundOut());

                await Task.Delay(950);
                
                /*Mixer();
                mMp3 = CodecFactory.Instance.GetCodec(openFileDialog.FileName).ToStereo().ToSampleSource();
                mMixer.AddSource(mMp3.ChangeSampleRate(mMixer.WaveFormat.SampleRate));
                await Task.Run(() => SoundOut());
                await Task.Delay(950);
                //mMixer.Dispose();
                //mMp3.Dispose();
                //Thread.Sleep(750);
            } while (click != 0);*/
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
            if (cmbLanguage.SelectedIndex == 1)
            {
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Выберите запись");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
                Window1 window1 = new Window1();
                window1.index = cmbLanguage.SelectedIndex;
                btnStart_Open.Content = "Старт";
                btnStop.Content = "Стоп";
                Help.Header = "Помощь";
                lbVersion.Content = "Версия: 1.0";
            }
            else
            {
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Select a record");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
                Window1 window1 = new Window1();
                window1.index = cmbLanguage.SelectedIndex;
                btnStart_Open.Content = "Start";
                btnStop.Content = "Stop";
                Help.Header = "Help";
                lbVersion.Content = "Version: 1.0";
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

        private async void btnRecord_Click(object sender, RoutedEventArgs e)
        {
           Recording();
        }

        private void Recording()
        {
            using(mSoundIn = new WasapiCapture())
            {
                mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                mSoundIn.Initialize();
                using(WaveWriter record = new WaveWriter("my.wav", mSoundIn.WaveFormat))
                {
                    mSoundIn.DataAvailable += (s, data) => record.Write(data.Data, data.Offset, data.ByteCount);
                    mSoundIn.Start();
                    Thread.Sleep(5000);
                    mSoundIn.Stop();
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan cutFromStart = new TimeSpan(0, 0, 2);
            TimeSpan cutFromEnd = new TimeSpan(0, 0, 4);
            WavFileUtils.TrimWavFile("my.wav", "cutmy.wav", cutFromStart, cutFromEnd);
        }

        private void cmbRecord_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //filename = cmbRecord.SelectedItem.ToString();
            //filename = System.IO.Path.GetFullPath(cmbRecord.SelectedItem.ToString());
            filename = @"Record\" + cmbRecord.SelectedItem.ToString();
        }
    }
}
