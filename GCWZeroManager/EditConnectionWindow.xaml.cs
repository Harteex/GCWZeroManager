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
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for EditConnectionWindow.xaml
    /// </summary>
    public partial class EditConnectionWindow : Window
    {
        private ConnectionNode connectionNode = new ConnectionNode();


        public EditConnectionWindow()
        {
            InitializeComponent();
        }

        public EditConnectionWindow(ConnectionNode connectionNode)
            : this()
        {
            this.connectionNode = connectionNode;

            SetValuesGui(connectionNode);
        }

        private void SetValuesGui(ConnectionNode cn)
        {
            textBoxHost.Text = cn.Host == null ? "" : cn.Host;
            textBoxKeys.Text = cn.PrivateKey == null ? "" : cn.PrivateKey;
        }

        private bool SaveValues(ConnectionNode cn)
        {
            // Get all values
            string ip = textBoxHost.Text;
            string keyPath = textBoxKeys.Text;

            // Verify values somewhat
            if (ip == "")
            {
                MessageBox.Show("Please enter a host / IP to connect to", "Host field empty", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            if (keyPath == "")
            {
                MessageBox.Show("Please enter the path to your private key", "Private key field empty", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            if (!File.Exists(keyPath))
            {
                MessageBox.Show("The private key file could not be found at the specified path", "Private key not found", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            // Set them in the object
            cn.Host = ip;
            cn.PrivateKey = keyPath;
            cn.AuthenticationMethod = AuthenticationMethod.PrivateKey;
            cn.Password = null;

            return true;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveValues(connectionNode))
            {
                e.Handled = true;
                return;
            }

            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public ConnectionNode ConnectionNode
        {
            get { return connectionNode; }
            set { connectionNode = value; }
        }

        private void buttonBrowseKey_Click(object sender, RoutedEventArgs e)
        {
            string keyDir = System.IO.Path.GetDirectoryName(textBoxKeys.Text);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (Directory.Exists(keyDir))
                openFileDialog.InitialDirectory = keyDir;
            Nullable<bool> result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                textBoxKeys.Text = openFileDialog.FileName;
            }
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            ConnectionNode tempCn = new ConnectionNode();
            if (SaveValues(tempCn))
            {
                if (ConnectionManager.Instance.TestConnection(tempCn))
                {
                    MessageBox.Show("Connection appears to work fine", "Connection OK", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Unable to connect", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void buttonSetupKeys_Click(object sender, RoutedEventArgs e)
        {
            SetupKeysWindow window = new SetupKeysWindow();
            window.PreSetHost(connectionNode.Host);
            window.ShowDialog();
            if (window.DialogResult.HasValue && window.DialogResult.Value)
            {
                SetValuesGui(window.ConnectionNode);
            }
        }
    }
}
