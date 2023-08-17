using System;
using System.Windows;

namespace AdbClientTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ContentRendered += OnContentRendered;
            DataContext = new MainViewModel();
        }

        private void OnContentRendered(object? sender, EventArgs e)
        {
            this.MinHeight = this.MaxHeight = this.Height;
            this.MinWidth = this.MaxWidth = this.Width;
        }
    }
}
