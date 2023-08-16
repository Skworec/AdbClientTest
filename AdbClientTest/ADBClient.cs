using AdbClientTest.ViewModel;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace AdbClientTest
{
    public class ADBClient
    {
        private readonly string _adbPath;
        private readonly string _dumpFolderPath;

        public ADBClient(string adbExecPath)
        {
            if (!File.Exists(adbExecPath))
                throw new ArgumentException("adb.exe not found");
            _adbPath = adbExecPath;
            _dumpFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"\ScreenDumps";
        }

        public void StartServer()
        {
            ExecCommand("start-server");
        }
        public void StopServer()
        {
            ExecCommand("kill-server");
        }
        public void RestartServer()
        {
            StopServer();
            StartServer();
        }

        public async Task<IEnumerable<ADBDeviceInfo>> GetDeviceListAsync()
        {
            return await ExecCommandAsync<IEnumerable<ADBDeviceInfo>>("devices -l", (response) =>
            {
                List<ADBDeviceInfo> devices = new List<ADBDeviceInfo>();
                for (int i = 1; i < response.Count - 1; i++)
                {
                    var r = response[i].ToString();
                    var fields = r.Split(' ').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var serial = fields[0];
                    var model = fields[3].Split(':')[1];
                    devices.Add(new ADBDeviceInfo(model, serial));
                }
                return devices;
            });
        }

        public void Connect(string ip)
        {
            ExecCommandAsync($"connect {ip}");
        }

        public async Task<XDocument> DumpScreenXMLAsync()
        {
            if (!Directory.Exists(_dumpFolderPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(_dumpFolderPath);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }

            var screenSavedTo = await ExecCommandAsync<string>("shell uiautomator dump", (response) =>
            {
                var str = response.FirstOrDefault(x => x.ToString().Contains("dumped to")).ToString();
                str = str.Substring(str.IndexOf('/'));
                return str;
            });

            await ExecCommandAsync($"pull {screenSavedTo} {_dumpFolderPath}");

            string filePath = string.Concat(_dumpFolderPath, screenSavedTo.Substring(screenSavedTo.LastIndexOf('/')));

            XDocument result;
            using (var stream = File.OpenText(filePath))
            {
                result = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            }
            File.Delete(filePath);

            return result;
        }
        public async Task<BitmapImage> TakeScrennshotAsync()
        {
            await ExecCommandAsync("shell screencap -p /sdcard/screencap.png");
            var screenCapPath = $@"{_dumpFolderPath}\screencap.png";
            if (!Directory.Exists(_dumpFolderPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(_dumpFolderPath);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            return await ExecCommandAsync<BitmapImage>($@"pull /sdcard/screencap.png {screenCapPath}", (x) =>
            {
                if (File.Exists(screenCapPath))
                {
                    BitmapImage image = new BitmapImage();
                    using (var fs = File.OpenRead(screenCapPath))
                    {
                        var ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Position = 0;
                        var imageSource = new BitmapImage();
                        imageSource.BeginInit();
                        imageSource.StreamSource = ms;
                        imageSource.EndInit();
                        image = imageSource;

                    }
                    File.Delete(screenCapPath);
                    return image;
                }
                else
                {
                    throw new Exception("Couldn't take a screenshot");
                }
            });
        }

        public void Touch(int x, int y)
        {
            if (x < 0)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0)
                throw new ArgumentOutOfRangeException(nameof(y));
            ExecCommand($"shell input tap {x} {y}");
        }
        public void Touch(IntVector2 coords)
        {
            Touch(coords.x, coords.y);
        }

        public void Write(string text)
        {
            ExecCommand($"shell input text \"{text.Replace(" ", "\\ ")}\"");
        }
        public void WriteLine(string text)
        {
            ExecCommand($"shell input text \"{text.Replace(" ", "\\ ")}\" && {_adbPath} shell input keyevent KEYCODE_ENTER");
        }

        public void KeyEvent(string keyCode)
        {
            ExecCommand($"shell input keyevent {keyCode}");
        }

        public void HomeBtn()
        {
            KeyEvent("KEYCODE_HOME");
        }
        public void BackBtn()
        {
            KeyEvent("KEYCODE_BACK");
        }
        public void MenuBtn()
        {
            KeyEvent("KEYCODE_MENU");
        }

        public async Task CloseRecentAppsAsync()
        {
            var regexp = new Regex("id=(\\d+) stackId=\\d+ userId=\\d+ hasTask");

            var ids = await ExecCommandAsync<IEnumerable<int>>("shell dumpsys activity recents", (response) =>
            {
                return response.Where((r) => regexp.IsMatch(r.ToString()))
                               .Select((x) => int.Parse(regexp.Match(x.ToString()).Groups[1].Value));
            });

            foreach (var id in ids)
            {
                await ExecCommandAsync($"shell am stack remove {id}");
            }
        }
        public async Task<bool> TryOpenAppOnScreenAsync(string appName)
        {
            var doc = await DumpScreenXMLAsync();
            try
            {
                var appIconCoords = GetCoordsOfScreenElementByAttr(doc, "content-desc", appName);
                Touch(appIconCoords);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public T ExecCommand<T>(string command, Func<IList<PSObject>, T> func) where T : class
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            Collection<PSObject> response;

            using (var pShell = PowerShell.Create())
            {
                pShell.AddScript($"& '{_adbPath}' {command}");
                response = pShell.Invoke();
            }
            return func(response);
        }
        public void ExecCommand(string command)
        {
            ExecCommand<object>(command, (collection) => { return 0; });
        }
        public async Task<T> ExecCommandAsync<T>(string command, Func<IList<PSObject>, T> func) where T : class
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            PSDataCollection<PSObject> response;
            using (var pShell = PowerShell.Create())
            {
                pShell.AddScript($"& '{_adbPath}' {command}");
                response = await pShell.InvokeAsync();
            }
            return func(response);
        }
        public async Task ExecCommandAsync(string command)
        {
            await ExecCommandAsync<object>(command, (collection) => { return 0; });
        }

        public static XElement FindElementOnScreenXMLByAttribute(XDocument doc, string attrName, Regex attrRegex)
        {
            return (from nodes in doc.Descendants("node")
                    where attrRegex.IsMatch(nodes.Attribute(attrName).Value)
                    select nodes).FirstOrDefault();
        }
        public static XElement FindElementOnScreenXMLByAttribute(XDocument doc, string attrName, string attrValue)
        {
            return (from nodes in doc.Descendants("node")
                    where nodes.Attribute(attrName).Value == attrValue
                    select nodes).FirstOrDefault();
        }

        public static IntVector2 ParseCenterOfBounds(string bounds)
        {
            string pattern = @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(bounds);

            if (match.Success)
            {
                int x1 = int.Parse(match.Groups[1].Value);
                int y1 = int.Parse(match.Groups[2].Value);
                int x2 = int.Parse(match.Groups[3].Value);
                int y2 = int.Parse(match.Groups[4].Value);
                return new IntVector2((int)(x1 + (x2 - x1) / 2), (int)(y1 + (y2 - y1) / 2));
            }
            else
            {
                throw new ArgumentException("Invalid input format.");
            }
        }
        public static IntVector2 GetCoordsOfScreenElementByAttr(XDocument doc, string attrName, string attrValue)
        {
            var element = FindElementOnScreenXMLByAttribute(doc, attrName, attrValue);
            if (element == null)
            {
                throw new PropertyNotFoundException();
            }
            return ParseCenterOfBounds(element.Attribute("bounds").Value);
        }
        public static bool TryGetCoordsOfScreenElementByAttr(XDocument doc, string attrName, string attrValue, out IntVector2 coords)
        {
            try
            {
                coords = GetCoordsOfScreenElementByAttr(doc, attrName, attrValue);
                return true;
            }
            catch
            {
                coords = default(IntVector2);
                return false;
            }
        }
    }
    public class ADBDeviceInfo : BaseViewModel
    {
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
            "Name",
            typeof(string),
            typeof(ADBDeviceInfo), new PropertyMetadata(""));

        public static readonly DependencyProperty SerialProperty = DependencyProperty.Register(
            "Serial",
            typeof(string),
            typeof(ADBDeviceInfo), new PropertyMetadata(""));

        public string Name
        {
            get
            {
                return (string)GetValue(NameProperty);
            }
            set
            {
                SetValue(NameProperty, value);
                OnPropertyChanged();
            }
        }
        public string Serial
        {
            get
            {
                return (string)GetValue(SerialProperty);
            }
            set
            {
                SetValue(SerialProperty, value);
            }
        }

        public static ADBDeviceInfo Empty;
        static ADBDeviceInfo()
        {
            Empty = new ADBDeviceInfo("", "");
        }
        public ADBDeviceInfo(string name, string serial)
        {
            Name = name;
            Serial = serial;
        }
    }

    public struct IntVector2
    {
        public int x;
        public int y;
        public IntVector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public override string ToString()
        {
            return $"x: {x}, y: {y}";
        }
    }
}
