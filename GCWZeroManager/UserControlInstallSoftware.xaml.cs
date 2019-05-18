using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using Renci.SshNet;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for UserControlInstallSoftware.xaml
    /// </summary>
    public partial class UserControlInstallSoftware : UserControl
    {
        private List<OPKFile> opkFiles = new List<OPKFile>();
        TransferProgressWindow transferWindow;

        public UserControlInstallSoftware()
        {
            InitializeComponent();
            gridPendingInstall.ItemsSource = GetOPKFiles();
        }

        private List<OPKFile> GetOPKFiles()
        {
            return opkFiles;
        }

        private void AddOPKFile(string path, long size)
        {
            OPKFile opk = new OPKFile()
            {
                LocalPath = path,
                Filename = System.IO.Path.GetFileName(path),
                Title = System.IO.Path.GetFileName(path),
                Size = new SizeElement(size)
            };
            opkFiles.Add(opk);
        }

        private bool IsDropOk(DragEventArgs e)
        {
            bool dropOk = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true) == true)
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, true);
                foreach (string filename in filenames)
                {
                    // Check if the file is already in the list
                    if (opkFiles.Any(opkFile => opkFile.LocalPath == filename))
                    {
                        continue;
                    }
                    if (File.Exists(filename) == false)
                    {
                        continue;
                    }
                    FileInfo info = new FileInfo(filename);
                    if (info.Extension != ".opk")
                    {
                        continue;
                    }

                    dropOk = true;
                }
            }

            return dropOk;
        }

        private void gridPendingInstall_DragEnter(object sender, DragEventArgs e)
        {
            if (IsDropOk(e))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true; 
        }

        private void gridPendingInstall_DragOver(object sender, DragEventArgs e)
        {
            if (IsDropOk(e))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true; 
        }

        private void gridPendingInstall_Drop(object sender, DragEventArgs e)
        {
            string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            foreach (string filename in filenames)
            {
                FileInfo fi = new FileInfo(filename);
                AddOPKFile(filename, fi.Length);
            }

            gridPendingInstall.Items.Refresh();
            e.Handled = true;
        }

        private void buttonInstall_Click(object sender, RoutedEventArgs e)
        {
            if (opkFiles.Count == 0)
            {
                MessageBox.Show("There are no OPKs in the list to upload. Please add OPK files by dragndrop or by clicking the Add OPK button and try again.", "No OPKs to upload", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            transferWindow = new TransferProgressWindow();
            bool? result = false;
            if (transferWindow.UploadFiles(opkFiles, ConnectionManager.Instance.OPKDirectory))
            {
                result = transferWindow.ShowDialog();
            }

            if (result.HasValue && result.Value)
            {
                opkFiles.Clear();
                gridPendingInstall.Items.Refresh();
            }
            else
            {
                if (!transferWindow.WasCancelled)
                    MessageBox.Show("Upload failed: " + transferWindow.ErrorMessage, "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (transferWindow.IsConnectionError)
            {
                ConnectionManager.Instance.Disconnect(true);
            }
        }

        private void buttonAddOpk_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfigurationManager.Instance.Settings.HasShownDragNDropNoticeInstallOpk)
            {
                MessageBox.Show("Did you know: You can also add OPKs by simply using drag'n'drop. This notice will not be shown again.", "Did you know", MessageBoxButton.OK, MessageBoxImage.Information);
                ConfigurationManager.Instance.Settings.HasShownDragNDropNoticeInstallOpk = true;
                ConfigurationManager.Instance.SaveSettings();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.DefaultExt = ".opk";
            openFileDialog.Filter = "Open Package (.opk)|*.opk";
            openFileDialog.Multiselect = true;

            Nullable<bool> result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    FileInfo fi = new FileInfo(filename);
                    AddOPKFile(filename, fi.Length);
                }

                gridPendingInstall.Items.Refresh();
            }
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (gridPendingInstall.SelectedIndex == -1)
                return;

            List<OPKFile> toRemove = new List<OPKFile>();

            foreach (Object o in gridPendingInstall.SelectedItems)
            {
                toRemove.Add((OPKFile)o);
            }

            foreach (OPKFile opk in toRemove)
            {
                opkFiles.Remove(opk);
            }

            gridPendingInstall.Items.Refresh();
        }
    }
}
