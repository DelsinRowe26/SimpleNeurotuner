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
    /// Логика взаимодействия для SaveDeleteWindow.xaml
    /// </summary>
    public partial class SaveDeleteWindow : Window
    {
        string index;
        public SaveDeleteWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader FileLanguage = new StreamReader("Data_Language.dat");
            index = FileLanguage.ReadToEnd();
            if(index == "0")
            {
                Title = "Окно сохранения/удаления";
                lbSaveDelete.Content = "Сохранить вашу запись или удалить и перезаписать?";
                btnSave.Content = "Сохранить";
                btnDelete.Content = "Удалить";
            }
            else
            {
                Title = "Save/Delete Window";
                lbSaveDelete.Content = "Keep your recording or delete and overwrite?";
                btnSave.Content = "Save";
                btnDelete.Content = "Delete";
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            StreamReader FileCutRecord = new StreamReader("Data_cutCreate.dat");
            string FileCut = FileCutRecord.ReadToEnd();
            File.Delete(FileCut);
            this.Close();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            StreamReader FileRecord = new StreamReader("Data_Create.dat");
            StreamReader FileCutRecord = new StreamReader("Data_cutCreate.dat");
            string myfile = FileRecord.ReadToEnd();
            string cutmyfile = FileCutRecord.ReadToEnd();
            File.Delete(@"Record\" + myfile);
            File.Delete(cutmyfile);
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StreamReader FileRecord = new StreamReader("Data_Create.dat");
            StreamReader FileCutRecord = new StreamReader("Data_cutCreate.dat");
            string myfile = FileRecord.ReadToEnd();
            string cutmyfile = FileCutRecord.ReadToEnd();
            //File.Delete(@"Record\" + myfile);
            //File.Delete(cutmyfile);
        }
    }
}
