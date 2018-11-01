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
using Renci.SshNet.Common;
using System.IO;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for UserControlFileBrowser.xaml
    /// </summary>
    public partial class UserControlFileBrowser : UserControl
    {
        private List<FileNode> files = new List<FileNode>();
        string lastOkPath = "/";

        public UserControlFileBrowser()
        {
            InitializeComponent();
            textBoxPath.Text = ConnectionManager.Instance.HomeDirectory;
            gridFileList.ItemsSource = new ListCollectionView(files);
            
            gridFileList.ColumnFromDisplayIndex(1).SortDirection = ListSortDirection.Ascending;
            ICollectionView view = CollectionViewSource.GetDefaultView(gridFileList.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Filename", ListSortDirection.Ascending));
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

            try
            {
                List<FileNode> tempList = ConnectionManager.Instance.ListFiles(textBoxPath.Text);
                if (tempList == null)
                {
                    MessageBox.Show("File listing failed!", "Listing failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    files.Clear();
                    foreach (FileNode file in tempList)
                    {
                        files.Add(file);
                    }
                    lastOkPath = textBoxPath.Text;
                }
            }
            catch (SftpPathNotFoundException)
            {
                if (textBoxPath.Text == lastOkPath)
                {
                    MessageBox.Show("File listing failed!", "Listing failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Could not open directory!", "Listing failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxPath.Text = lastOkPath;
                }
            }
            catch (SshOperationTimeoutException)
            {
                MessageBox.Show("Connection timed out while listing files.", "Listing failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectionManager.Instance.Disconnect(true);
            }

            ((ListCollectionView)this.gridFileList.ItemsSource).Refresh();
            if (gridFileList.HasItems)
                gridFileList.ScrollIntoView(gridFileList.Items[0]);
        }

        private void gridFileList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true; 
        }

        private void gridFileList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true; 
        }

        private void gridFileList_Drop(object sender, DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            List<FileUploadDownloadNode> filesToUpload = new List<FileUploadDownloadNode>();
            foreach (string path in paths)
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists)
                {
                    MessageBox.Show("Error: " + fi.Name + " is a directory or the file doesn't exist.", "Cannot Upload", MessageBoxButton.OK, MessageBoxImage.Stop);
                    e.Handled = true;
                    return;
                }
                FileUploadDownloadNode fileUploadNode = new FileUploadDownloadNode();
                fileUploadNode.Path = path;
                fileUploadNode.Filename = System.IO.Path.GetFileName(path);
                fileUploadNode.Size = new SizeElement(fi.Length);

                filesToUpload.Add(fileUploadNode);


            }

            TransferProgressWindow transferWindow = new TransferProgressWindow();
            bool? result = false;
            if (transferWindow.TryUploadFiles(filesToUpload, textBoxPath.Text))
            {
                result = transferWindow.ShowDialog();
            }

            if (!result.HasValue || !result.Value)
            {
                MessageBox.Show("Upload failed: " + transferWindow.ErrorMessage, "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (transferWindow.IsConnectionError)
            {
                ConnectionManager.Instance.Disconnect(true);
            }
            else
            {
                UpdateList();
            }

            e.Handled = true;
        }

        private void buttonHome_Click(object sender, RoutedEventArgs e)
        {
            textBoxPath.Text = ConnectionManager.Instance.HomeDirectory;
            UpdateList();
        }

        private void buttonParentDir_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxPath.Text == "/")
                return;

            int slashIndex = textBoxPath.Text.Substring(0, textBoxPath.Text.Length - 1).LastIndexOf('/');
            textBoxPath.Text = textBoxPath.Text.Substring(0, slashIndex + 1);
            UpdateList();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            DoDelete();
        }

        private void buttonNewFolder_Click(object sender, RoutedEventArgs e)
        {
            DoCreateNewFolder();
        }

        private void DoDelete()
        {
            if (gridFileList.SelectedIndex == -1)
            {
                MessageBox.Show("No files selected - nothing to delete", "No files selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            List<string> selectedFiles = new List<string>();
            List<string> selectedDirectories = new List<string>();
            string selectedString = "";

            foreach (Object o in gridFileList.SelectedItems)
            {
                FileNode fileNode = (FileNode)o;
                if (fileNode.FileType == FileType.RegularFile)
                    selectedFiles.Add(System.IO.Path.Combine(textBoxPath.Text, fileNode.Filename));
                else if (fileNode.FileType == FileType.Directory)
                    selectedDirectories.Add(System.IO.Path.Combine(textBoxPath.Text, fileNode.Filename));
                else
                {
                    MessageBox.Show("Cannot delete " + fileNode.Filename + ".", "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                selectedString += fileNode.Filename + "\n";
            }

            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the following files and/or directories?\n\n" + selectedString, "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ConnectionManager.Instance.DeleteFiles(selectedFiles);
                }
                catch (SshOperationTimeoutException ex)
                {
                    MessageBox.Show("Delete failed: " + ex.Message, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectionManager.Instance.Disconnect(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Delete failed: " + ex.Message, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                try
                {
                    ConnectionManager.Instance.DeleteDirectories(selectedDirectories);
                }
                catch (SshOperationTimeoutException ex)
                {
                    MessageBox.Show("Delete failed: " + ex.Message, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectionManager.Instance.Disconnect(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Delete failed: " + ex.Message + ". Make sure the directory is empty.", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (ConnectionManager.Instance.IsConnected)
                    UpdateList();
            }
        }

        private void DoCreateNewFolder()
        {
            TextInputDialog input = new TextInputDialog("New Folder", "Enter a name for the new folder", "Name:");
            input.ShowDialog();
            if (input.DialogResult.HasValue && input.DialogResult.Value)
            {
                try
                {
                    ConnectionManager.Instance.CreateFolder(textBoxPath.Text, input.InputText);
                    UpdateList();
                }
                catch (SshOperationTimeoutException ex)
                {
                    MessageBox.Show("Error creating folder: " + ex.Message, "Error Creating Folder", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectionManager.Instance.Disconnect(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating folder: " + ex.Message, "Error Creating Folder", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void gridFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            if (row == null)
                return;

            FileNode file = (FileNode)row.Item;
            if (file == null)
                return;

            if (file.FileType == FileType.Directory || file.FileType == FileType.SymLink) // FIXME How to differentiate folder symlinks and file symlinks?
            {
                textBoxPath.Text += file.Filename + "/";
                UpdateList();
            }
        }

        private void DoDownload()
        {
            if (gridFileList.SelectedIndex == -1)
            {
                MessageBox.Show("No files were selected - nothing to download", "No files selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            List<FileUploadDownloadNode> selectedFiles = new List<FileUploadDownloadNode>();

            foreach (Object o in gridFileList.SelectedItems)
            {
                FileNode fileNode = (FileNode)o;
                if (fileNode.FileType == FileType.RegularFile)
                {
                    FileUploadDownloadNode fileUpload = new FileUploadDownloadNode();
                    fileUpload.Filename = fileNode.Filename;
                    fileUpload.Size = fileNode.Size;
                    fileUpload.Path = System.IO.Path.Combine(textBoxPath.Text, fileNode.Filename);
                    selectedFiles.Add(fileUpload);
                }
                else
                {
                    MessageBox.Show("Cannot download " + fileNode.Filename + ". Only regular files can be downloaded.", "Cannot download", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
            }

            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();

            bool? result = folderBrowser.ShowDialog();

            if (result.HasValue && result.Value)
            {
                TransferProgressWindow transferWindow = new TransferProgressWindow();
                transferWindow.DownloadFiles(selectedFiles, folderBrowser.SelectedPath);
                bool? resultTransfer = transferWindow.ShowDialog();

                if (resultTransfer.HasValue && resultTransfer.Value)
                {
                    //
                }
                else
                {
                    MessageBox.Show("Download failed: " + transferWindow.ErrorMessage, "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (transferWindow.IsConnectionError)
                {
                    ConnectionManager.Instance.Disconnect(true);
                }
            }
        }

        private void DoUpload()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;

            bool? result = openFileDialog.ShowDialog();

            List<FileUploadDownloadNode> filesToUpload = new List<FileUploadDownloadNode>();
            if (result.HasValue && result.Value)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    FileInfo fi = new FileInfo(filename);
                    FileUploadDownloadNode fileUpload = new FileUploadDownloadNode();
                    fileUpload.Filename = System.IO.Path.GetFileName(filename);
                    fileUpload.Path = filename;
                    fileUpload.Size = new SizeElement(fi.Length);

                    filesToUpload.Add(fileUpload);
                }

                TransferProgressWindow transferWindow = new TransferProgressWindow();
                bool? resultTransfer = false;
                if (transferWindow.TryUploadFiles(filesToUpload, textBoxPath.Text))
                {
                    resultTransfer = transferWindow.ShowDialog();
                }

                if (!resultTransfer.HasValue || !resultTransfer.Value)
                {
                    MessageBox.Show("Upload failed: " + transferWindow.ErrorMessage, "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (transferWindow.IsConnectionError)
                {
                    ConnectionManager.Instance.Disconnect(true);
                }
                else
                {
                    UpdateList();
                }
            }
        }

        private void buttonDownload_Click(object sender, RoutedEventArgs e)
        {
            DoDownload();
        }

        private void buttonUpload_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfigurationManager.Instance.Settings.HasShownDragNDropNoticeFiles)
            {
                MessageBox.Show("Did you know: You can also upload files by simply using drag'n'drop. This notice will not be shown again.", "Did you know", MessageBoxButton.OK, MessageBoxImage.Information);
                ConfigurationManager.Instance.Settings.HasShownDragNDropNoticeFiles = true;
                ConfigurationManager.Instance.SaveSettings();
            }

            DoUpload();
        }

        private void menuItemRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void menuItemNewFolder_Click(object sender, RoutedEventArgs e)
        {
            DoCreateNewFolder();
        }

        private void menuItemDownload_Click(object sender, RoutedEventArgs e)
        {
            DoDownload();
        }

        private void menuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            DoDelete();
        }

        private void gridFileList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    DoDelete();
                    e.Handled = true;
                    break;
            }
        }
    }
}
