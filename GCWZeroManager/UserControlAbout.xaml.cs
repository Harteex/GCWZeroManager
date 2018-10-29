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
using System.Reflection;
using System.Diagnostics;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for UserControlAbout.xaml
    /// </summary>
    public partial class UserControlAbout : UserControl
    {
        public UserControlAbout()
        {
            InitializeComponent();
            labelVersion.Content += " " + GetFileVersion();
        }

        public string GetFileVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            return version;
        }

        private void buttonLicense_Click(object sender, RoutedEventArgs e)
        {
            WindowLicenses licenseWindow = new WindowLicenses();
            licenseWindow.ShowDialog();
        }

        private void OpenURL(string url)
        {
            Process.Start(url);
        }

        private void buttonOpenHomepage_Click(object sender, RoutedEventArgs e)
        {
            OpenURL("http://www.gcw-zero.com/");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
