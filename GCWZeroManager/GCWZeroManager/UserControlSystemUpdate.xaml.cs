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
using Renci.SshNet;
using System.Diagnostics;

namespace GCWZeroManager2
{
    /// <summary>
    /// Interaction logic for UserControlSystemUpdate.xaml
    /// </summary>
    public partial class UserControlSystemUpdate : UserControl
    {
        public UserControlSystemUpdate()
        {
            InitializeComponent();
        }

        private void buttonReadInstalledVer_Click(object sender, RoutedEventArgs e)
        {
            SshClient ssh = ConnectionManager.Instance.ConnectWithActiveConnectionSSH();
            if (ssh == null || !ssh.IsConnected)
            {
                MessageBox.Show("Unable to connect!", "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SshCommand cmd = ssh.CreateCommand("uname -v");
            cmd.Execute();
            labelInstalledVerContent.Content = cmd.Result;
        }

        private void buttonDownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.gcw-zero.com/updates");
        }
    }
}
