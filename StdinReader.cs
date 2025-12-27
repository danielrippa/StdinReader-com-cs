using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;

using static IO.Win32;
using static IO.Kernel32;

namespace IO {

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [Guid("A4B35606-B68E-4B54-A438-E2DD1B139022")]
    [ProgId("Stdin.Reader")]
    public class StdinReader {

        private IntPtr inputHandle;

        public StdinReader() {
            inputHandle = GetStdHandle(STD_INPUT_HANDLE);
        }

        public string ReadStdin(object timeoutMillisecondsParam = null) {
            int timeoutMilliseconds = 60000;

            if (timeoutMillisecondsParam != null && timeoutMillisecondsParam != DBNull.Value && timeoutMillisecondsParam.GetType() != typeof(System.Reflection.Missing)) {
                try {
                    timeoutMilliseconds = Convert.ToInt32(timeoutMillisecondsParam);
                } catch (Exception) {
                    // Ignore conversion errors and use the default
                }
            }

            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var stringBuilder = new StringBuilder();
            bool inputStarted = false;

            if (inputHandle == INVALID_HANDLE_VALUE) {
                return Serialize(new { error = "InvalidHandle", code = Marshal.GetLastWin32Error() });
            }

            uint fileType = GetFileType(inputHandle);

            if (fileType == FILE_TYPE_CHAR) {
                return Serialize(new { error = "ConsoleInputNotSupported" });
            } else if (fileType != FILE_TYPE_PIPE) {
                return Serialize(new { error = "UnsupportedHandleType", fileType = fileType });
            }

            while (stopwatch.Elapsed < timeout) {
                uint totalBytesAvail = 0;

                if (PeekNamedPipe(inputHandle, null, 0, out _, out totalBytesAvail, out _)) {
                    if (totalBytesAvail > 0) {
                        inputStarted = true;
                        uint bytesToRead = totalBytesAvail;
                        byte[] buffer = new byte[bytesToRead];
                        uint actualBytesRead = 0;

                        if (ReadFile(inputHandle, buffer, (uint)buffer.Length, out actualBytesRead, IntPtr.Zero)) {
                            stringBuilder.Append(Encoding.Default.GetString(buffer, 0, (int)actualBytesRead));
                            stopwatch.Reset();
                            stopwatch.Start();
                        } else {
                            return Serialize(new { error = new { type = "ReadError", code = Marshal.GetLastWin32Error() } });
                        }
                    } else if (inputStarted) {
                        return Serialize(new { value = stringBuilder.ToString() });
                    }
                } else {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == 109) {
                        if (stringBuilder.Length > 0) {
                            return Serialize(new { value = stringBuilder.ToString() });
                        } else {
                            return Serialize(new { error = "PipeClosed" });
                        }
                    }
                    if (stringBuilder.Length > 0) {
                        return Serialize(new { value = stringBuilder.ToString() });
                    }
                    return Serialize(new { error = new { type = "PipeError", code = lastError } });
                }

                if (!inputStarted) {
                    System.Threading.Thread.Sleep(10);
                }
            }

            if (stringBuilder.Length > 0) {
                return Serialize(new { value = stringBuilder.ToString() });
            }

            return Serialize(new { error = "Timeout" });
        }

        private string Serialize(object obj) {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }
    }
}