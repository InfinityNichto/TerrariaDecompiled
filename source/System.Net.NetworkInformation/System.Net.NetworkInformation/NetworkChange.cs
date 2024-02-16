using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace System.Net.NetworkInformation;

public class NetworkChange
{
	internal static class AvailabilityChangeListener
	{
		private static readonly NetworkAddressChangedEventHandler s_addressChange = ChangedAddress;

		private static volatile bool s_isAvailable;

		private static void ChangedAddress(object sender, EventArgs eventArgs)
		{
			Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext> dictionary = null;
			lock (s_globalLock)
			{
				bool flag = SystemNetworkInterface.InternalGetIsNetworkAvailable();
				if (flag != s_isAvailable)
				{
					s_isAvailable = flag;
					if (s_availabilityChangedSubscribers.Count > 0)
					{
						dictionary = new Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext>(s_availabilityChangedSubscribers);
					}
				}
			}
			if (dictionary == null)
			{
				return;
			}
			bool flag2 = s_isAvailable;
			NetworkAvailabilityEventArgs e = (flag2 ? s_availableEventArgs : s_notAvailableEventArgs);
			ContextCallback callback = (flag2 ? s_runHandlerAvailable : s_runHandlerNotAvailable);
			foreach (KeyValuePair<NetworkAvailabilityChangedEventHandler, ExecutionContext> item in dictionary)
			{
				NetworkAvailabilityChangedEventHandler key = item.Key;
				ExecutionContext value = item.Value;
				if (value == null)
				{
					key(null, e);
				}
				else
				{
					ExecutionContext.Run(value, callback, key);
				}
			}
		}

		internal static void Start(NetworkAvailabilityChangedEventHandler caller)
		{
			if (caller == null)
			{
				return;
			}
			lock (s_globalLock)
			{
				if (s_availabilityChangedSubscribers.Count == 0)
				{
					s_isAvailable = NetworkInterface.GetIsNetworkAvailable();
					AddressChangeListener.UnsafeStart(s_addressChange);
				}
				s_availabilityChangedSubscribers.TryAdd(caller, ExecutionContext.Capture());
			}
		}

		internal static void Stop(NetworkAvailabilityChangedEventHandler caller)
		{
			if (caller == null)
			{
				return;
			}
			lock (s_globalLock)
			{
				s_availabilityChangedSubscribers.Remove(caller);
				if (s_availabilityChangedSubscribers.Count == 0)
				{
					AddressChangeListener.Stop(s_addressChange);
				}
			}
		}
	}

	internal static class AddressChangeListener
	{
		private static bool s_isListening;

		private static bool s_isPending;

		private static Socket s_ipv4Socket;

		private static Socket s_ipv6Socket;

		private static WaitHandle s_ipv4WaitHandle;

		private static WaitHandle s_ipv6WaitHandle;

