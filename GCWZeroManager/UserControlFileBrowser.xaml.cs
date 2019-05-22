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
        TransferProgressWindow transferWindow;

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

            Dispatcher.BeginInvoke((Action)(() =>
            {
                DoUpload(paths);
            }));

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
            string selectedString = "";

            foreach (object o in gridFileList.SelectedItems)
            {
                FileNode fileNode = (FileNode)o;

                // Ignore these rare files since they could cause problems when doing command line rm
                if (fileNode.Filename.Name.Contains("\""))
                    continue;

                selectedFiles.Add(System.IO.Path.Combine(textBoxPath.Text, fileNode.Filename.Name));
                selectedString += fileNode.Filename + "\n";
            }

            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the following files and/or directories?\n\n" + selectedString, "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ConnectionManager.Instance.DeleteMultiple(selectedFiles);
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

        void EnterDirectory(FileNode node)
        {
            if (node == null)
                return;

            if (node.FileType == FileType.Directory || node.FileType == FileType.SymLink) // FIXME How to differentiate folder symlinks and file symlinks?
            {
                textBoxPath.Text += node.Filename + "/";
                UpdateList();
            }
        }

        private void gridFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            if (row == null)
                return;

            FileNode file = (FileNode)row.Item;
            EnterDirectory(file);
        }

        private void DoDownload()
        {
            if (gridFileList.SelectedIndex == -1)
            {
                MessageBox.Show("No files were selected - nothing to download", "No files selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var selectedFiles = new List<string>();

            foreach (object o in gridFileList.SelectedItems)
            {
                FileNode fileNode = (FileNode)o;

                if (fileNode.FileType == FileType.Directory || fileNode.FileType == FileType.RegularFile)
                    selectedFiles.Add(fileNode.Filename.Name);
            }

            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();

            bool? result = folderBrowser.ShowDialog();

            if (result.HasValue && result.Value)
            {
                transferWindow = new TransferProgressWindow();
                transferWindow.DownloadFiles(textBoxPath.Text, selectedFiles, folderBrowser.SelectedPath);
                bool? resultTransfer = transferWindow.ShowDialog();

                if (resultTransfer.HasValue && resultTransfer.Value)
                {
                    //
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

        private void DoUpload(string[] paths)
        {
            var filesToUpload = new TransferDirectory("");

            foreach (string path in paths)
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    filesToUpload.AddFile(new TransferFile(path, fi.Length));
                    continue;
                }

                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists)
                {
                    var subDir = GetFilesInPathRecursive(path);
                    if (subDir != null)
                        filesToUpload.AddDirectory(subDir);
                }
            }

            transferWindow = new TransferProgressWindow();
            bool? result = false;
            if (transferWindow.UploadFiles(filesToUpload, textBoxPath.Text))
            {
                result = transferWindow.ShowDialog();
            }

            if (!transferWindow.WasCancelled && (!result.HasValue || !result.Value))
            {
                MessageBox.Show("Upload failed: " + transferWindow.ErrorMessage, "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if ((!result.HasValue || !result.Value))
            {
                // Quick sleep to let the cleanup functions delete any half transferred file before continuing
                // No big deal if it doesn't finish in time though, in the worst case you'll see the file until you refresh or reenter the directory
                System.Threading.Thread.Sleep(100);
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

        TransferDirectory GetFilesInPathRecursive(string path)
        {
            var di = new DirectoryInfo(path);
            if (!di.Exists)
                return null;

            var directory = new TransferDirectory(di.Name);

            foreach (var file in di.GetFiles())
            {
                directory.AddFile(new TransferFile(file.FullName, file.Length));
            }

            foreach (var dir in di.GetDirectories())
            {
                var subDir = GetFilesInPathRecursive(dir.FullName);
                if (subDir != null)
                    directory.AddDirectory(subDir);
            }

            return directory;
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

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;

            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                DoUpload(openFileDialog.FileNames);
            }
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

        private void menuItemRename_Click(object sender, RoutedEventArgs e)
        {
            DoRename();
        }

        void DoRename()
        {
            if (gridFileList.SelectedIndex == -1)
            {
                MessageBox.Show("No file selected", "No file selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (gridFileList.SelectedItems.Count > 1)
            {
                MessageBox.Show("You can only rename one file at a time", "Many files selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            FileNode fileNode = gridFileList.SelectedItems[0] as FileNode;
            if (fileNode == null)
            {
                MessageBox.Show("An error occured trying to get the file path", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var oldPath = System.IO.Path.Combine(textBoxPath.Text, fileNode.Filename.Name);

            TextInputDialog input = new TextInputDialog("Rename", "Enter a new name", "Name:", fileNode.Filename.Name);

            var fileNameExtensionPos = fileNode.Filename.Name.LastIndexOf('.');
            if (fileNameExtensionPos > 0)
                input.SetTextBoxSelection(0, fileNameExtensionPos);

            input.ShowDialog();
            if (input.DialogResult.HasValue && input.DialogResult.Value)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(input.InputText))
                    {
                        var newPath = System.IO.Path.Combine(textBoxPath.Text, input.InputText);
                        ConnectionManager.Instance.RenameFile(oldPath, newPath);
                        UpdateList();
                    }
                }
                catch (SshOperationTimeoutException ex)
                {
                    MessageBox.Show("Failed to rename item: " + ex.Message, "Could Not Rename Item", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectionManager.Instance.Disconnect(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to rename item: " + ex.Message, "Could Not Rename Item", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void gridFileList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    DoDelete();
                    e.Handled = true;
                    break;
                case Key.F2:
                    DoRename();
                    e.Handled = true;
                    break;
                case Key.Return:
                    EnterDirectory(gridFileList.SelectedItem as FileNode);
                    e.Handled = true;
                    break;
            }
        }
    }
}
