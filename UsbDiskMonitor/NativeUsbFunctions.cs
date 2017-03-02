using System;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;

namespace UsbDiskMonitor
{
    public class NativeUsbFunctions
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(NativeUsbFunctions));

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
         string lpFileName,
         uint dwDesiredAccess,
         uint dwShareMode,
         IntPtr securityAttributes,
         uint dwCreationDisposition,
         uint dwFlagsAndAttributes,
         IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            byte[] lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //private static extern bool CloseHandle(IntPtr hObject);

        const uint GenericRead = 0x80000000;
        const uint GenericWrite = 0x40000000;
        const int FileShareRead = 0x1;
        const int FileShareWrite = 0x2;
        const int FsctlLockVolume = 0x00090018;
        const int FsctlDismountVolume = 0x00090020;
        const int IoctlStorageEjectMedia = 0x2D4808;
        const int IoctlStorageMediaRemoval = 0x002D4804;

        /// <summary>
        /// Constructor for the USBEject class
        /// </summary>
        /// <param name="driveLetter">This should be the drive letter. Format: F:/, C:/..</param>
        private IntPtr CreateUsbEjectHandler(string driveLetter)
        {
            string filename = @"\\.\" + driveLetter[0] + ":";
            return CreateFile(filename, GenericRead | GenericWrite, FileShareRead | FileShareWrite, IntPtr.Zero, 0x3, 0, IntPtr.Zero);
        }

        private bool Eject(IntPtr handle)
        {
            if (LockVolume(handle) && DismountVolume(handle))
            {
                PreventRemovalOfVolume(handle, false);
                return AutoEjectVolume(handle);
            }
            return false;
        }

        private NativeUsbFunctions()
        {
            
        }

        public static bool EjectDrive(string driveLetter)
        {
            try
            {
                var instance = new NativeUsbFunctions();
                var handle = instance.CreateUsbEjectHandler(driveLetter);
                return instance.Eject(handle);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to eject drive: '{driveLetter}' because: {ex.Message}");
                return false;
            }
        }

        private bool LockVolume(IntPtr handle)
        {
            for (var i = 0; i < 10; i++)
            {
                uint byteReturned;
                if (DeviceIoControl(handle, FsctlLockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero))
                {
                    Logger.Info("Lock success!");
                    return true;
                }
                Thread.Sleep(500);
            }
            return false;
        }

        private void PreventRemovalOfVolume(IntPtr handle, bool prevent)
        {
            byte[] buf = new byte[1];
            uint retVal;

            buf[0] = (prevent) ? (byte)1 : (byte)0;
            DeviceIoControl(handle, IoctlStorageMediaRemoval, buf, 1, IntPtr.Zero, 0, out retVal, IntPtr.Zero);
        }

        private bool DismountVolume(IntPtr handle)
        {
            uint byteReturned;
            return DeviceIoControl(handle, FsctlDismountVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
        }

        private bool AutoEjectVolume(IntPtr handle)
        {
            uint byteReturned;
            return DeviceIoControl(handle, IoctlStorageEjectMedia, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
        }
    }
}