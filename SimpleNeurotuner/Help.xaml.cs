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
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public string index;

        public Window1()
        {
            InitializeComponent();
        }

        private void Help_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader FileLanguage = new StreamReader("Data_Language.dat");
            index = FileLanguage.ReadToEnd();
            if (index == "0")
            {
                Title = "Помощь";
                tbHelp.Text = "Добро пожаловать в Нейротюнер NFT.\n\nИнструкция:\n1. Если микрофон и динамики (наушники) не выбраны\nпо умолчанию, то выберите их самостоятельно.\n2. Выберите запись.\n3. Нажмите кнопку запуска и наслаждайтесь. Также вы\nможете за счет ползунка регулировать громкость своего\nголоса в микрофоне. Возникло желание остановиться,\nнажмите кнопку стоп.\n4. Если вы хотите сделать свою запись, то переключите режим из прослушивания в режим записи.\n5. У вас появится окошко в котором вы можете назвать\nсвою запись.\n6. После того как вы назовете свою запись, и нажмите\nкнопку создать вы перейдете в основное окошко,\nв котором появится дополнительная кнопка.\n7. Чтобы сделать запись начинаете сначала издавать звук,\nа потом нажимаете кнопку старт, тогда начнется запись,\nпосле окончания записи выскочит окошко,о завершении\nзаписи.\n8. Потом вы нажимаете на кнопку прослушать и слушаете\nсвою запись. После того как вы наслушались запись,\nнажимаете кнопку стоп. Выскочит окошко в котором вы\nвыбираете сохранить свою запись, либо удалить\nи перезаписать.\n\nВерсия: 1.1";
            }
            else
            {
                Title = "Help";
                tbHelp.Text = "Welcome to Neurotuner NFT.\n\nInstruction:\n1. If the microphone and speakers (headphones) \nare not selected by default, then select them yourself.\n2. Select an entry.\n3. Press the start button, and enjoy.You can also use the slider\nto adjust the volume of your voice in the microphone.\nThere was a desire to stop, press the stop button.\n4. If you want to make your own recording, then switch the\nmode from listening to recording mode.\n5. You will have a window in which you can name your entry.\n6. After you name your entry and press the create button,\nyou will go to the main window in which an additional\nbutton will appear.\n7. To make a recording, first start making a sound, and then\npress the start button, then the recording will begin, after\nthe end of the recording, a window will pop up about\nthe completion of the recording.\n8. Then you click on the listen button and listen to your recording.\nAfter you have listened to the recording, press\nthe stop button. A window pops up in which you choose\nto keep your entry, or delete and overwrite.\n\nVersion: 1.1";
            }
        }

        private void tbHelp_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
