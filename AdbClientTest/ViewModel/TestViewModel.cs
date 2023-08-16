using System;
using System.Threading.Tasks;

namespace AdbClientTest.ViewModel
{
    public class TestViewModel : BaseViewModel
    {
        private TestStatus _status;
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        public TestStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        public Func<Task<object>> TestAction { get; private set; }
        public TestViewModel(string name, Func<Task<object>> testMethod)
        {
            Name = name;
            Status = TestStatus.NotStarted;
            TestAction = testMethod;
        }
        public async Task StartAsync()
        {
            IDialogService dialogService = new DialogService();
            Status = TestStatus.InProgress;
            try
            {
                var result = await (TestAction as Func<Task<object>>)();
                dialogService.ShowDialog(result, (result) =>
                {
                    Status = result ? TestStatus.Success : TestStatus.Failed;
                });
            }
            catch (Exception ex)
            {
                Status = TestStatus.Failed;
                dialogService.ShowDialog(ex.Message);
            }
        }
    }

    public enum TestStatus
    {
        NotStarted,
        InProgress,
        Success,
        Failed
    }
}
