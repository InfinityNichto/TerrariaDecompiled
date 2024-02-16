using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets;

[UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
internal unsafe delegate bool AcceptExDelegate(SafeSocketHandle listenSocketHandle, SafeSocketHandle acceptSocketHandle, IntPtr buffer, int len, int localAddressLength, int remoteAddressLength, out int bytesReceived, NativeOverlapped* overlapped);
