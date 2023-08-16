using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AdbClientTest.ViewModel;

namespace AdbClientTest
{
    /// <summary>
    /// Логика взаимодействия для TestResultWindow.xaml
    /// </summary>
    public partial class TestResultWindow : Window
    {
        public TestResultWindow(object content)
        {
            InitializeComponent();

            DataContext = new TestResultViewModel();

            this.Loaded += OnLoaded;
            IEnumerable<object> contents = new List<object>();
            if (!(content is IEnumerable<object>))
            {
                (contents as List<object>).Add(content);
            }
            else
            {
                contents = content as IEnumerable<object>;
            }
            foreach (object item in contents)
            {
                FrameworkElement control = new FrameworkElement();
                if (item is string | item is int | item is double)
                {
                    var label = new Label();
                    label.Content = item;
                    control = label;
                }
                else if (item is BitmapImage)
                {
                    var image = new Image();
                    image.Source = item as BitmapImage;
                    image.Stretch = System.Windows.Media.Stretch.Fill;
                    image.MaxHeight = 400;
                    control = image;
                }
                else
                {
                    throw new NotImplementedException();
                }
                control.HorizontalAlignment = HorizontalAlignment.Center;
                control.Margin = new Thickness(3);
                ContentContainer.Children.Add(control);
            }
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this.MinHeight = this.MaxHeight = this.Height;
            this.MinWidth = this.MaxWidth = this.Width;
        }
    }
}
