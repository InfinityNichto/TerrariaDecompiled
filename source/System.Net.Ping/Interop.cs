using System;
using System.Net.Internals;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class IpHlpApi
	{
		internal struct IPOptions
		{
			internal byte ttl;

			internal byte tos;

			internal byte flags;

			internal byte optionsSize;

			internal IntPtr optionsData;

			internal IPOptions(PingOptions options)
			{
				ttl = 128;
				tos = 0;
				flags = 0;
				optionsSize = 0;
				optionsData = IntPtr.Zero;
				if (options != null)
				{
					ttl = (byte)options.Ttl;
					if (options.DontFragment)
					{
						flags = 2;
					}
				}
			}
		}

		internal struct IcmpEchoReply
		{
			internal uint address;

			internal uint status;

			internal uint roundTripTime;

			internal ushort dataSize;

			internal ushort reserved;

			internal IntPtr data;

			internal IPOptions options;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct Ipv6Address
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			internal byte[] Goo;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal byte[] Address;

			internal uint ScopeID;
		}

		internal struct Icmp6EchoReply
		{
			internal Ipv6Address Address;

			internal uint Status;

			internal uint RoundTripTime;

			internal IntPtr data;
		}

		internal sealed class SafeCloseIcmpHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			public SafeCloseIcmpHandle()
				: base(ownsHandle: true)
			{
			}

			protected override bool ReleaseHandle()
			{
				return IcmpCloseHandle(handle);
			}
		}

		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern SafeCloseIcmpHandle IcmpCreateFile();

		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern SafeCloseIcmpHandle Icmp6CreateFile();

		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern bool IcmpCloseHandle(IntPtr handle);

		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern uint IcmpSendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, uint ipAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);

		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern uint Icmp6SendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);
	}

	internal static class Winsock
	{
		[StructLayout(LayoutKind.Sequential, Size = 408)]
		private struct WSAData
		{
		}

		private static int s_initialized;

		[DllImport("ws2_32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern SocketError closesocket([In] IntPtr socketHandle);

		[DllImport("ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern IntPtr WSASocketW([In] AddressFamily addressFamily, [In] System.Net.Internals.SocketType socketType, [In] int protocolType, [In] IntPtr protocolInfo, [In] int group, [In] int flags);

		internal static void EnsureInitialized()
		{
			if (s_initialized == 0)
			{
				Initialize();
			}
			unsafe static void Initialize()
			{
				Unsafe.SkipInit(out WSAData wSAData);
				SocketError socketError = WSAStartup(514, &wSAData);
				if (socketError != 0)
				{
					throw new SocketException((int)socketError);
				}
				if (Interlocked.CompareExchange(ref s_initialized, 1, 0) != 0)
				{
					socketError = WSACleanup();
				}
			}
		}

		[DllImport("ws2_32.dll")]
		private unsafe static extern SocketError WSAStartup(short wVersionRequested, WSAData* lpWSAData);

		[DllImport("ws2_32.dll")]
		private static extern SocketError WSACleanup();
	}
}
