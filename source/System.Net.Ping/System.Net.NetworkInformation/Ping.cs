using System.ComponentModel;
using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation;

public class Ping : Component
{
	private readonly ManualResetEventSlim _lockObject = new ManualResetEventSlim(initialState: true);

	private SendOrPostCallback _onPingCompletedDelegate;

	private bool _disposeRequested;

	private byte[] _defaultSendBuffer;

	private bool _canceled;

	private int _status;

	private static readonly SafeWaitHandle s_nullSafeWaitHandle = new SafeWaitHandle(IntPtr.Zero, ownsHandle: true);

	private int _sendSize;

	private bool _ipv6;

	private ManualResetEvent _pingEvent;

	private RegisteredWaitHandle _registeredWait;

	private SafeLocalAllocHandle _requestBuffer;

	private SafeLocalAllocHandle _replyBuffer;

	private global::Interop.IpHlpApi.SafeCloseIcmpHandle _handlePingV4;

	private global::Interop.IpHlpApi.SafeCloseIcmpHandle _handlePingV6;

	private TaskCompletionSource<PingReply> _taskCompletionSource;

	private byte[] DefaultSendBuffer
	{
		get
		{
			if (_defaultSendBuffer == null)
			{
				_defaultSendBuffer = new byte[32];
				for (int i = 0; i < 32; i++)
				{
					_defaultSendBuffer[i] = (byte)(97 + i % 23);
				}
			}
			return _defaultSendBuffer;
		}
	}

	public event PingCompletedEventHandler? PingCompleted;

	public Ping()
	{
		if (GetType() == typeof(Ping))
		{
			GC.SuppressFinalize(this);
		}
	}

	private void CheckArgs(int timeout, byte[] buffer, PingOptions options)
	{
		CheckDisposed();
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (buffer.Length > 65500)
		{
			throw new ArgumentException(System.SR.net_invalidPingBufferSize, "buffer");
		}
		if (timeout < 0)
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
	}

	private void CheckArgs(IPAddress address, int timeout, byte[] buffer, PingOptions options)
	{
		CheckArgs(timeout, buffer, options);
		if (address == null)
		{
			throw new ArgumentNullException("address");
		}
		TestIsIpSupported(address);
		if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
		{
			throw new ArgumentException(System.SR.net_invalid_ip_addr, "address");
		}
	}

	private void CheckDisposed()
	{
		if (_disposeRequested)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	private void CheckStart()
	{
		int status;
		lock (_lockObject)
		{
			status = _status;
			if (status == 0)
			{
				_canceled = false;
				_status = 1;
				_lockObject.Reset();
				return;
			}
		}
		if (status == 1)
		{
			throw new InvalidOperationException(System.SR.net_inasync);
		}
		throw new ObjectDisposedException(GetType().FullName);
	}

	private static IPAddress GetAddressSnapshot(IPAddress address)
	{
		return (address.AddressFamily == AddressFamily.InterNetwork) ? new IPAddress(address.Address) : new IPAddress(address.GetAddressBytes(), address.ScopeId);
	}

	private void Finish()
	{
		lock (_lockObject)
		{
			_status = 0;
			_lockObject.Set();
		}
		if (_disposeRequested)
		{
			InternalDispose();
		}
	}

	private void InternalDispose()
	{
		_disposeRequested = true;
		lock (_lockObject)
		{
			if (_status != 0)
			{
				return;
			}
			_status = 2;
		}
		InternalDisposeCore();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			InternalDispose();
		}
	}

	protected void OnPingCompleted(PingCompletedEventArgs e)
	{
		this.PingCompleted?.Invoke(this, e);
	}

	public PingReply Send(string hostNameOrAddress)
	{
		return Send(hostNameOrAddress, 5000, DefaultSendBuffer);
	}

	public PingReply Send(string hostNameOrAddress, int timeout)
	{
		return Send(hostNameOrAddress, timeout, DefaultSendBuffer);
	}

	public PingReply Send(IPAddress address)
	{
		return Send(address, 5000, DefaultSendBuffer);
	}

	public PingReply Send(IPAddress address, int timeout)
	{
		return Send(address, timeout, DefaultSendBuffer);
	}

