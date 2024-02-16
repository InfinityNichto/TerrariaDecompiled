using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicNativeMethods
{
	internal struct NativeApi
	{
		internal IntPtr SetContext;

		internal IntPtr GetContext;

		internal IntPtr SetCallbackHandler;

		internal IntPtr SetParam;

		internal IntPtr GetParam;

		internal IntPtr RegistrationOpen;

		internal IntPtr RegistrationClose;

		internal IntPtr RegistrationShutdown;

		internal IntPtr ConfigurationOpen;

		internal IntPtr ConfigurationClose;

		internal IntPtr ConfigurationLoadCredential;

		internal IntPtr ListenerOpen;

		internal IntPtr ListenerClose;

		internal IntPtr ListenerStart;

		internal IntPtr ListenerStop;

		internal IntPtr ConnectionOpen;

		internal IntPtr ConnectionClose;

		internal IntPtr ConnectionShutdown;

		internal IntPtr ConnectionStart;

		internal IntPtr ConnectionSetConfiguration;

		internal IntPtr ConnectionSendResumptionTicket;

		internal IntPtr StreamOpen;

		internal IntPtr StreamClose;

		internal IntPtr StreamStart;

		internal IntPtr StreamShutdown;

		internal IntPtr StreamSend;

		internal IntPtr StreamReceiveComplete;

		internal IntPtr StreamReceiveSetEnabled;

		internal IntPtr DatagramSend;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void SetCallbackHandlerDelegate(SafeHandle handle, Delegate del, IntPtr context);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate uint SetParamDelegate(SafeHandle handle, QUIC_PARAM_LEVEL level, uint param, uint bufferLength, byte* buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate uint GetParamDelegate(SafeHandle handle, QUIC_PARAM_LEVEL level, uint param, ref uint bufferLength, byte* buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint RegistrationOpenDelegate(ref RegistrationConfig config, out SafeMsQuicRegistrationHandle registrationContext);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void RegistrationCloseDelegate(IntPtr registrationContext);

	internal struct RegistrationConfig
	{
		[MarshalAs(UnmanagedType.LPUTF8Str)]
		internal string AppName;

		internal QUIC_EXECUTION_PROFILE ExecutionProfile;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate uint ConfigurationOpenDelegate(SafeMsQuicRegistrationHandle registrationContext, QuicBuffer* alpnBuffers, uint alpnBufferCount, ref QuicSettings settings, uint settingsSize, IntPtr context, out SafeMsQuicConfigurationHandle configuration);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ConfigurationCloseDelegate(IntPtr configuration);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ConfigurationLoadCredentialDelegate(SafeMsQuicConfigurationHandle configuration, ref CredentialConfig credConfig);

	internal struct QuicSettings
	{
		internal QuicSettingsIsSetFlags IsSetFlags;

		internal ulong MaxBytesPerKey;

		internal ulong HandshakeIdleTimeoutMs;

		internal ulong IdleTimeoutMs;

		internal uint TlsClientMaxSendBuffer;

		internal uint TlsServerMaxSendBuffer;

		internal uint StreamRecvWindowDefault;

		internal uint StreamRecvBufferDefault;

		internal uint ConnFlowControlWindow;

		internal uint MaxWorkerQueueDelayUs;

		internal uint MaxStatelessOperations;

		internal uint InitialWindowPackets;

		internal uint SendIdleTimeoutMs;

		internal uint InitialRttMs;

		internal uint MaxAckDelayMs;

		internal uint DisconnectTimeoutMs;

		internal uint KeepAliveIntervalMs;

		internal ushort PeerBidiStreamCount;

		internal ushort PeerUnidiStreamCount;

		internal ushort RetryMemoryLimit;

		internal ushort LoadBalancingMode;

		internal byte MaxOperationsPerDrain;

		internal QuicSettingsEnabledFlagsFlags EnabledFlags;

		internal unsafe uint* DesiredVersionsList;

		internal uint DesiredVersionsListLength;
	}

	[Flags]
	internal enum QuicSettingsIsSetFlags : ulong
	{
		MaxBytesPerKey = 1uL,
		HandshakeIdleTimeoutMs = 2uL,
		IdleTimeoutMs = 4uL,
		TlsClientMaxSendBuffer = 8uL,
		TlsServerMaxSendBuffer = 0x10uL,
		StreamRecvWindowDefault = 0x20uL,
		StreamRecvBufferDefault = 0x40uL,
		ConnFlowControlWindow = 0x80uL,
		MaxWorkerQueueDelayUs = 0x100uL,
		MaxStatelessOperations = 0x200uL,
		InitialWindowPackets = 0x400uL,
		SendIdleTimeoutMs = 0x800uL,
		InitialRttMs = 0x1000uL,
		MaxAckDelayMs = 0x2000uL,
		DisconnectTimeoutMs = 0x4000uL,
		KeepAliveIntervalMs = 0x8000uL,
		PeerBidiStreamCount = 0x10000uL,
		PeerUnidiStreamCount = 0x20000uL,
		RetryMemoryLimit = 0x40000uL,
		LoadBalancingMode = 0x80000uL,
		MaxOperationsPerDrain = 0x100000uL,
		SendBufferingEnabled = 0x200000uL,
		PacingEnabled = 0x400000uL,
		MigrationEnabled = 0x800000uL,
		DatagramReceiveEnabled = 0x1000000uL,
		ServerResumptionLevel = 0x2000000uL,
		DesiredVersionsList = 0x4000000uL,
		VersionNegotiationExtEnabled = 0x8000000uL
	}

	[Flags]
	internal enum QuicSettingsEnabledFlagsFlags : byte
	{
		SendBufferingEnabled = 1,
		PacingEnabled = 2,
		MigrationEnabled = 4,
		DatagramReceiveEnabled = 8,
		ServerResumptionLevel = 0x30,
		VersionNegotiationExtEnabled = 0x40
	}

	internal struct CredentialConfig
	{
		internal QUIC_CREDENTIAL_TYPE Type;

		internal QUIC_CREDENTIAL_FLAGS Flags;

		internal IntPtr Certificate;

		[MarshalAs(UnmanagedType.LPUTF8Str)]
		internal string Principal;

		internal IntPtr Reserved;

		internal IntPtr AsyncHandler;
	}

	internal struct CredentialConfigCertificatePkcs12
	{
		internal IntPtr Asn1Blob;

		internal uint Asn1BlobLength;

		internal IntPtr PrivateKeyPassword;
	}

	internal struct ListenerEvent
	{
		internal QUIC_LISTENER_EVENT Type;

		internal ListenerEventDataUnion Data;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct ListenerEventDataUnion
	{
		[FieldOffset(0)]
		internal ListenerEventDataNewConnection NewConnection;
	}

	internal struct ListenerEventDataNewConnection
	{
		internal unsafe NewConnectionInfo* Info;

		internal IntPtr Connection;
	}

	internal struct NewConnectionInfo
	{
		internal uint QuicVersion;

		internal IntPtr LocalAddress;

		internal IntPtr RemoteAddress;

		internal uint CryptoBufferLength;

		internal ushort ClientAlpnListLength;

		internal ushort ServerNameLength;

		internal byte NegotiatedAlpnLength;

		internal IntPtr CryptoBuffer;

		internal IntPtr ClientAlpnList;

		internal IntPtr NegotiatedAlpn;

		internal IntPtr ServerName;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ListenerCallbackDelegate(IntPtr listener, IntPtr context, ref ListenerEvent evt);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ListenerOpenDelegate(SafeMsQuicRegistrationHandle registration, ListenerCallbackDelegate handler, IntPtr context, out SafeMsQuicListenerHandle listener);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ListenerCloseDelegate(IntPtr listener);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate uint ListenerStartDelegate(SafeMsQuicListenerHandle listener, QuicBuffer* alpnBuffers, uint alpnBufferCount, ref SOCKADDR_INET localAddress);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ListenerStopDelegate(SafeMsQuicListenerHandle listener);

	internal struct ConnectionEventDataConnected
	{
		internal byte SessionResumed;

		internal byte NegotiatedAlpnLength;

		internal IntPtr NegotiatedAlpn;
	}

	internal struct ConnectionEventDataShutdownInitiatedByTransport
	{
		internal uint Status;
	}

	internal struct ConnectionEventDataShutdownInitiatedByPeer
	{
		internal long ErrorCode;
	}

	internal struct ConnectionEventDataShutdownComplete
	{
		internal ConnectionEventDataShutdownCompleteFlags Flags;
	}

	[Flags]
	internal enum ConnectionEventDataShutdownCompleteFlags : byte
	{
		HandshakeCompleted = 1,
		PeerAcknowledgedShutdown = 2,
		AppCloseInProgress = 4
	}

	internal struct ConnectionEventDataLocalAddressChanged
	{
		internal IntPtr Address;
	}

	internal struct ConnectionEventDataPeerAddressChanged
	{
		internal IntPtr Address;
	}

	internal struct ConnectionEventDataPeerStreamStarted
	{
		internal IntPtr Stream;

		internal QUIC_STREAM_OPEN_FLAGS Flags;
	}

	internal struct ConnectionEventDataStreamsAvailable
	{
		internal ushort BiDirectionalCount;

		internal ushort UniDirectionalCount;
	}

	internal struct ConnectionEventPeerCertificateReceived
	{
		internal IntPtr PlatformCertificateHandle;

		internal uint DeferredErrorFlags;

		internal uint DeferredStatus;

		internal IntPtr PlatformCertificateChainHandle;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct ConnectionEventDataUnion
	{
		[FieldOffset(0)]
		internal ConnectionEventDataConnected Connected;

		[FieldOffset(0)]
		internal ConnectionEventDataShutdownInitiatedByTransport ShutdownInitiatedByTransport;

		[FieldOffset(0)]
		internal ConnectionEventDataShutdownInitiatedByPeer ShutdownInitiatedByPeer;

		[FieldOffset(0)]
		internal ConnectionEventDataShutdownComplete ShutdownComplete;

		[FieldOffset(0)]
		internal ConnectionEventDataLocalAddressChanged LocalAddressChanged;

		[FieldOffset(0)]
		internal ConnectionEventDataPeerAddressChanged PeerAddressChanged;

		[FieldOffset(0)]
		internal ConnectionEventDataPeerStreamStarted PeerStreamStarted;

		[FieldOffset(0)]
		internal ConnectionEventDataStreamsAvailable StreamsAvailable;

		[FieldOffset(0)]
		internal ConnectionEventPeerCertificateReceived PeerCertificateReceived;
	}

	internal struct ConnectionEvent
	{
		internal QUIC_CONNECTION_EVENT_TYPE Type;

		internal ConnectionEventDataUnion Data;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ConnectionCallbackDelegate(IntPtr connection, IntPtr context, ref ConnectionEvent connectionEvent);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ConnectionOpenDelegate(SafeMsQuicRegistrationHandle registration, ConnectionCallbackDelegate handler, IntPtr context, out SafeMsQuicConnectionHandle connection);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ConnectionCloseDelegate(IntPtr connection);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ConnectionSetConfigurationDelegate(SafeMsQuicConnectionHandle connection, SafeMsQuicConfigurationHandle configuration);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint ConnectionStartDelegate(SafeMsQuicConnectionHandle connection, SafeMsQuicConfigurationHandle configuration, QUIC_ADDRESS_FAMILY family, [MarshalAs(UnmanagedType.LPUTF8Str)] string serverName, ushort serverPort);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ConnectionShutdownDelegate(SafeMsQuicConnectionHandle connection, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, long errorCode);

	internal struct StreamEventDataReceive
	{
		internal ulong AbsoluteOffset;

		internal ulong TotalBufferLength;

		internal unsafe QuicBuffer* Buffers;

		internal uint BufferCount;

		internal QUIC_RECEIVE_FLAGS Flags;
	}

	internal struct StreamEventDataSendComplete
	{
		internal byte Canceled;

		internal IntPtr ClientContext;
	}

	internal struct StreamEventDataPeerSendAborted
	{
		internal long ErrorCode;
	}

	internal struct StreamEventDataPeerReceiveAborted
	{
		internal long ErrorCode;
	}

	internal struct StreamEventDataSendShutdownComplete
	{
		internal byte Graceful;
	}

	internal struct StreamEventDataShutdownComplete
	{
		internal byte ConnectionShutdown;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct StreamEventDataUnion
	{
		[FieldOffset(0)]
		internal StreamEventDataReceive Receive;

		[FieldOffset(0)]
		internal StreamEventDataSendComplete SendComplete;

		[FieldOffset(0)]
		internal StreamEventDataPeerSendAborted PeerSendAborted;

		[FieldOffset(0)]
		internal StreamEventDataPeerReceiveAborted PeerReceiveAborted;

		[FieldOffset(0)]
		internal StreamEventDataSendShutdownComplete SendShutdownComplete;

		[FieldOffset(0)]
		internal StreamEventDataShutdownComplete ShutdownComplete;
	}

	internal struct StreamEvent
	{
		internal QUIC_STREAM_EVENT_TYPE Type;

		internal StreamEventDataUnion Data;
	}

	internal struct SOCKADDR_IN
	{
		internal ushort sin_family;

		internal ushort sin_port;

		internal unsafe fixed byte sin_addr[4];
	}

	internal struct SOCKADDR_IN6
	{
		internal ushort sin6_family;

		internal ushort sin6_port;

		internal uint sin6_flowinfo;

		internal unsafe fixed byte sin6_addr[16];

		internal uint sin6_scope_id;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct SOCKADDR_INET
	{
		[FieldOffset(0)]
		internal SOCKADDR_IN Ipv4;

		[FieldOffset(0)]
		internal SOCKADDR_IN6 Ipv6;

		[FieldOffset(0)]
		internal ushort si_family;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint StreamCallbackDelegate(IntPtr stream, IntPtr context, ref StreamEvent streamEvent);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint StreamOpenDelegate(SafeMsQuicConnectionHandle connection, QUIC_STREAM_OPEN_FLAGS flags, StreamCallbackDelegate handler, IntPtr context, out SafeMsQuicStreamHandle stream);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint StreamStartDelegate(SafeMsQuicStreamHandle stream, QUIC_STREAM_START_FLAGS flags);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void StreamCloseDelegate(IntPtr stream);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint StreamShutdownDelegate(SafeMsQuicStreamHandle stream, QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal unsafe delegate uint StreamSendDelegate(SafeMsQuicStreamHandle stream, QuicBuffer* buffers, uint bufferCount, QUIC_SEND_FLAGS flags, IntPtr clientSendContext);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint StreamReceiveCompleteDelegate(SafeMsQuicStreamHandle stream, ulong bufferLength);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint StreamReceiveSetEnabledDelegate(SafeMsQuicStreamHandle stream, [MarshalAs(UnmanagedType.U1)] bool enabled);

	internal struct QuicBuffer
	{
		internal uint Length;

		internal unsafe byte* Buffer;
	}
}
