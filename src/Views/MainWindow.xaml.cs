using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using WYW.RS232SOCKET.ViewModels;

namespace WYW.RS232SOCKET.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "RS232 & Socket调试助手 V1.0";
            if(SystemParameters.PrimaryScreenHeight<this.Height)
            {
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = this.Height * 4 / 3;
                this.Top = 0;
                this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
            }
            
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Environment.Exit(Environment.ExitCode);
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

    }
}
