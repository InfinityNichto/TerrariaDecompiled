using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets;

[UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
internal unsafe delegate SocketError WSARecvMsgDelegate(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred, NativeOverlapped* overlapped, IntPtr completionRoutine);