		private static void AddressChangedCallback(object stateObject, bool signaled)
		{
			Dictionary<NetworkAddressChangedEventHandler, ExecutionContext> dictionary = null;
			lock (s_globalLock)
			{
				s_isPending = false;
				if (!s_isListening)
				{
					return;
				}
				s_isListening = false;
				if (s_addressChangedSubscribers.Count > 0)
				{
					dictionary = new Dictionary<NetworkAddressChangedEventHandler, ExecutionContext>(s_addressChangedSubscribers);
				}
				try
				{
					StartHelper(null, captureContext: false, (StartIPOptions)stateObject);
				}
				catch (NetworkInformationException message)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(null, message, "AddressChangedCallback");
					}
				}
			}
			if (dictionary == null)
			{
				return;
			}
			foreach (KeyValuePair<NetworkAddressChangedEventHandler, ExecutionContext> item in dictionary)
			{
				NetworkAddressChangedEventHandler key = item.Key;
				ExecutionContext value = item.Value;
				if (value == null)
				{
					key(null, EventArgs.Empty);
				}
				else
				{
					ExecutionContext.Run(value, s_runAddressChangedHandler, key);
				}
			}
		}

		internal static void Start(NetworkAddressChangedEventHandler caller)
		{
			StartHelper(caller, captureContext: true, StartIPOptions.Both);
		}

		internal static void UnsafeStart(NetworkAddressChangedEventHandler caller)
		{
			StartHelper(caller, captureContext: false, StartIPOptions.Both);
		}

		private static void StartHelper(NetworkAddressChangedEventHandler caller, bool captureContext, StartIPOptions startIPOptions)
		{
			lock (s_globalLock)
			{
				if (s_ipv4Socket == null)
				{
					if (Socket.OSSupportsIPv4)
					{
						s_ipv4Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP)
						{
							Blocking = false
						};
						s_ipv4WaitHandle = new AutoResetEvent(initialState: false);
					}
					if (Socket.OSSupportsIPv6)
					{
						s_ipv6Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.IP)
						{
							Blocking = false
						};
						s_ipv6WaitHandle = new AutoResetEvent(initialState: false);
					}
				}
				if (caller != null)
				{
					s_addressChangedSubscribers.TryAdd(caller, captureContext ? ExecutionContext.Capture() : null);
				}
				if (s_isListening || s_addressChangedSubscribers.Count == 0)
				{
					return;
				}
				if (!s_isPending)
				{
					if (Socket.OSSupportsIPv4 && (startIPOptions & StartIPOptions.StartIPv4) != 0)
					{
						ThreadPool.RegisterWaitForSingleObject(s_ipv4WaitHandle, AddressChangedCallback, StartIPOptions.StartIPv4, -1, executeOnlyOnce: true);
						if (global::Interop.Winsock.WSAIoctl_Blocking(s_ipv4Socket.SafeHandle, 671088663, null, 0, null, 0, out var _, IntPtr.Zero, IntPtr.Zero) != 0)
						{
							NetworkInformationException ex = new NetworkInformationException();
							if ((long)ex.ErrorCode != 10035)
							{
								throw ex;
							}
						}
						if (global::Interop.Winsock.WSAEventSelect(s_ipv4Socket.SafeHandle, s_ipv4WaitHandle.GetSafeWaitHandle(), global::Interop.Winsock.AsyncEventBits.FdAddressListChange) != 0)
						{
							throw new NetworkInformationException();
						}
					}
					if (Socket.OSSupportsIPv6 && (startIPOptions & StartIPOptions.StartIPv6) != 0)
					{
						ThreadPool.RegisterWaitForSingleObject(s_ipv6WaitHandle, AddressChangedCallback, StartIPOptions.StartIPv6, -1, executeOnlyOnce: true);
						if (global::Interop.Winsock.WSAIoctl_Blocking(s_ipv6Socket.SafeHandle, 671088663, null, 0, null, 0, out var _, IntPtr.Zero, IntPtr.Zero) != 0)
						{
							NetworkInformationException ex2 = new NetworkInformationException();
							if ((long)ex2.ErrorCode != 10035)
							{
								throw ex2;
							}
						}
						if (global::Interop.Winsock.WSAEventSelect(s_ipv6Socket.SafeHandle, s_ipv6WaitHandle.GetSafeWaitHandle(), global::Interop.Winsock.AsyncEventBits.FdAddressListChange) != 0)
						{
							throw new NetworkInformationException();
						}
					}
				}
				s_isListening = true;
				s_isPending = true;
			}
		}

		internal static void Stop(NetworkAddressChangedEventHandler caller)
		{
			if (caller == null)
			{
				return;
			}
			lock (s_globalLock)
			{
				s_addressChangedSubscribers.Remove(caller);
				if (s_addressChangedSubscribers.Count == 0 && s_isListening)
				{
					s_isListening = false;
				}
			}
		}
	}

	private static readonly Dictionary<NetworkAddressChangedEventHandler, ExecutionContext> s_addressChangedSubscribers = new Dictionary<NetworkAddressChangedEventHandler, ExecutionContext>();

	private static readonly Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext> s_availabilityChangedSubscribers = new Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext>();

	private static readonly NetworkAvailabilityEventArgs s_availableEventArgs = new NetworkAvailabilityEventArgs(isAvailable: true);

	private static readonly NetworkAvailabilityEventArgs s_notAvailableEventArgs = new NetworkAvailabilityEventArgs(isAvailable: false);

	private static readonly ContextCallback s_runHandlerAvailable = RunAvailabilityHandlerAvailable;

	private static readonly ContextCallback s_runHandlerNotAvailable = RunAvailabilityHandlerNotAvailable;

	private static readonly ContextCallback s_runAddressChangedHandler = RunAddressChangedHandler;

	private static readonly object s_globalLock = new object();

	public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
	{
		add
		{
			AvailabilityChangeListener.Start(value);
		}
		remove
		{
			AvailabilityChangeListener.Stop(value);
		}
	}

	public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
	{
		add
		{
			AddressChangeListener.Start(value);
		}
		remove
		{
			AddressChangeListener.Stop(value);
		}
	}

	[Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
	public static void RegisterNetworkChange(NetworkChange nc)
	{
	}

	private static void RunAddressChangedHandler(object state)
	{
		((NetworkAddressChangedEventHandler)state)(null, EventArgs.Empty);
	}

	private static void RunAvailabilityHandlerAvailable(object state)
	{
		((NetworkAvailabilityChangedEventHandler)state)(null, s_availableEventArgs);
	}

	private static void RunAvailabilityHandlerNotAvailable(object state)
	{
		((NetworkAvailabilityChangedEventHandler)state)(null, s_notAvailableEventArgs);
	}
}
