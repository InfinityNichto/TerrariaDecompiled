using System;
using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

internal static class Interop
{
	internal static class Kernel32
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW", ExactSpelling = true, SetLastError = true)]
		internal static extern IntPtr LoadLibraryEx(string libFilename, IntPtr reserved, int flags);
	}

	internal static class Winsock
	{
		internal struct AddressInfo
		{
			internal AddressInfoHints ai_flags;

			internal AddressFamily ai_family;

			internal int ai_socktype;

			internal int ai_protocol;

			internal IntPtr ai_addrlen;

			internal unsafe sbyte* ai_canonname;

			internal unsafe byte* ai_addr;

			internal unsafe AddressInfo* ai_next;
		}

		[StructLayout(LayoutKind.Sequential, Size = 408)]
		private struct WSAData
		{
		}

		internal struct AddressInfoEx
		{
			internal AddressInfoHints ai_flags;

			internal AddressFamily ai_family;

			internal int ai_socktype;

			internal int ai_protocol;

			internal IntPtr ai_addrlen;

			internal IntPtr ai_canonname;

			internal unsafe byte* ai_addr;

			internal IntPtr ai_blob;

			internal IntPtr ai_bloblen;

			internal IntPtr ai_provider;

			internal unsafe AddressInfoEx* ai_next;
		}

		private static int s_initialized;

		[DllImport("ws2_32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern SocketError closesocket([In] IntPtr socketHandle);

		[DllImport("ws2_32.dll", SetLastError = true)]
		internal unsafe static extern SocketError gethostname(byte* name, int namelen);

		[DllImport("ws2_32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true, ThrowOnUnmappableChar = true)]
		internal unsafe static extern SocketError GetNameInfoW(byte* pSockaddr, int SockaddrLength, char* pNodeBuffer, int NodeBufferSize, char* pServiceBuffer, int ServiceBufferSize, int Flags);

		[DllImport("ws2_32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
		internal unsafe static extern int GetAddrInfoW([In] string pNameName, [In] string pServiceName, [In] AddressInfo* pHints, [Out] AddressInfo** ppResult);

		[DllImport("ws2_32.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern void FreeAddrInfoW(AddressInfo* info);

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

		[DllImport("ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern IntPtr WSASocketW([In] AddressFamily addressFamily, [In] SocketType socketType, [In] int protocolType, [In] IntPtr protocolInfo, [In] int group, [In] int flags);

		[DllImport("ws2_32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int GetAddrInfoExW([In] string pName, [In] string pServiceName, [In] int dwNamespace, [In] IntPtr lpNspId, [In] AddressInfoEx* pHints, [Out] AddressInfoEx** ppResult, [In] IntPtr timeout, [In] NativeOverlapped* lpOverlapped, [In] delegate* unmanaged<int, int, NativeOverlapped*, void> lpCompletionRoutine, [Out] IntPtr* lpNameHandle);

		[DllImport("ws2_32.dll", ExactSpelling = true)]
		internal unsafe static extern int GetAddrInfoExCancel([In] IntPtr* lpHandle);

		[DllImport("ws2_32.dll", ExactSpelling = true)]
		internal unsafe static extern void FreeAddrInfoExW(AddressInfoEx* pAddrInfo);
	}
}
