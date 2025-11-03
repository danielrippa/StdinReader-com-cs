using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Diagnostics;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[Guid("A4B35606-B68E-4B54-A438-E2DD1B139022")]
[ProgId("Stdin.Reader")]
public class StdinReader
{
    private IntPtr inputHandle;

    private const uint STD_INPUT_HANDLE = unchecked((uint)-10);
    private const uint FILE_TYPE_CHAR   = 0x0002;
    private const uint FILE_TYPE_PIPE   = 0x0003;
    private const uint FILE_TYPE_DISK   = 0x0001;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(uint nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetFileType(IntPtr hFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool PeekNamedPipe(
        IntPtr handle,
        byte[] lpBuffer,
        uint nBufferSize,
        out uint lpBytesRead,
        out uint lpTotalBytesAvail,
        out uint lpBytesLeftThisMessage);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        IntPtr hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    public StdinReader()
    {
        inputHandle = GetStdHandle(STD_INPUT_HANDLE);
    }

    public string ReadStdin(int timeoutMilliseconds = 500, int maxChunkSizeKB = 64)
    {
        var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
        var sw = Stopwatch.StartNew();
        var sb = new StringBuilder();
        bool inputReceived = false;
        uint chunkSize = (uint)maxChunkSizeKB * 1024;
        byte[] buffer = new byte[chunkSize];

        if (inputHandle == IntPtr.Zero)
        {
            return Serialize(new
            {
                error = "NoStdin",
                hint = "stdin not available. Use cscript.exe or pipe from cmd/PowerShell."
            });
        }

        if (inputHandle == INVALID_HANDLE_VALUE)
        {
            return Serialize(new
            {
                error = "InvalidHandle",
                code = Marshal.GetLastWin32Error()
            });
        }

        uint fileType = GetFileType(inputHandle);
        if (fileType == FILE_TYPE_CHAR)
            return Serialize(new { error = "ConsoleInputNotSupported" });

        bool isPipe = fileType == FILE_TYPE_PIPE;
        bool isFile = fileType == FILE_TYPE_DISK;

        if (!isPipe && !isFile)
            return Serialize(new { error = "UnsupportedHandleType", fileType });

        while (sw.Elapsed < timeout)
        {
            uint bytesToRead = chunkSize;
            bool hasData = false;

            if (isPipe)
            {
                if (PeekNamedPipe(inputHandle, null, 0, out _, out uint avail, out _))
                {
                    if (avail > 0)
                    {
                        hasData = true;
                        bytesToRead = Math.Min(avail, chunkSize);
                    }
                    else if (inputReceived)
                        break;
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err == 109) break;
                }
            }
            else
            {
                hasData = true;
            }

            if (hasData)
            {
                if (ReadFile(inputHandle, buffer, bytesToRead, out uint read, IntPtr.Zero))
                {
                    if (read == 0) break;
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, (int)read));
                    inputReceived = true;
                }
                else
                {
                    return Serialize(new
                    {
                        error = new { type = "ReadError", code = Marshal.GetLastWin32Error() }
                    });
                }
            }
            else if (!inputReceived)
            {
                System.Threading.Thread.Sleep(10);
            }
        }

        return (sb.Length > 0
            ? Serialize(new { value = sb.ToString() })
            : Serialize(new { error = "Timeout" }));
    }

    private string Serialize(object obj)
    {
        var serializer = new JavaScriptSerializer();
        return serializer.Serialize(obj);
    }
}