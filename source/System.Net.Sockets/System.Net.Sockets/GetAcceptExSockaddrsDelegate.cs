using System.Runtime.InteropServices;

namespace System.Net.Sockets;

[UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
internal delegate void GetAcceptExSockaddrsDelegate(IntPtr buffer, int receiveDataLength, int localAddressLength, int remoteAddressLength, out IntPtr localSocketAddress, out int localSocketAddressLength, out IntPtr remoteSocketAddress, out int remoteSocketAddressLength);
