using System.Threading.Tasks;
using System.Windows;

namespace Await_and_Synchronization_Context_Windows_Forms_
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("First Message", 3000);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Second Message", 3000);
        }

        private async Task ShowMessage(string message, int delay)
        {
            await Task.Delay(delay);

            lblMessage.Text = message;
        }

    }
}
