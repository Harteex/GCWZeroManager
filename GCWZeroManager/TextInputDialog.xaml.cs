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

        public TextInputDialog(string title, string description, string fieldLabel)
        {
            InitializeComponent();

            this.Title = title;
            labelDescription.Content = description;
            labelField.Content = fieldLabel;
        }

        public string InputText
        {
            get { return inputText; }
            set { inputText = value; }
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            inputText = textBox.Text;
            this.DialogResult = true;
        }
    }
}
