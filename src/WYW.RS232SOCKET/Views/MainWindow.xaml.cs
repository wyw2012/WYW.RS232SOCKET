using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Shapes;
using WYW.RS232SOCKET.ViewModels;

namespace WYW.RS232SOCKET.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel=new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
            Title = "RS232 & Socket & Modbus & VISA调试助手 V2.0";
            this.DataContext = viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            viewModel.Controller?.Close();
            Environment.Exit(Environment.ExitCode);
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
