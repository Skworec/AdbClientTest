using System;

namespace AdbClientTest
{
    public interface IDialogService
    {
        void ShowDialog(object content);
        void ShowDialog(object content, Action<bool> callback);
    }

    public class DialogService : IDialogService
    {
        public void ShowDialog(object content) 
        {
            var dialog = new TestResultWindow(content);
            dialog.ShowDialog();
        }
        public void ShowDialog(object content, Action<bool> callback)
        {
            var dialog = new TestResultWindow(content);
            EventHandler closeEventHandler = null;
            closeEventHandler = (s, e) =>
            {
                bool result = false;
                if (dialog.DialogResult != null) {
                    result = (bool)dialog.DialogResult;
                }
                callback(result);
                dialog.Closed -= closeEventHandler;
            };
            dialog.Closed += closeEventHandler;
            dialog.ShowDialog();
        }
    }
}
