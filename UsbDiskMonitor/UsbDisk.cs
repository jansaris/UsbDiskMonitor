using System;
using System.IO;
using System.Management;
using log4net;

namespace UsbDiskMonitor
{
    class UsbDisk : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UsbDisk));

        private readonly string _pathToValidate;
        private readonly string _drive;
        private bool _lastConnectedState;
        private ManagementEventWatcher _watcher;
        public bool IsConnected => File.Exists(_pathToValidate) || Directory.Exists(_pathToValidate);

        public event EventHandler<ConnectionEventArgs> ConnectionChanged;

        public UsbDisk(string pathToValidate)
        {
            if(string.IsNullOrWhiteSpace(pathToValidate)) throw new ArgumentNullException(nameof(pathToValidate));
            if(pathToValidate.Length < 2) throw new ArgumentException("Path should at least start with a drive letter");
            if(pathToValidate[1] != ':') throw new ArgumentException("Path should at least start with a drive letter");

            _pathToValidate = pathToValidate;
            _drive = _pathToValidate.Substring(0, 2);

            _lastConnectedState = IsConnected;

            StartMonitor();
        }

        private void StartMonitor()
        {
            _watcher = new ManagementEventWatcher();
            var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent");
            _watcher.EventArrived += EventArrived;
            _watcher.Query = query;
            _watcher.Start();
        }

        private void EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (_lastConnectedState == IsConnected) return;

            _lastConnectedState = IsConnected;
            Logger.Info($"Drive '{_drive}' is now available: {_lastConnectedState}");
            ConnectionChanged?.BeginInvoke(null, new ConnectionEventArgs(_lastConnectedState), ar => { }, null);
        }

        public void Disconnect()
        {
            Logger.Info($"Disconnect '{_drive}'");
            if (!IsConnected)
            {
                Logger.Warn($"Drive '{_drive}' is already disconnected!");
                return;
            }
            var result = NativeUsbFunctions.EjectDrive(_drive);
            if(result) Logger.Info($"Succesfully ejected '{_drive}'");
        }

        public void Dispose()
        {
            try
            {
                _watcher?.Stop();
                _watcher?.Dispose();

            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to stop the system watcher: {ex.Message}");
            }
        }
    }
}
