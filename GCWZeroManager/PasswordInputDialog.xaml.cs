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

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for PasswordInputDialog.xaml
    /// </summary>
    public partial class PasswordInputDialog : Window
    {
        private string passphrase;

        public PasswordInputDialog()
        {
            InitializeComponent();
        }

        public string Passphrase
        {
            get { return passphrase; }
            set { passphrase = value; }
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            passphrase = passwordBox.Password;
            this.DialogResult = true;
        }
    }
}
