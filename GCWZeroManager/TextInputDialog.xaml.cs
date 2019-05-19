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
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        private string inputText;

        public TextInputDialog(string title, string description, string fieldLabel, string originalText = "")
        {
            InitializeComponent();

            this.Title = title;
            labelDescription.Content = description;
            labelField.Content = fieldLabel;
            textBox.Text = originalText;
        }

        public void SetTextBoxSelection(int start, int length)
        {
            textBox.Select(start, length);
        }

        public string InputText
        {
            get { return inputText; }
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            inputText = textBox.Text;
            this.DialogResult = true;
        }
    }
}
