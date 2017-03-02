using System;

namespace UsbDiskMonitor
{
    internal class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; private set; }
    }
}