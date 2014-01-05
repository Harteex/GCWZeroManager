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
    /// Interaction logic for UserControlSetupKeysConnect.xaml
    /// </summary>
    public partial class UserControlSetupKeysConnect : UserControl
    {
        public UserControlSetupKeysConnect()
        {
            InitializeComponent();
        }

        public void SetInstallKey(bool b)
        {
            textBlockInstallKey.Visibility = b ? Visibility.Visible : Visibility.Hidden;
            textBoxPassword.Visibility = b ? Visibility.Visible : Visibility.Hidden;
            labelPassword.Visibility = b ? Visibility.Visible : Visibility.Hidden;
        }

        public bool ValidateFields()
        {
            if (textBoxHost.Text == "")
            {
                MessageBox.Show("Please enter a host", "No host entered", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            if (textBoxPassword.IsVisible == true && textBoxPassword.Text == "")
            {
                MessageBox.Show("Please enter a password", "No password entered", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            return true;
        }

        public string Host
        {
            get { return textBoxHost.Text; }
            set { textBoxHost.Text = value; }
        }

        public string Password
        {
            get { return textBoxPassword.Text; }
        }
    }
}
