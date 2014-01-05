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

namespace GCWZeroManager2
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
            gridSoftwareList.ItemsSource = GetSoftwareList();
        }

        private List<OPKFile> GetSoftwareList()
        {
            return files;
        }

        private void UpdateList()
        {
            files.Clear();
            List<OPKFile> tempList = ConnectionManager.Instance.GetOPKList();
            if (tempList == null)
            {
                MessageBox.Show("Unable to connect!", "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                foreach (OPKFile file in tempList)
                {
                    files.Add(file);
                }
            }
            gridSoftwareList.Items.Refresh();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void buttonUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (gridSoftwareList.SelectedIndex == -1)
            {
                MessageBox.Show("No software was selected - nothing to uninstall", "No software selected", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            List<OPKFile> selectedFiles = new List<OPKFile>();
            string selectedFilesString = "";

            foreach (Object o in gridSoftwareList.SelectedItems)
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
    }
}
