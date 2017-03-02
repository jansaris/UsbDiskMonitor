using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using log4net;

namespace UsbDiskMonitor
{
    public class SysTrayForm : Form
    {
        private const string DatabasePath = @"E:\Databases";
        private const string MsSqlService = @"MsSqlServer";
        private const string RabbitMqService = @"RabbitMQ";

        private readonly ILog _logger = LogManager.GetLogger(typeof (SysTrayForm));
        private readonly NotifyIcon _trayIcon;
        private ContextMenu _menu;
        private MenuItem _autoMsSql;
        private MenuItem _autoRabbitMq;
        private MenuItem _autoDisconnectUsb;
        private MenuItem _mssqlStatus;
        private MenuItem _rabbitMqStatus;
        private MenuItem _usbStatus;

        private WindowsService _mssql;
        private WindowsService _rabbitMq;
        private UsbDisk _usb;

        private bool _autoMsSqlChecked;
        private bool _autoRabbitMqChecked;
        private bool _autoDisconnectUsbChecked;

        public SysTrayForm()
        {
            CreateMenu();
            CreateServiceMonitors();
            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            _trayIcon = new NotifyIcon
            {
                Text = @"Usb Monitor",
                Icon = new Icon(CreateIconFromRersource("USB.ico"), 40, 40),
                ContextMenu = _menu,
                Visible = true
            };

            _menu.Popup += UpdateStatusMessages;
            _usb.ConnectionChanged += OnUsbConnectionChanged;
        }

        private void OnUsbConnectionChanged(object sender, ConnectionEventArgs connectionEventArgs)
        {
            if (connectionEventArgs.IsConnected)
            {
                OnConnect(null, null);
            }
            else
            {
                OnDisconnect(null, null);
            }
        }

        private void CreateServiceMonitors()
        {
            _usb = new UsbDisk(DatabasePath);
            _mssql = new WindowsService(MsSqlService);
            _rabbitMq = new WindowsService(RabbitMqService);
        }

        private void CreateMenu()
        {
            _menu = new ContextMenu();

            _mssqlStatus = _menu.MenuItems.Add("MsSql Stopped");
            _mssqlStatus.Enabled = false;
            _rabbitMqStatus = _menu.MenuItems.Add("RabbitMQ Stopped");
            _rabbitMqStatus.Enabled = false;
            _usbStatus = _menu.MenuItems.Add("USB Disconnected");
            _usbStatus.Enabled = false;

            _menu.MenuItems.Add("-");

            _autoMsSql = _menu.MenuItems.Add("MS SQL", ToggleMsSql);
            _autoRabbitMq = _menu.MenuItems.Add("RabbitMQ", ToggleRabbitMq);
            _autoDisconnectUsb = _menu.MenuItems.Add("Disconnect USB", ToggleUsb);

            ToggleMsSql(null, null);
            ToggleRabbitMq(null,null);
            ToggleUsb(null,null);

            _menu.MenuItems.Add("-");

            _menu.MenuItems.Add("Force connect", OnConnect);
            _menu.MenuItems.Add("Disconnect", OnDisconnect);

            _menu.MenuItems.Add("-");
            _menu.MenuItems.Add("Exit", OnExit);
        }

        private void ToggleUsb(object sender, EventArgs e)
        {
            _autoDisconnectUsb.Checked = !_autoDisconnectUsb.Checked;
            _autoDisconnectUsbChecked = _autoDisconnectUsb.Checked;
        }

        private void ToggleRabbitMq(object sender, EventArgs e)
        {
            _autoRabbitMq.Checked = !_autoRabbitMq.Checked;
            _autoRabbitMqChecked = _autoRabbitMq.Checked;
        }

        private void ToggleMsSql(object sender, EventArgs e)
        {
            _autoMsSql.Checked = !_autoMsSql.Checked;
            _autoMsSqlChecked = _autoMsSql.Checked;
        }

        private void UpdateStatusMessages(object sender, EventArgs e)
        {
            _mssqlStatus.Text = $"MsSql: {_mssql.Status()}";
            _rabbitMqStatus.Text = $"RabbitMQ: {_rabbitMq.Status()}";
            var usbStatus = _usb.IsConnected ? "Connected" : "Disconnected";
            _usbStatus.Text = $"Usb: {usbStatus}";
        }

        private void OnConnect(object sender, EventArgs e)
        {
            if (_usb.IsConnected && _autoMsSqlChecked) _mssql.Start();
            if(_autoRabbitMqChecked) _rabbitMq.Start();
            ShowBalloon(true);
        }

        private void ShowBalloon(bool connected)
        {
            _trayIcon.BalloonTipTitle = connected ? "Connected" : "Disconnected";
            var message = "USB: " + (_usb.IsConnected ? "Connected" : "Disconnected");
            message += Environment.NewLine + "MsSql: " + _mssql.Status();
            message += Environment.NewLine + "RabbitMq: " + _rabbitMq.Status();
            _trayIcon.BalloonTipText = message;
            _trayIcon.ShowBalloonTip(5000);
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            if(_autoRabbitMqChecked) _rabbitMq.Stop();
            if(_autoMsSqlChecked) _mssql.Stop();
            if(_autoDisconnectUsbChecked) _usb.Disconnect();
            ShowBalloon(false);
        }

        private Icon CreateIconFromRersource(string resource)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var imageStream = assembly.GetManifestResourceStream("UsbDiskMonitor." + resource);
                if(imageStream == null) throw new Exception("Null stream");
                return new Icon(imageStream);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create an icon from resource '{resource}' because: {ex.Message}");
                return null;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                _trayIcon.Dispose();
                _usb.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
