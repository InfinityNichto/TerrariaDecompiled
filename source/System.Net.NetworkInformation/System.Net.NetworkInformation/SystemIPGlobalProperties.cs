using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation;

internal sealed class SystemIPGlobalProperties : IPGlobalProperties
{
	public override string HostName => System.Net.NetworkInformation.HostInformationPal.FixedInfo.hostName;

	public override string DomainName => System.Net.NetworkInformation.HostInformationPal.FixedInfo.domainName;

	public override NetBiosNodeType NodeType => (NetBiosNodeType)System.Net.NetworkInformation.HostInformationPal.FixedInfo.nodeType;

	public override string DhcpScopeName => System.Net.NetworkInformation.HostInformationPal.FixedInfo.scopeId;

	public override bool IsWinsProxy => System.Net.NetworkInformation.HostInformationPal.FixedInfo.enableProxy;

	internal SystemIPGlobalProperties()
	{
	}

	public override TcpConnectionInformation[] GetActiveTcpConnections()
	{
		List<TcpConnectionInformation> list = new List<TcpConnectionInformation>();
		List<SystemTcpConnectionInformation> allTcpConnections = GetAllTcpConnections();
		foreach (SystemTcpConnectionInformation item in allTcpConnections)
		{
			if (item.State != TcpState.Listen)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public override IPEndPoint[] GetActiveTcpListeners()
	{
		List<IPEndPoint> list = new List<IPEndPoint>();
		List<SystemTcpConnectionInformation> allTcpConnections = GetAllTcpConnections();
		foreach (SystemTcpConnectionInformation item in allTcpConnections)
		{
			if (item.State == TcpState.Listen)
			{
				list.Add(item.LocalEndPoint);
			}
		}
		return list.ToArray();
	}

	private unsafe List<SystemTcpConnectionInformation> GetAllTcpConnections()
	{
		uint dwOutBufLen = 0u;
		uint num = 0u;
		List<SystemTcpConnectionInformation> list = new List<SystemTcpConnectionInformation>();
		if (Socket.OSSupportsIPv4)
		{
			num = global::Interop.IpHlpApi.GetTcpTable(IntPtr.Zero, ref dwOutBufLen, order: true);
			while (true)
			{
				switch (num)
				{
				case 122u:
				{
					IntPtr intPtr = Marshal.AllocHGlobal((int)dwOutBufLen);
					try
					{
						num = global::Interop.IpHlpApi.GetTcpTable(intPtr, ref dwOutBufLen, order: true);
						if (num != 0)
						{
							continue;
						}
						ReadOnlySpan<byte> span = new ReadOnlySpan<byte>((void*)intPtr, (int)dwOutBufLen);
						ref readonly global::Interop.IpHlpApi.MibTcpTable reference = ref MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibTcpTable>(span);
						if (reference.numberOfEntries != 0)
						{
							span = span.Slice(sizeof(global::Interop.IpHlpApi.MibTcpTable));
							for (int i = 0; i < reference.numberOfEntries; i++)
							{
								list.Add(new SystemTcpConnectionInformation(in MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibTcpRow>(span)));
								span = span.Slice(sizeof(global::Interop.IpHlpApi.MibTcpRow));
							}
						}
					}
					finally
					{
						Marshal.FreeHGlobal(intPtr);
					}
					continue;
				}
				default:
					throw new NetworkInformationException((int)num);
				case 0u:
				case 232u:
					break;
				}
				break;
			}
		}
		if (Socket.OSSupportsIPv6)
		{
			dwOutBufLen = 0u;
			num = global::Interop.IpHlpApi.GetExtendedTcpTable(IntPtr.Zero, ref dwOutBufLen, order: true, 23u, global::Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0u);
			while (true)
			{
				switch (num)
				{
				case 122u:
				{
					IntPtr intPtr2 = Marshal.AllocHGlobal((int)dwOutBufLen);
					try
					{
						num = global::Interop.IpHlpApi.GetExtendedTcpTable(intPtr2, ref dwOutBufLen, order: true, 23u, global::Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0u);
						if (num != 0)
						{
							continue;
						}
						ReadOnlySpan<byte> span2 = new ReadOnlySpan<byte>((void*)intPtr2, (int)dwOutBufLen);
						ref readonly global::Interop.IpHlpApi.MibTcp6TableOwnerPid reference2 = ref MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibTcp6TableOwnerPid>(span2);
						if (reference2.numberOfEntries != 0)
						{
							span2 = span2.Slice(sizeof(global::Interop.IpHlpApi.MibTcp6TableOwnerPid));
							for (int j = 0; j < reference2.numberOfEntries; j++)
							{
								list.Add(new SystemTcpConnectionInformation(in MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibTcp6RowOwnerPid>(span2)));
								span2 = span2.Slice(sizeof(global::Interop.IpHlpApi.MibTcp6RowOwnerPid));
							}
						}
					}
					finally
					{
						Marshal.FreeHGlobal(intPtr2);
					}
					continue;
				}
				default:
					throw new NetworkInformationException((int)num);
				case 0u:
				case 232u:
					break;
				}
				break;
			}
		}
		return list;
	}

	public unsafe override IPEndPoint[] GetActiveUdpListeners()
	{
		uint dwOutBufLen = 0u;
		uint num = 0u;
		List<IPEndPoint> list = new List<IPEndPoint>();
		if (Socket.OSSupportsIPv4)
		{
			num = global::Interop.IpHlpApi.GetUdpTable(IntPtr.Zero, ref dwOutBufLen, order: true);
			while (true)
			{
				switch (num)
				{
				case 122u:
				{
					IntPtr intPtr = Marshal.AllocHGlobal((int)dwOutBufLen);
					try
					{
						num = global::Interop.IpHlpApi.GetUdpTable(intPtr, ref dwOutBufLen, order: true);
						if (num != 0)
						{
							continue;
						}
						ReadOnlySpan<byte> span = new ReadOnlySpan<byte>((void*)intPtr, (int)dwOutBufLen);
						ref readonly global::Interop.IpHlpApi.MibUdpTable reference = ref MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibUdpTable>(span);
						if (reference.numberOfEntries != 0)
						{
							span = span.Slice(sizeof(global::Interop.IpHlpApi.MibUdpTable));
							for (int i = 0; i < reference.numberOfEntries; i++)
							{
								ref readonly global::Interop.IpHlpApi.MibUdpRow reference2 = ref MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibUdpRow>(span);
								int port = (reference2.localPort1 << 8) | reference2.localPort2;
								list.Add(new IPEndPoint(reference2.localAddr, port));
								span = span.Slice(sizeof(global::Interop.IpHlpApi.MibUdpRow));
							}
						}
					}
					finally
					{
						Marshal.FreeHGlobal(intPtr);
					}
					continue;
				}
				default:
					throw new NetworkInformationException((int)num);
				case 0u:
				case 232u:
					break;
				}
				break;
			}
		}
		if (Socket.OSSupportsIPv6)
		{
			dwOutBufLen = 0u;
			num = global::Interop.IpHlpApi.GetExtendedUdpTable(IntPtr.Zero, ref dwOutBufLen, order: true, 23u, global::Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0u);
			while (true)
			{
				switch (num)
				{
				case 122u:
				{
					IntPtr intPtr2 = Marshal.AllocHGlobal((int)dwOutBufLen);
					try
					{
						num = global::Interop.IpHlpApi.GetExtendedUdpTable(intPtr2, ref dwOutBufLen, order: true, 23u, global::Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0u);
						if (num != 0)
						{
							continue;
						}
						ReadOnlySpan<byte> span2 = new ReadOnlySpan<byte>((void*)intPtr2, (int)dwOutBufLen);
						ref readonly global::Interop.IpHlpApi.MibUdp6TableOwnerPid reference3 = ref MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibUdp6TableOwnerPid>(span2);
						if (reference3.numberOfEntries != 0)
						{
							span2 = span2.Slice(sizeof(global::Interop.IpHlpApi.MibUdp6TableOwnerPid));
							for (int j = 0; j < reference3.numberOfEntries; j++)
							{
								ref readonly global::Interop.IpHlpApi.MibUdp6RowOwnerPid reference4 = ref MemoryMarshal.AsRef<global::Interop.IpHlpApi.MibUdp6RowOwnerPid>(span2);
								int port2 = (reference4.localPort1 << 8) | reference4.localPort2;
								list.Add(new IPEndPoint(new IPAddress(reference4.localAddrAsSpan, reference4.localScopeId), port2));
								span2 = span2.Slice(sizeof(global::Interop.IpHlpApi.MibUdp6RowOwnerPid));
							}
						}
					}
					finally
					{
						Marshal.FreeHGlobal(intPtr2);
					}
					continue;
				}
				default:
					throw new NetworkInformationException((int)num);
				case 0u:
				case 232u:
					break;
				}
				break;
			}
		}
		return list.ToArray();
	}

	public override IPGlobalStatistics GetIPv4GlobalStatistics()
	{
		return new SystemIPGlobalStatistics(AddressFamily.InterNetwork);
	}

	public override IPGlobalStatistics GetIPv6GlobalStatistics()
	{
		return new SystemIPGlobalStatistics(AddressFamily.InterNetworkV6);
	}

	public override TcpStatistics GetTcpIPv4Statistics()
	{
		return new SystemTcpStatistics(AddressFamily.InterNetwork);
	}

	public override TcpStatistics GetTcpIPv6Statistics()
	{
		return new SystemTcpStatistics(AddressFamily.InterNetworkV6);
	}

	public override UdpStatistics GetUdpIPv4Statistics()
	{
		return new SystemUdpStatistics(AddressFamily.InterNetwork);
	}

	public override UdpStatistics GetUdpIPv6Statistics()
	{
		return new SystemUdpStatistics(AddressFamily.InterNetworkV6);
	}

	public override IcmpV4Statistics GetIcmpV4Statistics()
	{
		return new SystemIcmpV4Statistics();
	}

	public override IcmpV6Statistics GetIcmpV6Statistics()
	{
		return new SystemIcmpV6Statistics();
	}

	public override IAsyncResult BeginGetUnicastAddresses(AsyncCallback callback, object state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(GetUnicastAddressesAsync(), callback, state);
	}

	public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
	{
		return System.Threading.Tasks.TaskToApm.End<UnicastIPAddressInformationCollection>(asyncResult);
	}

	public override UnicastIPAddressInformationCollection GetUnicastAddresses()
	{
		return GetUnicastAddressesAsync().GetAwaiter().GetResult();
	}

	public override async Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
	{
		TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!TeredoHelper.UnsafeNotifyStableUnicastIpAddressTable(delegate(object s)
		{
			((TaskCompletionSource<bool>)s).TrySetResult(result: true);
		}, taskCompletionSource))
		{
			await taskCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false);
		}
		UnicastIPAddressInformationCollection unicastIPAddressInformationCollection = new UnicastIPAddressInformationCollection();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
			{
				if (!unicastIPAddressInformationCollection.Contains(unicastAddress))
				{
					unicastIPAddressInformationCollection.InternalAdd(unicastAddress);
				}
			}
		}
		return unicastIPAddressInformationCollection;
	}
}
