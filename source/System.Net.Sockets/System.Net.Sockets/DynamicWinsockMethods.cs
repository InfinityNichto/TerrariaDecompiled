using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets;

internal sealed class DynamicWinsockMethods
{
	private static readonly List<DynamicWinsockMethods> s_methodTable = new List<DynamicWinsockMethods>();

	private readonly AddressFamily _addressFamily;

	private readonly SocketType _socketType;

	private readonly ProtocolType _protocolType;

	private AcceptExDelegate _acceptEx;

	private GetAcceptExSockaddrsDelegate _getAcceptExSockaddrs;

	private ConnectExDelegate _connectEx;

	private TransmitPacketsDelegate _transmitPackets;

	private DisconnectExDelegate _disconnectEx;

	private WSARecvMsgDelegate _recvMsg;

	public static DynamicWinsockMethods GetMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
	{
		lock (s_methodTable)
		{
			DynamicWinsockMethods dynamicWinsockMethods;
			for (int i = 0; i < s_methodTable.Count; i++)
			{
				dynamicWinsockMethods = s_methodTable[i];
				if (dynamicWinsockMethods._addressFamily == addressFamily && dynamicWinsockMethods._socketType == socketType && dynamicWinsockMethods._protocolType == protocolType)
				{
					return dynamicWinsockMethods;
				}
			}
			dynamicWinsockMethods = new DynamicWinsockMethods(addressFamily, socketType, protocolType);
			s_methodTable.Add(dynamicWinsockMethods);
			return dynamicWinsockMethods;
		}
	}

	private DynamicWinsockMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
	{
		_addressFamily = addressFamily;
		_socketType = socketType;
		_protocolType = protocolType;
	}

	private unsafe static T CreateDelegate<T>([NotNull] ref T cache, SafeSocketHandle socketHandle, string guidString) where T : Delegate
	{
		Guid guid = new Guid(guidString);
		IntPtr funcPtr = IntPtr.Zero;
		if (global::Interop.Winsock.WSAIoctl(socketHandle, -939524090, ref guid, sizeof(Guid), out funcPtr, sizeof(IntPtr), out var _, IntPtr.Zero, IntPtr.Zero) != 0)
		{
			throw new SocketException();
		}
		Interlocked.CompareExchange(ref cache, Marshal.GetDelegateForFunctionPointer<T>(funcPtr), null);
		return cache;
	}

	internal AcceptExDelegate GetAcceptExDelegate(SafeSocketHandle socketHandle)
	{
		return _acceptEx ?? CreateDelegate(ref _acceptEx, socketHandle, "b5367df1cbac11cf95ca00805f48a192");
	}

	internal GetAcceptExSockaddrsDelegate GetGetAcceptExSockaddrsDelegate(SafeSocketHandle socketHandle)
	{
		return _getAcceptExSockaddrs ?? CreateDelegate(ref _getAcceptExSockaddrs, socketHandle, "b5367df2cbac11cf95ca00805f48a192");
	}

	internal ConnectExDelegate GetConnectExDelegate(SafeSocketHandle socketHandle)
	{
		return _connectEx ?? CreateDelegate(ref _connectEx, socketHandle, "25a207b9ddf346608ee976e58c74063e");
	}

	internal DisconnectExDelegate GetDisconnectExDelegate(SafeSocketHandle socketHandle)
	{
		return _disconnectEx ?? CreateDelegate(ref _disconnectEx, socketHandle, "7fda2e118630436fa031f536a6eec157");
	}

	internal WSARecvMsgDelegate GetWSARecvMsgDelegate(SafeSocketHandle socketHandle)
	{
		return _recvMsg ?? CreateDelegate(ref _recvMsg, socketHandle, "f689d7c86f1f436b8a53e54fe351c322");
	}

	internal TransmitPacketsDelegate GetTransmitPacketsDelegate(SafeSocketHandle socketHandle)
	{
		return _transmitPackets ?? CreateDelegate(ref _transmitPackets, socketHandle, "d9689da01f9011d3997100c04f68c876");
	}
}
