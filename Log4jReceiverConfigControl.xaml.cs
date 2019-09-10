using System.Windows.Controls;
using System.Windows.Input;

namespace Prosa.Log4View.Log4jReceiver
{
    /// <summary>
    /// Interaction logic for SampleReceiverConfigView.xaml
    /// </summary>
    public partial class Log4jReceiverConfigControl : UserControl
    {
        public Log4jReceiverConfigControl()
        {
            InitializeComponent();
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int result;

            if (!int.TryParse(e.Text, out result))
            {
                e.Handled = true;
            }
        }
    }
}
