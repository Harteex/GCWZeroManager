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
using System.Windows.Resources;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for WindowLicenses.xaml
    /// </summary>
    public partial class WindowLicenses : Window
    {
        private string[] licenseText = new string[2];
        public WindowLicenses()
        {
            InitializeComponent();

            licenseText[0] = GetLicenseText("License.txt");
            licenseText[1] = GetLicenseText("License-Oxygen-Icons.txt");

            textBox1.Text = licenseText[comboBox1.SelectedIndex];
        }

        private string GetLicenseText(string file)
        {
            Uri uri = new Uri(String.Format("pack://application:,,,/{0}", file));
            StreamResourceInfo stream = System.Windows.Application.GetResourceStream(uri);
            StreamReader sr = new StreamReader(stream.Stream);
            var content = sr.ReadToEnd();

            return content;
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textBox1 != null)
                textBox1.Text = licenseText[comboBox1.SelectedIndex];
        }
    }
}
