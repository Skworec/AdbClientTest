using AdbClientTest.View;
using AdbClientTest.ViewModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AdbClientTest
{
    public class MainViewModel : BaseViewModel
    {
        public static readonly DependencyProperty AdbExePathProperty = DependencyProperty.Register(
            "AdbExePath",
            typeof(string),
            typeof(MainWindow), new PropertyMetadata(""));

        public static readonly DependencyProperty DeviceInfoProperty = DependencyProperty.Register(
            "DeviceInfo",
            typeof(ADBDeviceInfo),
            typeof(MainWindow));

        public static readonly DependencyProperty BorwserRequestProperty = DependencyProperty.Register(
            "BorwserRequest",
            typeof(string),
            typeof(MainWindow), new PropertyMetadata(""));

        public string AdbExePath
        {
            get { return (string)GetValue(AdbExePathProperty); }
            set 
            { 
                SetValue(AdbExePathProperty, value);
                OnPropertyChanged();
                AdbExePathChanged();
            }
        }
        public ICommand ButtonRunTest1Command { get; set; }
        public ICommand ButtonRunTest2Command { get; set; }
        public ICommand ButtonChooseADBPathCommand { get; set; }
        public ICommand ButtonConnectByIPCommand { get; set; }

        public TestViewModel TestViewModel1 { get; set; }
        public TestViewModel TestViewModel2 { get; set; }


        private ADBClient _adbClient;
        public ADBDeviceInfo DeviceInfo
        {
            get { return (ADBDeviceInfo)GetValue(DeviceInfoProperty); }
            set { SetValue(DeviceInfoProperty, value); OnPropertyChanged("DeviceInfo.Name"); }
        }
        public string BrowserRequest
        {
            get { return (string)GetValue(BorwserRequestProperty); }
            set { SetValue(BorwserRequestProperty, value); OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            DeviceInfo = ADBDeviceInfo.Empty;
            TestViewModel1 = new TestViewModel("Test 1", RunTest1);
            TestViewModel2 = new TestViewModel("Test 2", RunTest2);
            ButtonRunTest1Command = new RelayCommandAsync(async (o) => await TestViewModel1.StartAsync());
            ButtonRunTest2Command = new RelayCommandAsync(async (o) => await TestViewModel2.StartAsync());
            ButtonChooseADBPathCommand = new RelayCommand( (o) => ChooseADBPath());
            ButtonConnectByIPCommand = new RelayCommand((o) => OpenConnectByIpWindow());

        }

        private async Task<object> RunTest1()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            _adbClient.HomeBtn();

            await _adbClient.CloseRecentAppsAsync();

            _adbClient.MenuBtn();

            var image = await _adbClient.TakeScrennshotAsync();
            watch.Stop();
            var result = new List<object>();
            result.Add(image);
            result.Add($"Execution time: {watch.ElapsedMilliseconds} ms");
            return result;
        }

        private async Task<object> RunTest2()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            _adbClient.HomeBtn();
            _adbClient.HomeBtn();
            var result = new List<object>();

            if (!await _adbClient.TryOpenAppOnScreenAsync("Chrome"))
            {
                throw new ItemNotFoundException("App not found on screen");
            }

            var doc = await _adbClient.DumpScreenXMLAsync();

            if (!ADBClient.TryGetCoordsOfScreenElementByAttr(doc, "resource-id", "com.android.chrome:id/search_box_text", out IntVector2 searchBarCoords))
            {
                if (ADBClient.TryGetCoordsOfScreenElementByAttr(doc, "resource-id", "com.android.chrome:id/new_tab_view", out IntVector2 newTabBtnCoords) |
                    ADBClient.TryGetCoordsOfScreenElementByAttr(doc, "resource-id", "com.android.chrome:id/optional_toolbar_button", out newTabBtnCoords))
                {
                    _adbClient.Touch(newTabBtnCoords);
                    await Task.Delay(500);
                    doc = await _adbClient.DumpScreenXMLAsync();
                    searchBarCoords = ADBClient.GetCoordsOfScreenElementByAttr(doc, "resource-id", "com.android.chrome:id/search_box_text");
                }
            }

            Trace.WriteLine(searchBarCoords.ToString());
            _adbClient.Touch(searchBarCoords);
            _adbClient.WriteLine(BrowserRequest);
            await Task.Delay(3500); //Wait browser load result
            if (BrowserRequest.ToLower() == "my ip address")
            {
                doc = await _adbClient.DumpScreenXMLAsync();
                var ipRegex = new Regex("\\b(?:\\d{1,3}\\.){3}\\d{1,3}\\b");
                var ipNode = ADBClient.FindElementOnScreenXMLByAttribute(doc, "text", "Your public IP address").
                    Parent.Elements().
                    Where((x) =>
                    {
                        return ipRegex.IsMatch(x.Attribute("text").Value);
                    }).FirstOrDefault();
                if (ipNode != null)
                {
                    var ip = ipNode.Attribute("text").Value;
                    result.AddRange(new List<object> { "Device ip Address", ip });
                }
                else
                {
                    result.Add("Browser did not show IP");
                }
            }
            else
            {
                result.Add(await _adbClient.TakeScrennshotAsync());
            }
            watch.Stop();
            result.Add($"Execution time: {watch.ElapsedMilliseconds} ms");
            return result;
        }

        private void OpenConnectByIpWindow()
        {
            ConnectByIpWindow dialog = new ConnectByIpWindow(this);
            dialog.ShowDialog();
        }

        public async Task ConnectToDeviceByIp(string ip)
        {
            _adbClient.Connect(ip);
            var device = (await _adbClient.GetDeviceListAsync()).FirstOrDefault();
            if (device != null)
            {
                DeviceInfo.Name = device.Name;
                DeviceInfo.Serial = device.Serial;
            }
            else
            {
                MessageBox.Show("Could not find device");
            }
        }

        private async Task AdbExePathChanged()
        {
            this.OnPropertyChanged(nameof(AdbExePath));
            _adbClient = new ADBClient(AdbExePath);
            var device = (await _adbClient.GetDeviceListAsync()).FirstOrDefault();
            if (device != null)
            {
                DeviceInfo.Name = device.Name;
                DeviceInfo.Serial = device.Serial;
            }
        }

        private void ChooseADBPath()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "exe files (*.exe)|*.exe";
            dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            dialog.Title = "Please select an ADB executable file ";
            if (dialog.ShowDialog() == true)
            {
                AdbExePath = dialog.FileName;
            }
        }
    }
}
