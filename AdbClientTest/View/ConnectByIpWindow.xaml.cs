using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace AdbClientTest.View
{
    public partial class ConnectByIpWindow : Window
    {
        private MainViewModel _model;
        public ConnectByIpWindow(MainViewModel model)
        {
            InitializeComponent();
            _model = model;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this.MinHeight = this.MaxHeight = this.Height;
            this.MinWidth = this.MaxWidth = this.Width;
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            Regex regexp = new Regex("\\b(?:\\d{1,3}\\.){3}\\d{1,3}\\b");
            if (!regexp.IsMatch(ipTextBox.Text))
            {
                MessageBox.Show("Ip not valid");
            }
            _model.ConnectToDeviceByIp(ipTextBox.Text);
            this.Close();
        }
    }
}
