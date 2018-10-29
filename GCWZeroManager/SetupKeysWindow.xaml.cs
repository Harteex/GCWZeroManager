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
    /// Interaction logic for SetupKeysWindow.xaml
    /// </summary>
    public partial class SetupKeysWindow : Window
    {
        private UserControlSetupKeysKeyOption userControlKeyOption = new UserControlSetupKeysKeyOption();
        private UserControlSetupKeysConnect userControlConnect = new UserControlSetupKeysConnect();
        private UserControlSetupKeysFinished userControlFinished = new UserControlSetupKeysFinished();

        private UserControl[] userControls;
        private Label[] labels;
        private int curPage = 0;

        private ConnectionNode newCn = new ConnectionNode();

        public SetupKeysWindow()
        {
            InitializeComponent();

            userControls = new UserControl[] { userControlKeyOption, userControlConnect, userControlFinished };
            labels = new Label[] { labelPage1, labelPage2, labelPage3 };

            newCn.AuthenticationMethod = AuthenticationMethod.PrivateKey;

            SetPage(0);
        }

        public ConnectionNode ConnectionNode
        {
            get { return newCn; }
        }

        public void PreSetHost(string host)
        {
            newCn.Host = host;
            userControlConnect.Host = host;
        }

        private void SetPage(int page)
        {
            curPage = page;
            contentControl1.Content = userControls[curPage];

            for (int i = 0; i < labels.Length; i++)
            {
                if (i == curPage)
                    labels[i].FontWeight = FontWeights.Bold;
                else
                    labels[i].FontWeight = FontWeights.Normal;
            }

            if (curPage == userControls.Length - 1)
                buttonNext.Content = "Finish";
            else
                buttonNext.Content = "Next >";

            if (curPage == 0)
                buttonPrevious.IsEnabled = false;
            else
                buttonPrevious.IsEnabled = true;
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            if (curPage == userControls.Length - 1)
            {
                // Last page
                DialogResult = true;
            }
            else
            {
                if (curPage == 0)
                {
                    if (!userControlKeyOption.CreateOrValidateKeys())
                    {
                        return;
                    }

                    if (userControlKeyOption.KeyOption == KeyOption.NoInstall)
                        userControlConnect.SetInstallKey(false);
                    else
                        userControlConnect.SetInstallKey(true);

                    newCn.PrivateKey = userControlKeyOption.PrivateKey;
                }
                else if (curPage == 1)
                {
                    if (!userControlConnect.ValidateFields())
                    {
                        return;
                    }

                    newCn.Host = userControlConnect.Host;

                    if (userControlKeyOption.KeyOption == KeyOption.NoInstall)
                    {
                        if (!ConnectionManager.Instance.TestConnection(newCn))
                        {
                            MessageBox.Show("Testing the connection failed, unable to connect! Please verify that the host is correct.", "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        ConnectionNode tempCn = new ConnectionNode();
                        tempCn.AuthenticationMethod = AuthenticationMethod.Password;
                        tempCn.Password = userControlConnect.Password;
                        tempCn.Host = newCn.Host;
                        string opRes;
                        if (!ConnectionManager.Instance.InstallPublicKey(tempCn, userControlKeyOption.PublicKey, out opRes))
                        {
                            MessageBox.Show("Unable to connect to install the key! Please verify that the host and password is correct. Error: " + opRes, "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                SetPage(curPage + 1);
            }
        }

        private void buttonPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (curPage > 0)
                SetPage(curPage - 1);
        }
    }
}
