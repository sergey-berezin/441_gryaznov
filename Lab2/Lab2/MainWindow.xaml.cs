using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using Microsoft.Data.Sqlite;
using ModelLibrary;
using Ookii.Dialogs.Wpf;

namespace Lab2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static DetectedObjectContext db = new DetectedObjectContext();
        public static ObservableCollection<DetectedObject> resultCollection = db.DetectedObjects.Local.ToObservableCollection();

        public static async Task Consumer()
        {
            while (true)
            {
                DetectedObject? obj;

                obj = await Detection.resultBufferBlock.ReceiveAsync();
                if (obj == null)
                {
                    db.SaveChanges();
                    MessageBox.Show("Ready");
                    break;
                }
                else
                {
                    db.AddElem(obj);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = resultCollection;
            foreach (var elem in db.DetectedObjects.ToArray())
            {
                resultCollection.Add(elem);
            }
            //Obj.ItemsSource = db.DetectedObjects.ToArray();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if ((bool)dialog.ShowDialog())
                TextBox_Path.Text = dialog.SelectedPath;
        }

        private async void btnRun_Click(object sender, RoutedEventArgs e)
        {
            btnRun.IsEnabled = false;
            //db.Clear();
            Detection.cancelTokenSource = new CancellationTokenSource();
            Detection.token = Detection.cancelTokenSource.Token;

            await Task.WhenAll(Detection.Detect(TextBox_Path.Text, 3), Consumer());
            //Obj.ItemsSource = db.DetectedObjects.ToArray();
            btnRun.IsEnabled = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Detection.cancelTokenSource.Cancel();
            btnRun.IsEnabled = true;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var obj = Obj.SelectedItem as DetectedObject;
            var objId = obj.DetectedObjectId;
            db.Delete(objId);
            resultCollection.Remove(obj);
            MessageBox.Show("Deleted");
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            db.Clear();
            
        }
    }
}
