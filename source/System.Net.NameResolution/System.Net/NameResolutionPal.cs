using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal static class NameResolutionPal
{
	private sealed class GetAddrInfoExState : IThreadPoolWorkItem
	{
		private unsafe GetAddrInfoExContext* _cancellationContext;

		private CancellationTokenRegistration _cancellationRegistration;

		private AsyncTaskMethodBuilder<IPHostEntry> IPHostEntryBuilder;

		private AsyncTaskMethodBuilder<IPAddress[]> IPAddressArrayBuilder;

		private object _result;

		public string HostName { get; }

		public bool JustAddresses { get; }

		public Task Task
		{
			get
			{
				if (!JustAddresses)
				{
					return IPHostEntryBuilder.Task;
				}
				return IPAddressArrayBuilder.Task;
			}
		}

		public unsafe GetAddrInfoExState(GetAddrInfoExContext* context, string hostName, bool justAddresses)
		{
			_cancellationContext = context;
			HostName = hostName;
			JustAddresses = justAddresses;
			if (justAddresses)
			{
				IPAddressArrayBuilder = AsyncTaskMethodBuilder<IPAddress[]>.Create();
				_ = IPAddressArrayBuilder.Task;
			}
			else
			{
				IPHostEntryBuilder = AsyncTaskMethodBuilder<IPHostEntry>.Create();
				_ = IPHostEntryBuilder.Task;
			}
		}

		public unsafe void RegisterForCancellation(CancellationToken cancellationToken)
		{
			if (!cancellationToken.CanBeCanceled)
			{
				return;
			}
			lock (this)
			{
				if (_cancellationContext == null)
				{
					return;
				}
				_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object o)
				{
					GetAddrInfoExState getAddrInfoExState = (GetAddrInfoExState)o;
					int num = 0;
					lock (getAddrInfoExState)
					{
						GetAddrInfoExContext* cancellationContext = getAddrInfoExState._cancellationContext;
						if (cancellationContext != null)
						{
							num = global::Interop.Winsock.GetAddrInfoExCancel(&cancellationContext->CancelHandle);
						}
					}
					if (num != 0 && num != 6 && System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(getAddrInfoExState, $"GetAddrInfoExCancel returned error {num}", "RegisterForCancellation");
					}
				}, this);
			}
		}

		public unsafe CancellationToken UnregisterAndGetCancellationToken()
		{
			lock (this)
			{
				_cancellationContext = null;
				_cancellationRegistration.Unregister();
			}
			return _cancellationRegistration.Token;
		}

		public void SetResult(object result)
		{
			_result = result;
			ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
		}

		void IThreadPoolWorkItem.Execute()
		{
			if (JustAddresses)
			{
				if (_result is Exception exception)
				{
					IPAddressArrayBuilder.SetException(exception);
				}
				else
				{
					IPAddressArrayBuilder.SetResult((IPAddress[])_result);
				}
			}
			else if (_result is Exception exception2)
			{
				IPHostEntryBuilder.SetException(exception2);
			}
			else
			{
				IPHostEntryBuilder.SetResult((IPHostEntry)_result);
			}
		}

		public IntPtr CreateHandle()
		{
			return GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Normal));
		}

		public static GetAddrInfoExState FromHandleAndFree(IntPtr handle)
		{
			GCHandle gCHandle = GCHandle.FromIntPtr(handle);
			GetAddrInfoExState result = (GetAddrInfoExState)gCHandle.Target;
			gCHandle.Free();
			return result;
		}
	}

	private struct GetAddrInfoExContext
	{
		public NativeOverlapped Overlapped;

		public unsafe global::Interop.Winsock.AddressInfoEx* Result;

		public IntPtr CancelHandle;

		public IntPtr QueryStateHandle;

		public unsafe static GetAddrInfoExContext* AllocateContext()
		{
			GetAddrInfoExContext* ptr = (GetAddrInfoExContext*)(void*)Marshal.AllocHGlobal(sizeof(GetAddrInfoExContext));
			*ptr = default(GetAddrInfoExContext);
			return ptr;
		}

		public unsafe static void FreeContext(GetAddrInfoExContext* context)
		{
			if (context->Result != null)
			{
				global::Interop.Winsock.FreeAddrInfoExW(context->Result);
			}
			Marshal.FreeHGlobal((IntPtr)context);
		}
	}

	private static volatile int s_getAddrInfoExSupported;

	public static bool SupportsGetAddrInfoAsync
	{
		get
		{
			int num = s_getAddrInfoExSupported;
			if (num == 0)
			{
				Initialize();
				num = s_getAddrInfoExSupported;
			}
			return num == 1;
			static void Initialize()
			{
				global::Interop.Winsock.EnsureInitialized();
				IntPtr handle = global::Interop.Kernel32.LoadLibraryEx("ws2_32.dll", IntPtr.Zero, 2048);
				IntPtr address;
				bool flag = NativeLibrary.TryGetExport(handle, "GetAddrInfoExCancel", out address);
				Interlocked.CompareExchange(ref s_getAddrInfoExSupported, flag ? 1 : (-1), 0);
			}
		}
	}

	public unsafe static SocketError TryGetAddrInfo(string name, bool justAddresses, AddressFamily addressFamily, out string hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode)
	{
		global::Interop.Winsock.EnsureInitialized();
		aliases = Array.Empty<string>();
		global::Interop.Winsock.AddressInfo addressInfo = default(global::Interop.Winsock.AddressInfo);
		addressInfo.ai_family = addressFamily;
		global::Interop.Winsock.AddressInfo addressInfo2 = addressInfo;
		if (!justAddresses)
		{
			addressInfo2.ai_flags = AddressInfoHints.AI_CANONNAME;
		}
		global::Interop.Winsock.AddressInfo* ptr = null;
		try
		{
			SocketError addrInfoW = (SocketError)global::Interop.Winsock.GetAddrInfoW(name, null, &addressInfo2, &ptr);
			if (addrInfoW != 0)
			{
				nativeErrorCode = (int)addrInfoW;
				hostName = name;
				addresses = Array.Empty<IPAddress>();
				return addrInfoW;
			}
			addresses = ParseAddressInfo(ptr, justAddresses, out hostName);
			nativeErrorCode = 0;
			return SocketError.Success;
		}
		finally
		{
			if (ptr != null)
			{
				global::Interop.Winsock.FreeAddrInfoW(ptr);
			}
		}
	}

	public unsafe static string TryGetNameInfo(IPAddress addr, out SocketError errorCode, out int nativeErrorCode)
	{
		global::Interop.Winsock.EnsureInitialized();
		SocketAddress socketAddress = new IPEndPoint(addr, 0).Serialize();
		Span<byte> span = ((socketAddress.Size > 64) ? ((Span<byte>)new byte[socketAddress.Size]) : stackalloc byte[64]);
		Span<byte> span2 = span;
		for (int i = 0; i < socketAddress.Size; i++)
		{
			span2[i] = socketAddress[i];
		}
		char* ptr = stackalloc char[1025];
		fixed (byte* pSockaddr = span2)
		{
			errorCode = global::Interop.Winsock.GetNameInfoW(pSockaddr, socketAddress.Size, ptr, 1025, null, 0, 4);
		}
		if (errorCode == SocketError.Success)
		{
			nativeErrorCode = 0;
			return new string(ptr);
		}
		nativeErrorCode = (int)errorCode;
		return null;
	}

	public unsafe static string GetHostName()
	{
		global::Interop.Winsock.EnsureInitialized();
		byte* ptr = stackalloc byte[256];
		SocketError socketError = global::Interop.Winsock.gethostname(ptr, 256);
		if (socketError != 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"GetHostName failed with {socketError}", "GetHostName");
			}
			throw new SocketException();
		}
		return new string((sbyte*)ptr);
	}

	public unsafe static Task GetAddrInfoAsync(string hostName, bool justAddresses, AddressFamily family, CancellationToken cancellationToken)
	{
		global::Interop.Winsock.EnsureInitialized();
		GetAddrInfoExContext* ptr = GetAddrInfoExContext.AllocateContext();
		GetAddrInfoExState getAddrInfoExState;
		try
		{
			getAddrInfoExState = new GetAddrInfoExState(ptr, hostName, justAddresses);
			ptr->QueryStateHandle = getAddrInfoExState.CreateHandle();
		}
		catch
		{
			GetAddrInfoExContext.FreeContext(ptr);
			throw;
		}
		global::Interop.Winsock.AddressInfoEx addressInfoEx = default(global::Interop.Winsock.AddressInfoEx);
		addressInfoEx.ai_family = family;
		global::Interop.Winsock.AddressInfoEx addressInfoEx2 = addressInfoEx;
		if (!justAddresses)
		{
			addressInfoEx2.ai_flags = AddressInfoHints.AI_CANONNAME;
		}
		SocketError addrInfoExW = (SocketError)global::Interop.Winsock.GetAddrInfoExW(hostName, null, 0, IntPtr.Zero, &addressInfoEx2, &ptr->Result, IntPtr.Zero, &ptr->Overlapped, (delegate* unmanaged<int, int, NativeOverlapped*, void>)(delegate*<int, int, NativeOverlapped*, void>)(&GetAddressInfoExCallback), &ptr->CancelHandle);
		switch (addrInfoExW)
		{
		case SocketError.IOPending:
			getAddrInfoExState.RegisterForCancellation(cancellationToken);
			break;
		case (SocketError)10111:
		case SocketError.TryAgain:
			GetAddrInfoExContext.FreeContext(ptr);
			return null;
		default:
			ProcessResult(addrInfoExW, ptr);
			break;
		}
		return getAddrInfoExState.Task;
	}

	[UnmanagedCallersOnly]
	private unsafe static void GetAddressInfoExCallback(int error, int bytes, NativeOverlapped* overlapped)
	{
		ProcessResult((SocketError)error, (GetAddrInfoExContext*)overlapped);
	}

	private unsafe static void ProcessResult(SocketError errorCode, GetAddrInfoExContext* context)
	{
		try
		{
			GetAddrInfoExState getAddrInfoExState = GetAddrInfoExState.FromHandleAndFree(context->QueryStateHandle);
			CancellationToken token = getAddrInfoExState.UnregisterAndGetCancellationToken();
			if (errorCode == SocketError.Success)
			{
				string hostName;
				IPAddress[] array = ParseAddressInfoEx(context->Result, getAddrInfoExState.JustAddresses, out hostName);
				getAddrInfoExState.SetResult(getAddrInfoExState.JustAddresses ? ((object)array) : ((object)new IPHostEntry
				{
					HostName = (hostName ?? getAddrInfoExState.HostName),
					Aliases = Array.Empty<string>(),
					AddressList = array
				}));
			}
			else
			{
				Exception currentStackTrace = ((errorCode == (SocketError)10111 && token.IsCancellationRequested) ? ((Exception)new OperationCanceledException(token)) : ((Exception)(object)new SocketException((int)errorCode)));
				getAddrInfoExState.SetResult(ExceptionDispatchInfo.SetCurrentStackTrace(currentStackTrace));
			}
		}
		finally
		{
			GetAddrInfoExContext.FreeContext(context);
		}
	}

	private unsafe static IPAddress[] ParseAddressInfo(global::Interop.Winsock.AddressInfo* addressInfoPtr, bool justAddresses, out string hostName)
	{
		int num = 0;
		for (global::Interop.Winsock.AddressInfo* ptr = addressInfoPtr; ptr != null; ptr = ptr->ai_next)
		{
			int num2 = (int)ptr->ai_addrlen;
			if (ptr->ai_family == AddressFamily.InterNetwork)
			{
				if (num2 == 16)
				{
					num++;
				}
			}
			else if (SocketProtocolSupportPal.OSSupportsIPv6 && ptr->ai_family == AddressFamily.InterNetworkV6 && num2 == 28)
			{
				num++;
			}
		}
		IPAddress[] array = new IPAddress[num];
		num = 0;
		string text = (justAddresses ? "NONNULLSENTINEL" : null);
		for (global::Interop.Winsock.AddressInfo* ptr2 = addressInfoPtr; ptr2 != null; ptr2 = ptr2->ai_next)
		{
			if (text == null && ptr2->ai_canonname != null)
			{
				text = Marshal.PtrToStringUni((IntPtr)ptr2->ai_canonname);
			}
			int num3 = (int)ptr2->ai_addrlen;
			ReadOnlySpan<byte> socketAddress = new ReadOnlySpan<byte>(ptr2->ai_addr, num3);
			if (ptr2->ai_family == AddressFamily.InterNetwork)
			{
				if (num3 == 16)
				{
					array[num++] = CreateIPv4Address(socketAddress);
				}
			}
			else if (SocketProtocolSupportPal.OSSupportsIPv6 && ptr2->ai_family == AddressFamily.InterNetworkV6 && num3 == 28)
			{
				array[num++] = CreateIPv6Address(socketAddress);
			}
		}
		hostName = (justAddresses ? null : text);
		return array;
	}

	private unsafe static IPAddress[] ParseAddressInfoEx(global::Interop.Winsock.AddressInfoEx* addressInfoExPtr, bool justAddresses, out string hostName)
	{
		int num = 0;
		for (global::Interop.Winsock.AddressInfoEx* ptr = addressInfoExPtr; ptr != null; ptr = ptr->ai_next)
		{
			int num2 = (int)ptr->ai_addrlen;
			if (ptr->ai_family == AddressFamily.InterNetwork)
			{
				if (num2 == 16)
				{
					num++;
				}
			}
			else if (SocketProtocolSupportPal.OSSupportsIPv6 && ptr->ai_family == AddressFamily.InterNetworkV6 && num2 == 28)
			{
				num++;
			}
		}
		IPAddress[] array = new IPAddress[num];
		num = 0;
		string text = (justAddresses ? "NONNULLSENTINEL" : null);
		for (global::Interop.Winsock.AddressInfoEx* ptr2 = addressInfoExPtr; ptr2 != null; ptr2 = ptr2->ai_next)
		{
			if (text == null && ptr2->ai_canonname != IntPtr.Zero)
			{
				text = Marshal.PtrToStringUni(ptr2->ai_canonname);
			}
			int num3 = (int)ptr2->ai_addrlen;
			ReadOnlySpan<byte> socketAddress = new ReadOnlySpan<byte>(ptr2->ai_addr, num3);
			if (ptr2->ai_family == AddressFamily.InterNetwork)
			{
				if (num3 == 16)
				{
					array[num++] = CreateIPv4Address(socketAddress);
				}
			}
			else if (SocketProtocolSupportPal.OSSupportsIPv6 && ptr2->ai_family == AddressFamily.InterNetworkV6 && num3 == 28)
			{
				array[num++] = CreateIPv6Address(socketAddress);
			}
		}
		hostName = (justAddresses ? null : text);
		return array;
	}

	private static IPAddress CreateIPv4Address(ReadOnlySpan<byte> socketAddress)
	{
		long newAddress = (long)System.Net.SocketAddressPal.GetIPv4Address(socketAddress) & 0xFFFFFFFFL;
		return new IPAddress(newAddress);
	}

	private static IPAddress CreateIPv6Address(ReadOnlySpan<byte> socketAddress)
	{
		Span<byte> span = stackalloc byte[16];
		System.Net.SocketAddressPal.GetIPv6Address(socketAddress, span, out var scope);
		return new IPAddress(span, scope);
	}
}
