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
    /// Логика взаимодействия для CreateWindow.xaml
    /// </summary>
    public partial class CreateWindow : Window
    {
        public string FileName;
        public string cutFileName;
        private FileInfo fileCreate = new FileInfo("Data_Create.dat");
        private FileInfo fileCutCreate = new FileInfo("Data_cutCreate.dat");

        public CreateWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader FileLanguage = new StreamReader("Data_Language.dat");
            string index = FileLanguage.ReadLine();
            if (index == "0")
            {
                lbRecordTitle.Content = "Название записи";
                Title = "Создание файла";
                btnCreate.Content = "Создать";
            }
            else
            {
                lbRecordTitle.Content = "Record title";
                Title = "Create window";
                btnCreate.Content = "Create";
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            FileName = tbRecordTitle.Text + ".wav";
            cutFileName = "cut" + tbRecordTitle.Text + ".wav";
            File.WriteAllText(fileCreate.FullName, FileName);
            File.WriteAllText(fileCutCreate.FullName, cutFileName);
            //await File.WriteAllTextAsync(fileCreate.FullName, FileName);
            this.Close();
        }
    }
}
