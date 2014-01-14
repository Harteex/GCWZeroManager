﻿using System;
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
using Renci.SshNet.Common;

namespace GCWZeroManager
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
                SshClient ssh = ConnectionManager.Instance.GetActiveSshConnection();
                SshCommand cmd = ssh.CreateCommand("uname -v");
                cmd.Execute();
                labelInstalledVerContent.Content = cmd.Result;
            }
            catch (SshOperationTimeoutException ex)
            {
                MessageBox.Show("Operation failed: " + ex.Message, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectionManager.Instance.Disconnect(true);
            }
        }

        private void buttonSystemInfo_Click(object sender, RoutedEventArgs e)
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
                SshClient ssh = ConnectionManager.Instance.GetActiveSshConnection();
                SshCommand cmd;

                textBoxSystemInfo.Text = "";

                textBoxSystemInfo.Text += "*********************************\n";
                textBoxSystemInfo.Text += "********** system_info **********\n";
                textBoxSystemInfo.Text += "*********************************\n\n";

                cmd = ssh.CreateCommand("/usr/bin/system_info");
                cmd.Execute();
                textBoxSystemInfo.Text += cmd.Result;
                textBoxSystemInfo.Text += "\n";

                textBoxSystemInfo.Text += "*********************************\n";
                textBoxSystemInfo.Text += "*********** ifconfig ************\n";
                textBoxSystemInfo.Text += "*********************************\n\n";

                cmd = ssh.CreateCommand("/sbin/ifconfig -a");
                cmd.Execute();
                textBoxSystemInfo.Text += cmd.Result;
                textBoxSystemInfo.Text += "\n";

                textBoxSystemInfo.Text += "*********************************\n";
                textBoxSystemInfo.Text += "************ Battery ************\n";
                textBoxSystemInfo.Text += "*********************************\n\n";

                cmd = ssh.CreateCommand("cat /sys/class/power_supply/battery/uevent");
                cmd.Execute();
                textBoxSystemInfo.Text += cmd.Result;
                textBoxSystemInfo.Text += "\n";

                string connectedDc;
                string connectedUsb;
                cmd = ssh.CreateCommand("cat /sys/class/power_supply/dc/online");
                cmd.Execute();
                connectedDc = cmd.Result.Trim();
                cmd = ssh.CreateCommand("cat /sys/class/power_supply/usb/online");
                cmd.Execute();
                connectedUsb = cmd.Result.Trim();

                /*textBoxSystemInfo.Text += "DC Power: " + (connectedDc == "1" ? "Connected" : "Not connected") + "\n";
                textBoxSystemInfo.Text += "USB Power: " + (connectedUsb == "1" ? "Connected" : "Not connected") + "\n";*/

                textBoxSystemInfo.Text += "Connected Power Source:\n";
                textBoxSystemInfo.Text += "DC Power: " + connectedDc + "\n";
                textBoxSystemInfo.Text += "USB Power: " + connectedUsb + "\n";

                textBoxSystemInfo.Text += "\n";

                textBoxSystemInfo.Text += "*********************************\n";
                textBoxSystemInfo.Text += "************* dmesg *************\n";
                textBoxSystemInfo.Text += "*********************************\n\n";

                cmd = ssh.CreateCommand("dmesg");
                cmd.Execute();
                textBoxSystemInfo.Text += cmd.Result;
                textBoxSystemInfo.Text += "\n";
            }
            catch (SshOperationTimeoutException ex)
            {
                MessageBox.Show("Operation failed: " + ex.Message, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectionManager.Instance.Disconnect(true);
            }
        }

        private void buttonCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(DataFormats.Text, textBoxSystemInfo.Text);
        }

        private void buttonVerifyIntegrity_Click(object sender, RoutedEventArgs e)
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
                SshClient ssh = ConnectionManager.Instance.GetActiveSshConnection();
                SshCommand cmd;

                string actualSha1;
                string expectedSha1;
                string outMsg = "";

                cmd = ssh.CreateCommand("sha1sum /media/data/rootfs.bin");
                cmd.Execute();
                actualSha1 = cmd.Result.Split(new char[] { ' ' })[0].Trim();

                cmd = ssh.CreateCommand("cat /media/data/rootfs.bin.sha1");
                cmd.Execute();
                expectedSha1 = cmd.Result.Split(new char[] { ' ' })[0].Trim();

                outMsg += "Actual: " + actualSha1 + "\n";
                outMsg += "Expected: " + expectedSha1 + "\n";
                if (actualSha1 == expectedSha1)
                    outMsg += "Rootfs SHA1 OK\n";
                else
                    outMsg += "Rootfs SHA1 MISMATCH!\n";

                MessageBox.Show(outMsg, "Integrity Check", MessageBoxButton.OK, (actualSha1 == expectedSha1) ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (SshOperationTimeoutException ex)
            {
                MessageBox.Show("Operation failed: " + ex.Message, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectionManager.Instance.Disconnect(true);
            }
        }

        private void buttonSystemLog_Click(object sender, RoutedEventArgs e)
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
                SshClient ssh = ConnectionManager.Instance.GetActiveSshConnection();
                SshCommand cmd;

                textBoxSystemInfo.Text = "";

                cmd = ssh.CreateCommand("cat /var/log/messages");
                cmd.Execute();
                textBoxSystemInfo.Text += cmd.Result;
                textBoxSystemInfo.Text += "\n";
            }
            catch (SshOperationTimeoutException ex)
            {
                MessageBox.Show("Operation failed: " + ex.Message, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectionManager.Instance.Disconnect(true);
            }
        }
    }
}