	public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer)
	{
		return Send(hostNameOrAddress, timeout, buffer, null);
	}

	public PingReply Send(IPAddress address, int timeout, byte[] buffer)
	{
		return Send(address, timeout, buffer, null);
	}

	public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
	{
		if (string.IsNullOrEmpty(hostNameOrAddress))
		{
			throw new ArgumentNullException("hostNameOrAddress");
		}
		if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
		{
			return Send(address, timeout, buffer, options);
		}
		CheckArgs(timeout, buffer, options);
		return GetAddressAndSend(hostNameOrAddress, timeout, buffer, options);
	}

	public PingReply Send(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
	{
		CheckArgs(address, timeout, buffer, options);
		IPAddress addressSnapshot = GetAddressSnapshot(address);
		CheckStart();
		try
		{
			return SendPingCore(addressSnapshot, buffer, timeout, options);
		}
		catch (Exception innerException)
		{
			throw new PingException(System.SR.net_ping, innerException);
		}
		finally
		{
			Finish();
		}
	}

	public void SendAsync(string hostNameOrAddress, object? userToken)
	{
		SendAsync(hostNameOrAddress, 5000, DefaultSendBuffer, userToken);
	}

	public void SendAsync(string hostNameOrAddress, int timeout, object? userToken)
	{
		SendAsync(hostNameOrAddress, timeout, DefaultSendBuffer, userToken);
	}

	public void SendAsync(IPAddress address, object? userToken)
	{
		SendAsync(address, 5000, DefaultSendBuffer, userToken);
	}

	public void SendAsync(IPAddress address, int timeout, object? userToken)
	{
		SendAsync(address, timeout, DefaultSendBuffer, userToken);
	}

	public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, object? userToken)
	{
		SendAsync(hostNameOrAddress, timeout, buffer, null, userToken);
	}

	public void SendAsync(IPAddress address, int timeout, byte[] buffer, object? userToken)
	{
		SendAsync(address, timeout, buffer, null, userToken);
	}

	public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options, object? userToken)
	{
		TranslateTaskToEap(userToken, SendPingAsync(hostNameOrAddress, timeout, buffer, options));
	}

	public void SendAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options, object? userToken)
	{
		TranslateTaskToEap(userToken, SendPingAsync(address, timeout, buffer, options));
	}

	private void TranslateTaskToEap(object userToken, Task<PingReply> pingTask)
	{
		pingTask.ContinueWith(delegate(Task<PingReply> t, object state)
		{
			AsyncOperation asyncOperation = (AsyncOperation)state;
			PingCompletedEventArgs arg = new PingCompletedEventArgs(t.IsCompletedSuccessfully ? t.Result : null, t.Exception, t.IsCanceled, asyncOperation.UserSuppliedState);
			SendOrPostCallback d = delegate(object o)
			{
				OnPingCompleted((PingCompletedEventArgs)o);
			};
			asyncOperation.PostOperationCompleted(d, arg);
		}, AsyncOperationManager.CreateOperation(userToken), CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	public Task<PingReply> SendPingAsync(IPAddress address)
	{
		return SendPingAsync(address, 5000, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress)
	{
		return SendPingAsync(hostNameOrAddress, 5000, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, int timeout)
	{
		return SendPingAsync(address, timeout, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout)
	{
		return SendPingAsync(hostNameOrAddress, timeout, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer)
	{
		return SendPingAsync(address, timeout, buffer, null);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer)
	{
		return SendPingAsync(hostNameOrAddress, timeout, buffer, null);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
	{
		CheckArgs(address, timeout, buffer, options);
		return SendPingAsyncInternal(address, timeout, buffer, options);
	}

	private async Task<PingReply> SendPingAsyncInternal(IPAddress address, int timeout, byte[] buffer, PingOptions options)
	{
		IPAddress addressSnapshot = GetAddressSnapshot(address);
		CheckStart();
		try
		{
			Task<PingReply> task = SendPingAsyncCore(addressSnapshot, buffer, timeout, options);
			return await task.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception innerException)
		{
			throw new PingException(System.SR.net_ping, innerException);
		}
		finally
		{
			Finish();
		}
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
	{
		if (string.IsNullOrEmpty(hostNameOrAddress))
		{
			throw new ArgumentNullException("hostNameOrAddress");
		}
		if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
		{
			return SendPingAsync(address, timeout, buffer, options);
		}
		CheckArgs(timeout, buffer, options);
		return GetAddressAndSendAsync(hostNameOrAddress, timeout, buffer, options);
	}

	public void SendAsyncCancel()
	{
		lock (_lockObject)
		{
			if (!_lockObject.IsSet)
			{
				_canceled = true;
			}
		}
		_lockObject.Wait();
	}

	private PingReply GetAddressAndSend(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options)
	{
		CheckStart();
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(hostNameOrAddress);
			return SendPingCore(hostAddresses[0], buffer, timeout, options);
		}
		catch (Exception innerException)
		{
			throw new PingException(System.SR.net_ping, innerException);
		}
		finally
		{
			Finish();
		}
	}

	private async Task<PingReply> GetAddressAndSendAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options)
	{
		CheckStart();
		try
		{
			Task<PingReply> task = SendPingAsyncCore((await Dns.GetHostAddressesAsync(hostNameOrAddress).ConfigureAwait(continueOnCapturedContext: false))[0], buffer, timeout, options);
			return await task.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception innerException)
		{
			throw new PingException(System.SR.net_ping, innerException);
		}
		finally
		{
			Finish();
		}
	}

	private void TestIsIpSupported(IPAddress ip)
	{
		if (ip.AddressFamily == AddressFamily.InterNetwork && !System.Net.SocketProtocolSupportPal.OSSupportsIPv4)
		{
			throw new NotSupportedException(System.SR.net_ipv4_not_installed);
		}
		if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !System.Net.SocketProtocolSupportPal.OSSupportsIPv6)
		{
			throw new NotSupportedException(System.SR.net_ipv6_not_installed);
		}
	}

	private void InternalDisposeCore()
	{
		if (_handlePingV4 != null)
		{
			_handlePingV4.Dispose();
			_handlePingV4 = null;
		}
		if (_handlePingV6 != null)
		{
			_handlePingV6.Dispose();
			_handlePingV6 = null;
		}
		UnregisterWaitHandle();
		if (_pingEvent != null)
		{
			_pingEvent.Dispose();
			_pingEvent = null;
		}
		if (_replyBuffer != null)
		{
			_replyBuffer.Dispose();
			_replyBuffer = null;
		}
	}

	private PingReply SendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
	{
		return DoSendPingCore(address, buffer, timeout, options, isAsync: false).GetAwaiter().GetResult();
	}

	private Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
	{
		return DoSendPingCore(address, buffer, timeout, options, isAsync: true);
	}

	private Task<PingReply> DoSendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions options, bool isAsync)
	{
		TaskCompletionSource<PingReply> taskCompletionSource = null;
		if (isAsync)
		{
			taskCompletionSource = (_taskCompletionSource = new TaskCompletionSource<PingReply>());
		}
		_ipv6 = address.AddressFamily == AddressFamily.InterNetworkV6;
		_sendSize = buffer.Length;
		InitialiseIcmpHandle();
		if (_replyBuffer == null)
		{
			_replyBuffer = SafeLocalAllocHandle.LocalAlloc(65791);
		}
		int num;
		try
		{
			if (isAsync)
			{
				RegisterWaitHandle();
			}
			SetUnmanagedStructures(buffer);
			num = SendEcho(address, buffer, timeout, options, isAsync);
		}
		catch
		{
			Cleanup(isAsync);
			throw;
		}
		if (num == 0)
		{
			num = Marshal.GetLastPInvokeError();
			if (!isAsync || (long)num != 997)
			{
				Cleanup(isAsync);
				IPStatus statusFromCode = GetStatusFromCode(num);
				return Task.FromResult(new PingReply(address, null, statusFromCode, 0L, Array.Empty<byte>()));
			}
		}
		if (taskCompletionSource != null)
		{
			return taskCompletionSource.Task;
		}
		Cleanup(isAsync);
		return Task.FromResult(CreatePingReply());
	}

	private void RegisterWaitHandle()
	{
		if (_pingEvent == null)
		{
			_pingEvent = new ManualResetEvent(initialState: false);
		}
		else
		{
			_pingEvent.Reset();
		}
		_registeredWait = ThreadPool.RegisterWaitForSingleObject(_pingEvent, delegate(object state, bool _)
		{
			((Ping)state).PingCallback();
		}, this, -1, executeOnlyOnce: true);
	}

	private void UnregisterWaitHandle()
	{
		lock (_lockObject)
		{
			if (_registeredWait != null)
			{
				_registeredWait.Unregister(null);
				_registeredWait = null;
			}
		}
	}

	private SafeWaitHandle GetWaitHandle(bool async)
	{
		if (async)
		{
			return _pingEvent.GetSafeWaitHandle();
		}
		return s_nullSafeWaitHandle;
	}

	private void InitialiseIcmpHandle()
	{
		if (!_ipv6 && _handlePingV4 == null)
		{
			_handlePingV4 = global::Interop.IpHlpApi.IcmpCreateFile();
			if (_handlePingV4.IsInvalid)
			{
				_handlePingV4 = null;
				throw new Win32Exception();
			}
		}
		else if (_ipv6 && _handlePingV6 == null)
		{
			_handlePingV6 = global::Interop.IpHlpApi.Icmp6CreateFile();
			if (_handlePingV6.IsInvalid)
			{
				_handlePingV6 = null;
				throw new Win32Exception();
			}
		}
	}

	private int SendEcho(IPAddress address, byte[] buffer, int timeout, PingOptions options, bool isAsync)
	{
		global::Interop.IpHlpApi.IPOptions options2 = new global::Interop.IpHlpApi.IPOptions(options);
		if (!_ipv6)
		{
			return (int)global::Interop.IpHlpApi.IcmpSendEcho2(_handlePingV4, GetWaitHandle(isAsync), IntPtr.Zero, IntPtr.Zero, (uint)address.Address, _requestBuffer, (ushort)buffer.Length, ref options2, _replyBuffer, 65791u, (uint)timeout);
		}
		IPEndPoint endpoint = new IPEndPoint(address, 0);
		System.Net.Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(endpoint);
		byte[] sourceSocketAddress = new byte[28];
		return (int)global::Interop.IpHlpApi.Icmp6SendEcho2(_handlePingV6, GetWaitHandle(isAsync), IntPtr.Zero, IntPtr.Zero, sourceSocketAddress, socketAddress.Buffer, _requestBuffer, (ushort)buffer.Length, ref options2, _replyBuffer, 65791u, (uint)timeout);
	}

	private PingReply CreatePingReply()
	{
		SafeLocalAllocHandle replyBuffer = _replyBuffer;
		if (_ipv6)
		{
			global::Interop.IpHlpApi.Icmp6EchoReply reply = Marshal.PtrToStructure<global::Interop.IpHlpApi.Icmp6EchoReply>(replyBuffer.DangerousGetHandle());
			return CreatePingReplyFromIcmp6EchoReply(reply, replyBuffer.DangerousGetHandle(), _sendSize);
		}
		global::Interop.IpHlpApi.IcmpEchoReply reply2 = Marshal.PtrToStructure<global::Interop.IpHlpApi.IcmpEchoReply>(replyBuffer.DangerousGetHandle());
		return CreatePingReplyFromIcmpEchoReply(reply2);
	}

	private void Cleanup(bool isAsync)
	{
		FreeUnmanagedStructures();
		if (isAsync)
		{
			UnregisterWaitHandle();
		}
	}

	private void PingCallback()
	{
		TaskCompletionSource<PingReply> taskCompletionSource = _taskCompletionSource;
		_taskCompletionSource = null;
		PingReply pingReply = null;
		Exception exception = null;
		bool flag = false;
		try
		{
			lock (_lockObject)
			{
				flag = _canceled;
				pingReply = CreatePingReply();
			}
		}
		catch (Exception innerException)
		{
			exception = new PingException(System.SR.net_ping, innerException);
		}
		finally
		{
			Cleanup(isAsync: true);
		}
		if (flag)
		{
			taskCompletionSource.SetCanceled();
		}
		else if (pingReply != null)
		{
			taskCompletionSource.SetResult(pingReply);
		}
		else
		{
			taskCompletionSource.SetException(exception);
		}
	}

	private unsafe void SetUnmanagedStructures(byte[] buffer)
	{
		_requestBuffer = SafeLocalAllocHandle.LocalAlloc(buffer.Length);
		byte* ptr = (byte*)(void*)_requestBuffer.DangerousGetHandle();
		for (int i = 0; i < buffer.Length; i++)
		{
			ptr[i] = buffer[i];
		}
	}

	private void FreeUnmanagedStructures()
	{
		if (_requestBuffer != null)
		{
			_requestBuffer.Dispose();
			_requestBuffer = null;
		}
	}

	private static IPStatus GetStatusFromCode(int statusCode)
	{
		if (statusCode != 0 && statusCode < 11000)
		{
			throw new Win32Exception(statusCode);
		}
		return (IPStatus)statusCode;
	}

	private static PingReply CreatePingReplyFromIcmpEchoReply(global::Interop.IpHlpApi.IcmpEchoReply reply)
	{
		IPAddress address = new IPAddress(reply.address);
		IPStatus statusFromCode = GetStatusFromCode((int)reply.status);
		long rtt;
		PingOptions options;
		byte[] array;
		if (statusFromCode == IPStatus.Success)
		{
			rtt = reply.roundTripTime;
			options = new PingOptions(reply.options.ttl, (reply.options.flags & 2) > 0);
			array = new byte[reply.dataSize];
			Marshal.Copy(reply.data, array, 0, reply.dataSize);
		}
		else
		{
			rtt = 0L;
			options = null;
			array = Array.Empty<byte>();
		}
		return new PingReply(address, options, statusFromCode, rtt, array);
	}

	private static PingReply CreatePingReplyFromIcmp6EchoReply(global::Interop.IpHlpApi.Icmp6EchoReply reply, IntPtr dataPtr, int sendSize)
	{
		IPAddress address = new IPAddress(reply.Address.Address, reply.Address.ScopeID);
		IPStatus statusFromCode = GetStatusFromCode((int)reply.Status);
		long rtt;
		byte[] array;
		if (statusFromCode == IPStatus.Success)
		{
			rtt = reply.RoundTripTime;
			array = new byte[sendSize];
			Marshal.Copy(dataPtr + 36, array, 0, sendSize);
		}
		else
		{
			rtt = 0L;
			array = Array.Empty<byte>();
		}
		return new PingReply(address, null, statusFromCode, rtt, array);
	}
}
