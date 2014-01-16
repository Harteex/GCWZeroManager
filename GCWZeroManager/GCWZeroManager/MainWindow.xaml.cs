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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserControl[] userControls = new UserControl[] { new UserControlInstallSoftware(), new UserControlManageSoftware(), new UserControlFileBrowser(), new UserControlSystemUpdate(), new UserControlAbout() };

        public MainWindow()
        {
            InitializeComponent();
            LoadConfiguration();
            listBoxMenu.SelectedIndex = 0;
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            contentControl1.Content = userControls[((ListBox)sender).SelectedIndex];
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            ConnectionNode activeCn = ConnectionManager.Instance.Connections.GetActiveConnection();
            if (activeCn == null)
                return;

            EditConnectionWindow window = new EditConnectionWindow(activeCn);
            window.ShowDialog();
            if (window.DialogResult.HasValue && window.DialogResult.Value)
            {
                comboBoxConnections.Items.Refresh();
                comboBoxConnections.SelectedItem = null;
                comboBoxConnections.SelectedItem = activeCn;
                ConfigurationManager.Instance.SaveConnections();
            }
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            SetupKeysWindow window = new SetupKeysWindow();
            window.Title = "Add Connection Wizard";
            window.ShowDialog();
            if (window.DialogResult.HasValue && window.DialogResult.Value)
            {
                ConnectionNode cn = window.ConnectionNode;
                ConnectionManager.Instance.AddConnection(cn);
                comboBoxConnections.Items.Refresh();
                comboBoxConnections.SelectedItem = cn;
                ConfigurationManager.Instance.SaveConnections();
            }
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            ConnectionNode activeCn = ConnectionManager.Instance.Connections.GetActiveConnection();
            if (activeCn == null)
                return;

            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the connection '" + activeCn.Host + "'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ConnectionManager.Instance.Connections.DeleteActiveConnection();
                comboBoxConnections.Items.Refresh();
                comboBoxConnections.SelectedItem = ConnectionManager.Instance.Connections.GetActiveConnection();
                ConfigurationManager.Instance.SaveConnections();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConfigurationManager.Instance.SaveConnections();
        }

        private void LoadConfiguration()
        {
            ConfigurationManager.Instance.LoadConnections();
            ConfigurationManager.Instance.LoadSettings();
            comboBoxConnections.ItemsSource = ConnectionManager.Instance.Connections.Connections;
            comboBoxConnections.SelectedItem = ConnectionManager.Instance.Connections.GetActiveConnection();
        }

        private void comboBoxConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectionManager.Instance.Disconnect(false);
            ConnectionManager.Instance.Connections.SetActiveConnection((ConnectionNode)comboBoxConnections.SelectedItem);
            ConfigurationManager.Instance.SaveConnections();
        }
    }
}
