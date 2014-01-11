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

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for UserControlFileBrowser.xaml
    /// </summary>
    public partial class UserControlFileBrowser : UserControl
    {
        private List<FileNode> files = new List<FileNode>();

        public UserControlFileBrowser()
        {
            InitializeComponent();
            gridFileList.ItemsSource = GetFileList();
        }

        private List<FileNode> GetFileList()
        {
            return files;
        }

        private void UpdateList()
        {
            files.Clear();
            List<FileNode> tempList = ConnectionManager.Instance.ListFiles(textBoxPath.Text);
            if (tempList == null)
            {
                MessageBox.Show("Unable to connect!", "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                foreach (FileNode file in tempList)
                {
                    files.Add(file);
                }
            }
            gridFileList.Items.Refresh();
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

        private void gridFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            if (row == null)
                return;

            FileNode file = (FileNode)row.Item;
            if (file == null)
                return;

            if (file.FileType == FileType.Directory)
            {
                textBoxPath.Text += file.Filename + "/";
                UpdateList();
            }
        }
    }
}
