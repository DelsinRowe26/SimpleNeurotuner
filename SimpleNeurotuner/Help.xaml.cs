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

        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (index == "0")
            {
                Title = "Помощь";
                tbHelp.Text = "Добро пожаловать в Нейрокейс.\n\nИнструкция:\n1. Если по умолчанию не выбраны микрофон и колонки(наушники), то выберите их самостоятельно.\n2. Выбираем запись.\n3. Нажимаем кнопку старт, и наслаждаемся.Возникло желание остановить нажимаете кнопку стоп.\n\nВерсия: 1.0";
            }
            else
            {
                Title = "Help";
                tbHelp.Text = "Welcome to Neurokeys.\n\nInstruction:\n1. If the microphone and speakers (headphones) are not selected by default, then select them yourself.\n2. Select an entry.\n3. Press the start button, and enjoy. There was a desire to stop, press the stop button.\n\nVersion: 1.0";
            }
        }

        private void Help_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader FileLanguage = new StreamReader("Data_Language.dat");
            index = FileLanguage.ReadToEnd();
            if (index == "0")
            {
                Title = "Помощь";
                tbHelp.Text = "Добро пожаловать в Нейрокейс.\n\nИнструкция:\n1. Если по умолчанию не выбраны микрофон и колонки(наушники), то выберите их самостоятельно.\n2. Выбираем запись.\n3. Нажимаем кнопку старт, и наслаждаемся.Возникло желание остановить нажимаете кнопку стоп.\n\nВерсия: 1.0";
            }
            else
            {
                Title = "Help";
                tbHelp.Text = "Welcome to Neurokeys.\n\nInstruction:\n1. If the microphone and speakers (headphones) are not selected by default, then select them yourself.\n2. Select an entry.\n3. Press the start button, and enjoy. There was a desire to stop, press the stop button.\n\nVersion: 1.0";
            }
        }
    }
}
