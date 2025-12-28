using System;
using System.Runtime.InteropServices;

namespace IO {
    public static class Win32 {
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_ERROR_HANDLE = -12;
        public const uint FILE_TYPE_CHAR = 0x0002;
        public const uint FILE_TYPE_DISK = 0x0001;
        public const uint FILE_TYPE_PIPE = 0x0003;
        public const uint PIPE_NOWAIT = 0x00000001;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    }
}