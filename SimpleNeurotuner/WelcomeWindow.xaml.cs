using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleNeurotuner
{
    /// <summary>
    /// Логика взаимодействия для WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FileInfo FileLanguage = new FileInfo("Data_Language.dat");
            if(cmbLanguage.SelectedIndex == 1)
            {
                File.WriteAllText("DataTemp.dat", "1");
                //File.Create("DataTemp.dat");
                File.WriteAllText(FileLanguage.FullName, "0");
                Title = "Добро пожаловать";
                lbWelcome.Content = "Добро пожаловать в Нейротюнер NFT";
                lbDescriptionText.Content = "Версия: 1.1\n\nПрограмма предназначена для курса\nпо входу в ресурсное состояние.\n\nВ данном окне вы можете выбрать язык русский\nили английский.\n\nИнструкция:\n\n1. Если микрофон и динамики (наушники)\nне выбраны по умолчанию, то выберите их самостоятельно.\n\n2. Выберите запись.\n\n3. Нажмите кнопку запуска и наслаждайтесь.\nТакже вы можете за счет ползунка регулировать громкость\nсвоего голоса в микрофоне.\nВозникло желание остановиться, нажми кнопку стоп.\n\n4. Если вы хотите сделать свою запись, то переключите режим\nиз прослушивания в режим записи.\n\n5. У вас появится окошко в котором вы можете\nназвать свою запись.\n\n6. После того как вы назовете свою запись, и нажмите\nкнопку создать вы перейдете в основное окошко, в котором\nпоявится дополнительная кнопка.\n\n7. Чтобы сделать запись начинаете сначала издавать звук,\nа потом нажимаете кнопку старт, тогда начнется запись,\nпосле окончания записи выскочит окошко,\nо завершении записи.\n\n8. Потом вы нажимаете на кнопку прослушать и слушаете\nсвою запись. После того как вы наслушались запись,\nнажимаете кнопку стоп. Выскочит окошко в котором вы\nвыбираете сохранить свою запись, либо удалить\nи перезаписать.";
            } 
            else
            {
                File.WriteAllText("DataTemp.dat", "1");
                //File.Create("DataTemp.dat");
                File.WriteAllText(FileLanguage.FullName, "1");
                Title = "Welcome";
                lbWelcome.Content = "Welcome to Neurotuner NFT";
                lbDescriptionText.Content = "Version: 1.1\n\nThe program is intended for a course on entering the resource state.\n\nIn this window, you can select the language Russian or English.\n\nInstruction:\n\n1. If the microphone and speakers (headphones) \nare not selected by default, then select them yourself.\n\n2. Select an entry.\n\n3. Press the start button, and enjoy.You can also use the slider to\nadjust the volume of your voice in the microphone. There was a desire to stop,\npress the stop button.\n\n4. If you want to make your own recording, then switch the\nmode from listening to recording mode.\n\n5. You will have a window in which you can name your entry.\n\n6. After you name your entry and press the create button,\nyou will go to the main window in which an additional\nbutton will appear.\n\n7. To make a recording, first start making a sound, and then\npress the start button, then the recording will begin, after\nthe end of the recording, a window will pop up about\nthe completion of the recording.\n\n8. Then you click on the listen button and listen to your recording.\nAfter you have listened to the recording, press the stop button.\nA window pops up in which you choose to keep your entry,\nor delete and overwrite.";
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            //File.AppendAllText("Data_Load.dat", "1");
        }
    }
}
