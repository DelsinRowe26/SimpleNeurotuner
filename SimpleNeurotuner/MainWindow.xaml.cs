﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSCore;
using CSCore.SoundIn;//Вход звука
using CSCore.SoundOut;//Выход звука
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;
using System.IO;
using Microsoft.DirectX.DirectSound;
using Buffer = Microsoft.DirectX.DirectSound.Buffer;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SimpleNeurotuner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
            
        [DllImport("BiblZvuk.dll", CallingConvention = CallingConvention.Cdecl)]
        //unsafe
          static extern int vizualzvuk(string filename,  int[] Rdat);
        
        private FileInfo fileInfo = new FileInfo("window.tmp");
        private FileInfo fileInfo1 = new FileInfo("Data_Load.dat");
        private FileInfo FileLanguage = new FileInfo("Data_Language.dat");
        private FileInfo fileinfo = new FileInfo("DataTemp.dat");
        private SimpleMixer mMixer;
        private int SampleRate = 44100;
        private WasapiOut mSoundOut;
        private WasapiCapture mSoundIn;
        private SampleDSP mDsp, mDsp1;
        string[] file1 = File.ReadAllLines("window.tmp");
        
        string folder = "Record";
        private IWaveSource _source;
        private MMDeviceCollection mOutputDevices;
        private MMDeviceCollection mInputDevices;
        string start = "00:00:03,25";
        string end = "00:00:04,25";
        string myfile;
        string cutmyfile;
        public int index = 1;
        string langindex;
        string FileName, cutFileName;
        //private PitchShifter _pitchShifter;

        private ISampleSource mMp3;
        private string file, filename;
        private string record;
        private string[] allfile;
        private int click, audioclick = 0;

        BackgroundWorker worker;
        
        public MainWindow()
        {
            InitializeComponent();
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
        }
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                if (worker.CancellationPending == true)
                {
                    //e.Cancel = true;
                    (sender as BackgroundWorker).ReportProgress(100);
                    break;
                    //return;
                }
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(100);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PBNFT.Value = e.ProgressPercentage;
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
                //File.Create("DataTemp.dat");
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
            string[] filename = File.ReadAllLines(fileInfo1.FullName);
            if (filename.Length == 1)
            {
                Languages();
            }
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
                //Recording();
                Recordind2();
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
                System.Windows.MessageBox.Show(msg);
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
                mDsp1 = new SampleDSP(mMp3.ToWaveSource(16).ToSampleSource());
                //mDsp1.GainDB = (float)slVolume.Value;
                mMixer.AddSource(mDsp1.ChangeSampleRate(mMixer.WaveFormat.SampleRate).ToWaveSource(16).Loop().ToSampleSource());

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
            audioclick = 0;
            if (audioclick == 0)
            {
                SaveDeleteWindow saveDelete = new SaveDeleteWindow();
                saveDelete.Show();
            }
        }

        private void SimpleNeurotuner_Closing(object sender, CancelEventArgs e)
        {
            Stop();
            Environment.Exit(0);
        }

        private void btnStart_Open_MouseMove(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new System.Windows.Media.FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.White) });
            btnStart_Open.Style = style;
        }

        private void btnStart_Open_MouseLeave(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new System.Windows.Media.FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.Black) });
            btnStart_Open.Style = style;
        }

        private void btnStop_MouseMove(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new System.Windows.Media.FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.White) });
            btnStop.Style = style;
        }

        private void btnStop_MouseLeave(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new System.Windows.Media.FontFamily("Verdana") });
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

        private void slVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                mDsp.GainDB = (float)slVolume.Value;
                lbVolValue.Content = (int)slVolume.Value;
            }
            catch
            {
                if (langindex == "0")
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
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new System.Windows.Media.FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.White) });
            btnRecord.Style = style;
        }

        private void btnRecord_MouseLeave(object sender, MouseEventArgs e)
        {
            Style style = new Style();
            style.Setters.Add(new Setter { Property = Control.FontFamilyProperty, Value = new System.Windows.Media.FontFamily("Verdana") });
            style.Setters.Add(new Setter { Property = Control.MarginProperty, Value = new Thickness(10) });
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new SolidColorBrush(Colors.Blue) });
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = new SolidColorBrush(Colors.Black) });
            btnRecord.Style = style;
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            //audioclick = 1;
            //mDsp.PitchShift = 0;
            Audition();
        }

        private async void Recording()
        {
            try
            {
                StreamReader FileRecord = new StreamReader("Data_Create.dat");
                StreamReader FileCutRecord = new StreamReader("Data_cutCreate.dat");
                myfile = FileRecord.ReadToEnd();
                cutmyfile = FileCutRecord.ReadToEnd();
                using (mSoundIn = new WasapiCapture())
                {
                    mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                    mSoundIn.Initialize();
                    lbRecordPB.Visibility = Visibility.Visible; 
                    mSoundIn.Start();
                    using (WaveWriter record = new WaveWriter(cutmyfile, mSoundIn.WaveFormat))
                    {
                        mSoundIn.DataAvailable += (s, data) => record.Write(data.Data, data.Offset, data.ByteCount);
                        for(int i = 0; i < 100; i++)
                        {
                            pbRecord.Value++;
                            await Task.Delay(50);
                        }
                        //Thread.Sleep(5000);
                        mSoundIn.Stop();
                        lbRecordPB.Visibility = Visibility.Hidden;
                        pbRecord.Value = 0;
                    }
                    Thread.Sleep(1000);
                    CutRecord cutRecord = new CutRecord();
                    cutRecord.CutFromWave(cutmyfile, myfile, start, end);
                    File.Move(myfile, @"Record\" + myfile);
                }
                if (langindex == "0")
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
                if (langindex == "0")
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

        private void Recordind2()
        {
            //float[] buffer = new float[4096];
            mSoundIn = new WasapiCapture();
            mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
            mSoundIn.Initialize();

            var source = new SoundInSource(mSoundIn) { FillWithZeros = true };

            mDsp = new SampleDSP(source.ToSampleSource().ToStereo());
            
            mSoundIn.Start();

            //Инициальный микшер
            Mixer();

            //Добавляем наш источник звука в микшер
            mMixer.AddSource(mDsp.ChangeSampleRate(mMixer.WaveFormat.SampleRate));

            SoundOut();
            Thread.Sleep(5000);
            mDsp.PitchShift = 0;
            Thread.Sleep(5000);
            Stop();
            mSoundIn.Stop();
            if (langindex == "0")
            {
                string msg = "Запись и обработка завершена.";
                MessageBox.Show(msg);
            }
            else
            {
                string msg = "Recording and processing completed.";
                MessageBox.Show(msg);
            }
            //PitchShifter.PitchShift(0, 2, 2, 2048, 4, mSoundIn.WaveFormat.SampleRate, );
            //mSoundIn.DataAvailable += (s, data) => PitchShifter.PitchShift(0, data.Offset, data.ByteCount, 2048, 4, mSoundIn.WaveFormat.SampleRate, data.Data);
        }

        private void Languages()
        {
            StreamReader FileLanguage = new StreamReader("Data_Language.dat");
            File.WriteAllText("Data_Load.dat", "1");
            File.WriteAllText("DataTemp.dat", "0");
            langindex = FileLanguage.ReadToEnd();
            if (langindex == "0")
            {
                index = 0;
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Выберите запись");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
                cmbModes.Items.Clear();
                cmbModes.Items.Add("Записи");
                cmbModes.Items.Add("Прослушивание");
                cmbModes.SelectedIndex = cmbModes.Items.Count - 1;
                Title = "Нейротюнер NFT";
                btnStart_Open.Content = "Старт";
                btnStop.Content = "Стоп";
                Help.Header = "Помощь";
                TabNFT.Header = "gNeuro NFT";
                TabSettings.Header = "Настройки";
                lbVersion.Content = "Версия: 1.1";
                btnRecord.Content = "Прослушать";
                cmbInput.ToolTip = "Микрофон";
                cmbOutput.ToolTip = "Наушники";
                cmbRecord.ToolTip = "Записи";
                cmbModes.ToolTip = "Режимы";
                lbPBNFT.Content = "Идёт загрузка NFT...";
                lbRecordPB.Content = "Идёт запись...";
            }
            else
            {
                index = 0;
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Select a record");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
                cmbModes.Items.Clear();
                cmbModes.Items.Add("Record");
                cmbModes.Items.Add("Audition");
                cmbModes.SelectedIndex = cmbModes.Items.Count - 1;
                Title = "Neurotuner NFT";
                btnStart_Open.Content = "Start";
                btnStop.Content = "Stop";
                Help.Header = "Help";
                TabNFT.Header = "gNeuro NFT";
                TabSettings.Header = "Settings";
                lbVersion.Content = "Version: 1.1";
                btnRecord.Content = "Audition";
                cmbInput.ToolTip = "Microphone";
                cmbOutput.ToolTip = "Speaker";
                cmbRecord.ToolTip = "Record";
                cmbModes.ToolTip = "Modes";
                lbPBNFT.Content = "NFT loading in progress...";
                lbRecordPB.Content = "Recording in progress...";
            }
        }

        private void SimpleNeurotuner_Activated(object sender, EventArgs e)
        {
            string[] text = File.ReadAllLines("Data_Load.dat");
            string[] text1 = File.ReadAllLines(fileinfo.FullName);
            //string[] filename = File.ReadAllLines(fileInfo1.FullName);
            if (text.Length == 0 && text1.Length == 1)
            {
                Languages();
            }
            if (langindex == "0")
            {
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Выберите запись");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
            }
            else
            {
                cmbRecord.Items.Clear();
                cmbRecord.Items.Add("Select a record");
                cmbRecord.SelectedIndex = cmbRecord.Items.Count - 1;
                Filling();
            }
        }

        private void cmbModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cmbModes.SelectedIndex == 0)
            {
                CreateWindow window = new CreateWindow();
                window.Show();
                btnRecord.Visibility = Visibility.Visible;
                pbRecord.Visibility = Visibility.Visible;
                pbRecord.Value = 0;
            }
            else
            {
                btnRecord.Visibility = Visibility.Hidden;
                pbRecord.Visibility = Visibility.Hidden;
            }
        }

        private async void Audition()
        {
            StreamReader FileRecord = new StreamReader("Data_Create.dat");
            myfile = FileRecord.ReadToEnd();
            Stop();
            Mixer();
            mMp3 = CodecFactory.Instance.GetCodec(@"Record\" + myfile).ToStereo().ToSampleSource();
            mMixer.AddSource(mMp3.ChangeSampleRate(mMixer.WaveFormat.SampleRate).ToWaveSource(16).Loop().ToSampleSource());

            await Task.Run(() => SoundOut());
        }

        private async void cmbRecord_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRecord.SelectedItem != null)
            {
                //unsafe
                {
                    int[] Rdat = new int[150000];
                    int Ndt;
                    int Ww, Hw,k,ik,dWw,dHw;
                    filename = @"Record\" + cmbRecord.SelectedItem.ToString();
                    if ((filename != "Record\\Select a record") && (filename != "Record\\Выберите запись"))
                    {
                        worker.RunWorkerAsync();
                        Ndt = await Task.Run(() =>
                        {
                            return vizualzvuk(filename, Rdat);
                        });                        
                        Hw = (int)Math.Sqrt(Ndt);
                        Ww = (int)((double)(Ndt) / (double)(Hw) + 0.5);
                        dWw = (int)((Image1.Width - (double)Ww) / 2.0)-5;
                        if (dWw < 0)
                            dWw = 0;
                        dHw = (int)((Image1.Height - (double)Hw) / 2.0)-5;
                        if (dHw < 0)
                            dHw = 0;
                        WriteableBitmap wb = new WriteableBitmap((int)Image1.Width, (int)Image1.Height, Ww, Hw, PixelFormats.Bgra32, null);

                        // Define the update square (which is as big as the entire image).
                        Int32Rect rect = new Int32Rect(0, 0, (int)Image1.Width, (int)Image1.Height);

                        byte[] pixels = new byte[(int)Image1.Width * (int)Image1.Height * wb.Format.BitsPerPixel / 8];
                        //Random rand = new Random();
                        k = 0;
                        ik = 0;
                        int Wwt = 2, Hwt = 2, it0 = Ww / 2, jt0 = Hw / 2, it = 0, jt = 0;
                        int R=0,G=0,B=0,A=0;
                        int pixelOffset,poffp=0,kt=0;
                        while (k < Ndt)
                        {
                            if (ik % 4 == 0)
                            {
                                R = Rdat[3 * k];
                                G = Rdat[3 * k + 1];
                                B = Rdat[3 * k + 2];
                                A = 255;
                                pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                                pixels[pixelOffset] = (byte)B;
                                pixels[pixelOffset + 1] = (byte)G;
                                pixels[pixelOffset + 2] = (byte)R;
                                pixels[pixelOffset + 3] = (byte)A;                               
                                jt++;
                                if (jt == Hwt)
                                {
                                    ik++;
                                }                                
                            }
                            else if (ik % 4 == 1)
                            {
                                R = Rdat[3 * k];
                                G = Rdat[3 * k + 1];
                                B = Rdat[3 * k + 2];
                                A = 255;
                                pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                                pixels[pixelOffset] = (byte)B;
                                pixels[pixelOffset + 1] = (byte)G;
                                pixels[pixelOffset + 2] = (byte)R;
                                pixels[pixelOffset + 3] = (byte)A;
                                it++;
                                if (it == Wwt)
                                {
                                    ik++;
                                }
                            }
                            else if (ik % 4 == 2)
                            {
                                R = Rdat[3 * k];
                                G = Rdat[3 * k + 1];
                                B = Rdat[3 * k + 2];
                                A = 255;
                                pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                                pixels[pixelOffset] = (byte)B;
                                pixels[pixelOffset + 1] = (byte)G;
                                pixels[pixelOffset + 2] = (byte)R;
                                pixels[pixelOffset + 3] = (byte)A;
                                jt--;
                                if (jt == -1)
                                {
                                    ik++;
                                    //jt0--;
                                }
                            }
                            else
                            {
                                R = Rdat[3 * k];
                                G = Rdat[3 * k + 1];
                                B = Rdat[3 * k + 2];
                                A = 255;
                                pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                                pixels[pixelOffset] = (byte)B;
                                pixels[pixelOffset + 1] = (byte)G;
                                pixels[pixelOffset + 2] = (byte)R;
                                pixels[pixelOffset + 3] = (byte)A;
                                it--;
                                if (it == -1)
                                {
                                    it = 0;
                                    jt = 0;
                                    ik++;
                                    it0--;
                                    jt0--;
                                    Hwt += 2;
                                    Wwt += 2;
                                }
                            }
                            int stride = ((int)Image1.Width * wb.Format.BitsPerPixel) / 8;
                            wb.WritePixels(rect, pixels, stride, 0);                            
                            k++;
                        }
                        // Show the bitmap in an Image element.
                        Image1.Source = wb;
                        worker.CancelAsync();
                    }
                }
            }
            /*for (int i = 0; i < 100; i++)
            {
                PBNFT.Value++;
                await Task.Delay(200);
            }*/
        }        
    }
}
