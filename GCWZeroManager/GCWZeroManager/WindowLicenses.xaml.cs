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

namespace GCWZeroManager2
{
    /// <summary>
    /// Interaction logic for WindowLicenses.xaml
    /// </summary>
    public partial class WindowLicenses : Window
    {
        private string[] licenseText = { "Copyright (c) 2013 Andreas Bjerkeholt" +
                                           "\n\n" + 
                                           "Permission is hereby granted, free of charge, to any person obtaining a copy" +
                                           " of this software and associated documentation files (the \"Software\"), to deal" +
                                           " in the Software without restriction, including without limitation the rights" +
                                           " to use, copy, modify, merge, publish, distribute, sublicense, and/or sell" +
                                           " copies of the Software, and to permit persons to whom the Software is" +
                                           " furnished to do so, subject to the following conditions:" +
                                           "\n\n" +
                                           "The above copyright notice and this permission notice shall be included in" +
                                           " all copies or substantial portions of the Software." +
                                           "\n\n" +
                                           "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR" +
                                           " IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY," +
                                           " FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE" +
                                           " AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER" +
                                           " LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM," +
                                           " OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN" +
                                           " THE SOFTWARE.",
                                       "License: Creative Commons Attribution-NoDerivs 3.0\n" +
                                           "Attribution: Icon Design by Creative Freedom (http://www.creativefreedom.co.uk/icon-design/)\n" +
                                           "All copyright for Shimmer Icons belongs to Creative Freedom Ltd.\n" +
                                           "\n" +
                                           "http://creativecommons.org/licenses/by-nd/3.0/",
                                       "License: Creative Commons Attribution 3.0\n" +
                                           "Attribution: Ray Cheung - WebAppers.com\n" +
                                           "\n" +
                                           "You are free:\n" +
                                           "- to use the icons for your personal or commercial projects in any way you like \n" +
                                           "- to share it with others (but please keep the Terms Of Use.txt file intact together with the icons.) \n" +
                                           "\n" +
                                           "You cannot:\n" +
                                           "- claim ownership of the icons (copyrights belong exclusively to Ray Cheung - WebAppers.com)\n" +
                                           "- sell them to third parties\n" +
                                           "- bundle them and offer them toghether with your products as a free gift\n" +
                                           "\n" +
                                           "http://creativecommons.org/licenses/by/3.0/"
                                       };
        public WindowLicenses()
        {
            InitializeComponent();
            textBox1.Text = licenseText[comboBox1.SelectedIndex];
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textBox1 != null)
                textBox1.Text = licenseText[comboBox1.SelectedIndex];
        }
    }
}
