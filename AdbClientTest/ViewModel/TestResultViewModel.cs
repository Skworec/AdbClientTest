using System.Windows;
using System.Windows.Input;

namespace AdbClientTest.ViewModel
{
    class TestResultViewModel : BaseViewModel
    {
        public ICommand ButtonAcceptCommand { get; set; }
        public ICommand ButtonRejectCommand { get; set; }

        public TestResultViewModel()
        {
            ButtonAcceptCommand = new RelayCommand(o => ButtonAcceptClick(o));
            ButtonRejectCommand = new RelayCommand(o => ButtonRejectClick(o));
        }

        private void ButtonAcceptClick(object sender)
        {
            var window = sender as Window;
            window.DialogResult = true;
            window.Close();
        }

        private void ButtonRejectClick(object sender)
        {
            var window = sender as Window;
            window.DialogResult = false;
            window.Close();
        }
    }
}
