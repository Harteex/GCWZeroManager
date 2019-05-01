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
using System.ComponentModel;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for UserControlManageSoftware.xaml
    /// </summary>
    public partial class UserControlManageSoftware : UserControl
    {
        private List<OPKFile> files = new List<OPKFile>();

        public UserControlManageSoftware()
        {
            InitializeComponent();
            gridSoftwareList.ItemsSource = new ListCollectionView(files);

            gridSoftwareList.ColumnFromDisplayIndex(0).SortDirection = ListSortDirection.Ascending;
            ICollectionView view = CollectionViewSource.GetDefaultView(gridSoftwareList.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
            view.Refresh();
        }

        private void UpdateList()
        {
            if (!ConnectionManager.Instance.IsConnected)
            {
                if (!ConnectionManager.Instance.Connect())
                {
                    MessageBox.Show("Unable to connect!", "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            files.Clear();
            List<OPKFile> tempList = ConnectionManager.Instance.ListOPKs();
            if (tempList == null)
            {
                MessageBox.Show("File listing failed!", "Listing failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                foreach (OPKFile file in tempList)
                {
                    files.Add(file);
                }
            }

            ((ListCollectionView)this.gridSoftwareList.ItemsSource).Refresh();
        }

        private void UninstallSelected()
        {
            if (gridSoftwareList.SelectedIndex == -1)
            {
                MessageBox.Show("No software was selected - nothing to uninstall", "No software selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            List<OPKFile> selectedFiles = new List<OPKFile>();
            string selectedFilesString = "";

            foreach (object o in gridSoftwareList.SelectedItems)
            {
                OPKFile opk = (OPKFile)o;
                selectedFiles.Add(opk);
                selectedFilesString += opk.Filename + "\n";
            }

            MessageBoxResult result = MessageBox.Show("Are you sure you want to uninstall the following software?\n\n" + selectedFilesString, "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ConnectionManager.Instance.DeleteFiles(selectedFiles);

                UpdateList();
                gridSoftwareList.Items.Refresh();
                MessageBox.Show("Operation complete", "Operation complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void buttonUninstall_Click(object sender, RoutedEventArgs e)
        {
            UninstallSelected();
        }

        private void menuItemUninstall_Click(object sender, RoutedEventArgs e)
        {
            UninstallSelected();
        }

        private void menuItemDownload_Click(object sender, RoutedEventArgs e)
        {
            if (gridSoftwareList.SelectedIndex == -1)
            {
                MessageBox.Show("No software was selected - nothing to download", "No software selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var selectedFiles = new List<string>();

            foreach (object o in gridSoftwareList.SelectedItems)
            {
                OPKFile opk = (OPKFile)o;
                selectedFiles.Add(opk.Filename);
            }

            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();

            var result = folderBrowser.ShowDialog();
            if (result.HasValue && result.Value)
            {
                TransferProgressWindow transferWindow = new TransferProgressWindow();
                transferWindow.DownloadFiles(ConnectionManager.Instance.OPKDirectory, selectedFiles, folderBrowser.SelectedPath);
                var resultTransfer = transferWindow.ShowDialog();

                if (resultTransfer.HasValue && resultTransfer.Value)
                {
                    gridSoftwareList.SelectedIndex = -1;
                }
                else
                {
                    if (!transferWindow.WasCancelled)
                        MessageBox.Show("Download failed: " + transferWindow.ErrorMessage, "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (transferWindow.IsConnectionError)
                {
                    ConnectionManager.Instance.Disconnect(true);
                }
            }
        }
    }
}
