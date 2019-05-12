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
    /// Interaction logic for ReplacePromptDialog.xaml
    /// </summary>
    public partial class ReplacePromptDialog : Window
    {
        public enum SelectedReplaceOption {Replace, ReplaceAll, Skip, SkipAll, Cancel}

        public SelectedReplaceOption SelectedOption { get; private set; } = SelectedReplaceOption.Cancel;

        string infoText = "";

        public ReplacePromptDialog()
        {
            InitializeComponent();
            infoText = textBlockInfo.Text;
        }

        public void SetFilename(string fileName)
        {
            textBlockInfo.Text = string.Format(infoText, fileName);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedOption = SelectedReplaceOption.Cancel;
            DialogResult = false;
        }

        private void buttonReplace_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxApplyToAll.IsChecked ?? false)
                SelectedOption = SelectedReplaceOption.ReplaceAll;
            else
                SelectedOption = SelectedReplaceOption.Replace;

            DialogResult = true;
        }

        private void buttonSkip_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxApplyToAll.IsChecked ?? false)
                SelectedOption = SelectedReplaceOption.SkipAll;
            else
                SelectedOption = SelectedReplaceOption.Skip;

            DialogResult = true;
        }
    }
}
