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
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace GCWZeroManager
{
    public enum KeyOption { GenerateKeys, InstallKeys, NoInstall };
    /// <summary>
    /// Interaction logic for UserControlSetupKeysKeyOption.xaml
    /// </summary>
    public partial class UserControlSetupKeysKeyOption : UserControl
    {
        string alreadyGeneratedKeyPath = null;

        public UserControlSetupKeysKeyOption()
        {
            InitializeComponent();
            EnableDisableFields();
        }

        private void radioButtonCreateInstall_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                EnableDisableFields();
        }

        private void radioButtonInstall_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                EnableDisableFields();
        }

        private void radioButtonNoInstall_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                EnableDisableFields();
        }

        private void EnableFieldsInstall(bool enable)
        {
            textBoxPublicKey.IsEnabled = enable;
            textBoxPrivateKey.IsEnabled = enable;
            buttonBrowsePublicKey.IsEnabled = enable;
            buttonBrowsePrivateKey.IsEnabled = enable;
        }

        private void EnableFieldsNoInstall(bool enable)
        {
            textBoxPrivateKeyNoInstall.IsEnabled = enable;
            buttonBrowseKeyNoInstall.IsEnabled = enable;
        }

        private void EnableDisableFields()
        {
            
            if (radioButtonCreateInstall.IsChecked == true)
            {
                EnableFieldsInstall(false);
                EnableFieldsNoInstall(false);
            }
            else if (radioButtonInstall.IsChecked == true)
            {
                EnableFieldsInstall(true);
                EnableFieldsNoInstall(false);
            }
            else if (radioButtonNoInstall.IsChecked == true)
            {
                EnableFieldsInstall(false);
                EnableFieldsNoInstall(true);
            }
        }

        private string GetUnusedFilename()
        {
            string path = ConfigurationManager.Instance.GetSaveFilePath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            for (int i = 1; i < 100; i++)
            {
                string filename = System.IO.Path.Combine(new string[] { path, "id_rsa" + i });
                if (!File.Exists(filename) && !File.Exists(filename + ".pub"))
                    return filename;
            }

            return null;
        }

        private bool CreateKeys()
        {
            //ssh-keygen -b 2048 -t rsa -f /path/sshkey -q -N ""

            string filename = GetUnusedFilename();
            if (filename == null)
            {
                MessageBox.Show("Could not find an unused filename for the key", "No unnused filename", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            string filenamePub = filename + ".pub";

            try
            {
                // Start the child process. 
                Process p = new Process();
                // Redirect the output stream of the child process. 
                p.StartInfo.UseShellExecute = false;
                //p.StartInfo.EnvironmentVariables.Add("CYGWIN", "nodosfilewarning");
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = "ssh-keygen";
                p.StartInfo.Arguments = "-b 2048 -t rsa -f \"" + filename + "\" -q -N \"\"";
                p.Start();

                //string output = p.StandardOutput.ReadToEnd();
                //output += p.StandardError.ReadToEnd();
                //output = output.Replace("\n", "\r\n");
                p.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!File.Exists(filename))
            {
                MessageBox.Show("Generation of keys failed (private key does not exist)", "Generation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!File.Exists(filenamePub))
            {
                MessageBox.Show("Generation of keys failed (public key does not exist)", "Generation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            alreadyGeneratedKeyPath = filename;

            return true;
        }

        public bool CreateOrValidateKeys()
        {
            if (radioButtonCreateInstall.IsChecked == true)
            {
                if (alreadyGeneratedKeyPath != null)
                {
                    if (!File.Exists(alreadyGeneratedKeyPath) || !File.Exists(alreadyGeneratedKeyPath + ".pub"))
                    {
                        MessageBox.Show("Generated key seems to be gone", "Key gone", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                else if (!CreateKeys())
                {
                    return false;
                }
            }
            else if (radioButtonInstall.IsChecked == true)
            {
                if (textBoxPublicKey.Text == "")
                {
                    MessageBox.Show("You must enter the path to your public key", "Public key missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }

                if (textBoxPrivateKey.Text == "")
                {
                    MessageBox.Show("You must enter the path to your private key", "Private key missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }

                if (!File.Exists(textBoxPublicKey.Text))
                {
                    MessageBox.Show("The public key could not be found at the specified path", "Public key missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }

                if (!File.Exists(textBoxPrivateKey.Text))
                {
                    MessageBox.Show("The private key could not be found at the specified path", "Private key missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }
            }
            else if (radioButtonNoInstall.IsChecked == true)
            {
                if (textBoxPrivateKeyNoInstall.Text == "")
                {
                    MessageBox.Show("You must enter the path to your private key", "Private key missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }

                if (!File.Exists(textBoxPrivateKeyNoInstall.Text))
                {
                    MessageBox.Show("The private key could not be found at the specified path", "Private key missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Internal error");
                return false;
            }

            return true;
        }

        public KeyOption KeyOption
        {
            get
            {
                if (radioButtonCreateInstall.IsChecked == true)
                {
                    return KeyOption.GenerateKeys;
                }
                else if (radioButtonInstall.IsChecked == true)
                {
                    return KeyOption.InstallKeys;
                }
                else
                {
                    return KeyOption.NoInstall;
                }
            }
        }

        public string PublicKey
        {
            get
            {
                if (radioButtonCreateInstall.IsChecked == true)
                {
                    return alreadyGeneratedKeyPath + ".pub";
                }
                else if (radioButtonInstall.IsChecked == true)
                {
                    return textBoxPublicKey.Text;
                }

                return null;
            }
        }

        public string PrivateKey
        {
            get
            {
                if (radioButtonCreateInstall.IsChecked == true)
                {
                    return alreadyGeneratedKeyPath;
                }
                else if (radioButtonInstall.IsChecked == true)
                {
                    return textBoxPrivateKey.Text;
                }
                else if (radioButtonNoInstall.IsChecked == true)
                {
                    return textBoxPrivateKeyNoInstall.Text;
                }

                return null;
            }
        }

        private void buttonBrowsePublicKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            Nullable<bool> result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                textBoxPublicKey.Text = openFileDialog.FileName;
            }
        }

        private void buttonBrowsePrivateKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            Nullable<bool> result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                textBoxPrivateKey.Text = openFileDialog.FileName;
            }
        }

        private void buttonBrowseKeyNoInstall_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = ConfigurationManager.Instance.GetSaveFilePath();
            Nullable<bool> result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                textBoxPrivateKeyNoInstall.Text = openFileDialog.FileName;
            }
        }
    }
}
