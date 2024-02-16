using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets;

[UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
internal unsafe delegate bool DisconnectExDelegate(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved);
