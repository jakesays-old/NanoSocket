using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

// ReSharper disable InconsistentNaming

namespace Std.NanoMsg.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct nn_iovec
    {
        public void* iov_base;
        public int iov_len;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct nn_msghdr
    {
        public nn_iovec* msg_iov;
        public int msg_iovlen;
        public void* msg_control;
        public int msg_controllen;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct nn_pollfd
    {
        public int fd;
        public short events;
        public short revents;
    }

    [SuppressUnmanagedCodeSecurity]
    internal static unsafe class Library
    {
        private const string LibNanoMsg = "nanomsg.dll";
        private static readonly IntPtr _libHandle;

        static Library()
        {
            var nanoMsgDllPath = Path.Combine(ProcessHelpers.HostProcessDirectory, LibNanoMsg);

            if (!File.Exists(nanoMsgDllPath))
            {
                //using reflection to avoid taking a build dependency on Std.Network.Native.Binaries.dll
                var nanoMsgBinariesPath = Path.Combine(ProcessHelpers.HostProcessDirectory, "Std.Network.Native.Binaries.dll");
                var assy = Assembly.LoadFile(nanoMsgBinariesPath);
                var method = assy.GetType("Std.Network.Native.Binaries.BinaryManager")
                    .GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, null);
            }

            _libHandle = LoadLibrary(nanoMsgDllPath);
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_socket(int domain, int protocol);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int nn_connect(int s, string addr);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int nn_bind(int s, string addr);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_send(int s, byte[] buf, int len, int flags);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_recv(int s, ref void* buf, int len, int flags);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_recv_array(int s, byte[] buf, int len, int flags);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_errno();

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_close(int s);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_shutdown(int s, int how);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte* nn_strerror(int errnum);

        public static string GetErrorDescription(int errno)
        {
            var error = nn_strerror(errno);

            var len = 0;
            var ptr = error;
            //limit the length to 100 for safety
            while (*ptr++ != 0x00 && len++ < 100)
            { }

            var text = Encoding.ASCII.GetString(error, len);
            return text;
        }

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_device(int s1, int s2);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nn_term();

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_setsockopt(int s, int level, int option, IntPtr optval, int optvallen);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_getsockopt(int s, int level, int option, ref int optval, ref int optvallen);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_getsockopt_intptr(int s, int level, int option, IntPtr optval, ref int optvallen);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int nn_getsockopt_string(int s,
            int level,
            int option,
            ref string optval,
            ref int optvallen);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern void* nn_allocmsg(int size, int type);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_freemsg(void* msg);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_sendmsg(int s, nn_msghdr* msghdr, int flags);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nn_recvmsg(int s, nn_msghdr* msghdr, int flags);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int nn_symbol_count();

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string nn_symbol(int i, out int value);

        [DllImport(LibNanoMsg, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nn_poll(nn_pollfd* fds, int nfds, int timeout);
    }
}