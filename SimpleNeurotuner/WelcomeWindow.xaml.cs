using System;
using System.Collections.Generic;
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
            if(cmbLanguage.SelectedIndex == 1)
            {
                lbWelcome.Content = "Добро пожаловать в МиниНейротюнер";
                lbDescriptionText.Content = "Версия: 1.0\n\nПрограмма предназначена для курса\nпо входу в ресурсное состояние.\n\nИнструкция:\n\n1. Если микрофон и динамики (наушники)\nне выбраны по умолчанию, то выберите их самостоятельно.\n\n2. Выберите запись.\n\n3. Нажмите кнопку запуска и наслаждайтесь.\nВозникло желание остановиться, нажми кнопку стоп.";
            } 
            else
            {
                lbWelcome.Content = "Welcome to MiniNeurotuner";
                lbDescriptionText.Content = "Version: 1.0\n\nThe program is intended for a course on entering the resource state.\n\nInstruction:\n\n1. If the microphone and speakers (headphones) \nare not selected by default, then select them yourself.\n\n2. Select an entry.\n\n3. Press the start button, and enjoy. There was a desire to stop,\npress the stop button.";
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
