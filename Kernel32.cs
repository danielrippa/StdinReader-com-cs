using System;
using System.Runtime.InteropServices;

namespace IO {
    public static class Kernel32 {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool PeekNamedPipe(IntPtr hNamedPipe, byte[] lpBuffer, uint nBufferSize, 
            out uint lpBytesRead, out uint lpTotalBytesAvail, out uint lpBytesLeftThisMessage);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, 
            out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetNamedPipeHandleState(IntPtr hNamedPipe, ref uint lpMode, IntPtr lpMaxCollectionCount, IntPtr lpCollectDataTimeout);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, 
            out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    }
}