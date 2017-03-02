using System;
using System.ServiceProcess;
using log4net;

namespace UsbDiskMonitor
{
    public class WindowsService
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(WindowsService));
        private readonly string _serviceName;

        private ServiceController ServiceController => new ServiceController(_serviceName);

        public WindowsService(string name)
        {
            _serviceName = name;
        }

        public void Start()
        {
            try
            {
                if (ServiceController.Status != ServiceControllerStatus.Stopped)
                {
                    _logger.Warn($"Cannot start '{_serviceName}' with status {ServiceController.Status} because it is not {ServiceControllerStatus.Stopped}");
                    return;
                }

                _logger.Info($"Starting {_serviceName}");
                ServiceController.Start();
                ServiceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                _logger.Info($"Started {_serviceName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to start '{_serviceName}' because: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                if (ServiceController.Status != ServiceControllerStatus.Running)
                {
                    _logger.Warn($"Cannot stop '{_serviceName}' with status {ServiceController.Status} because it is not {ServiceControllerStatus.Running}");
                    return;
                }

                _logger.Info($"Stopping {_serviceName}");
                ServiceController.Stop();
                ServiceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                _logger.Info($"Stopped {_serviceName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to stop '{_serviceName}' because: {ex.Message}");
            }
        }

        public string Status()
        {
            try
            {
                switch (ServiceController.Status)
                {
                    case ServiceControllerStatus.Running:
                        return "Running";
                    case ServiceControllerStatus.Stopped:
                        return "Stopped";
                    case ServiceControllerStatus.Paused:
                        return "Paused";
                    case ServiceControllerStatus.StopPending:
                        return "Stopping";
                    case ServiceControllerStatus.StartPending:
                        return "Starting";
                    default:
                        return "Status Changing";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get status for '{_serviceName}' because: {ex.Message}");
                return "Unknown";
            }
        }
    }
}