using System.Threading;
using System.Windows;

namespace OffloadTask
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(() => ShowMessage("Hello, World!", 3000));
            thread.Start();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(() => ShowMessage("Message 2", 5000));
            thread.Start();
        }


        private void ShowMessage(string message, int delay)
        {
            Thread.Sleep(delay);
            lblMessage.Content = message;
        }
    }
}
