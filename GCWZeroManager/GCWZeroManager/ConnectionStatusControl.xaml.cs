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

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for ConnectionStatusControl.xaml
    /// </summary>
    public partial class ConnectionStatusControl : UserControl
    {
        public ConnectionStatusControl()
        {
            InitializeComponent();

            ChangeImage(ConnectionState.DisconnectedNormal);
            ConnectionManager.Instance.ConnectionStateChanged += ChangeImageEventHandler;
        }

        public void ChangeImageEventHandler(object sender, ConnectionStateEventArgs e)
        {
            ChangeImage(e.ConnectionState);
        }

        public void ChangeImage(ConnectionState state)
        {
            string imageName = "";
            switch (state)
            {
                case ConnectionState.Connected:
                    imageName = "connection-online16.png";
                    break;
                case ConnectionState.DisconnectedNormal:
                    imageName = "connection-offline16.png";
                    break;
                case ConnectionState.DisconnectedError:
                    imageName = "connection-error16.png";
                    break;
                default:
                    return;
            }

            image1.Source = new BitmapImage(new Uri(String.Format("pack://application:,,,/Resources/ConnectionIcons/{0}", imageName)));
        }
    }
}
