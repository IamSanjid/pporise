using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PPORise.Views
{
    /// <summary>
    /// Interaction logic for ChatPanel.xaml
    /// </summary>
    public partial class ChatPanel : UserControl
    {
        public ChatPanel()
        {
            InitializeComponent();
        }

        private void ChatBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var richTextBox = (RichTextBox) sender;
            if (!richTextBox.Selection.IsEmpty) return;
            richTextBox.ScrollToEnd();
            richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
        }
    }
}
