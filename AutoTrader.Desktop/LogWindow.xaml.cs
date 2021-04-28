using System.Windows;
using System.Windows.Controls;

namespace AutoTrader.Desktop
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        private MainWindow mainWindow;

        public TextBox Console => console;

        public LogWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            mainWindow.IsLogWindowClosed = false;
            this.mainWindow = mainWindow;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            mainWindow.IsLogWindowClosed = true;
        }
    }
}
