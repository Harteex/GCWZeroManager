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
            gridFileList.ItemsSource = new ListCollectionView(files);

            gridFileList.ColumnFromDisplayIndex(1).SortDirection = ListSortDirection.Ascending;
            ICollectionView view = CollectionViewSource.GetDefaultView(gridFileList.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Filename", ListSortDirection.Ascending));
            view.Refresh();
        }

        private void UpdateList()
        {
            if (!ConnectionManager.Instance.Connected)
            {
                if (!ConnectionManager.Instance.Connect())
                {
                    MessageBox.Show("Unable to connect!", "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            files.Clear();
            List<FileNode> tempList = ConnectionManager.Instance.ListFiles(textBoxPath.Text); // FIXME Handle SftpPathNotFoundException!!!
            if (tempList == null)
            {
                MessageBox.Show("File listing failed!", "Listing failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                foreach (FileNode file in tempList)
                {
                    files.Add(file);
                }
            }

            ((ListCollectionView)this.gridFileList.ItemsSource).Refresh();

            lastOkPath = textBoxPath.Text;
        }

        private void gridFileList_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void gridFileList_DragOver(object sender, DragEventArgs e)
        {

        }

        private void gridFileList_Drop(object sender, DragEventArgs e)
        {

        }

        private void buttonHome_Click(object sender, RoutedEventArgs e)
        {
            textBoxPath.Text = "/usr/local/home/";
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

        }

        private void buttonNewFolder_Click(object sender, RoutedEventArgs e)
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

            if (file.FileType == FileType.Directory || file.FileType == FileType.SymLink) // FIXME not all symlinks can be followed I guess.......?
            {
                textBoxPath.Text += file.Filename + "/";
                UpdateList();
            }
        }
    }
}
