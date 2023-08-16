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
            this.Loaded += OnLoaded;
            DataContext = new MainViewModel();
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this.MinHeight = this.MaxHeight = this.Height;
            this.MinWidth = this.MaxWidth = this.Width;
        }
    }
}
