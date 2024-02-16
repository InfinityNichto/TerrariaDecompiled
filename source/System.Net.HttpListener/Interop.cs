using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	internal static class WebSocket
	{
		[StructLayout(LayoutKind.Explicit)]
		internal struct Buffer
		{
			[FieldOffset(0)]
			internal DataBuffer Data;

			[FieldOffset(0)]
			internal CloseBuffer CloseStatus;
		}

		internal struct Property
		{
			internal WebSocketProtocolComponent.PropertyType Type;

			internal IntPtr PropertyData;

			internal uint PropertySize;
		}

		internal struct DataBuffer
		{
			internal IntPtr BufferData;

			internal uint BufferLength;
		}

		internal struct CloseBuffer
		{
			internal IntPtr ReasonData;

			internal uint ReasonLength;

			internal ushort CloseStatus;
		}

		internal struct HttpHeader
		{
			[MarshalAs(UnmanagedType.LPStr)]
			internal string Name;

			internal uint NameLength;

			[MarshalAs(UnmanagedType.LPStr)]
			internal string Value;

			internal uint ValueLength;
		}

		[DllImport("websocket.dll")]
		internal static extern void WebSocketAbortHandle([In] SafeHandle webSocketHandle);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketBeginClientHandshake([In] SafeHandle webSocketHandle, [In] IntPtr subProtocols, [In] uint subProtocolCount, [In] IntPtr extensions, [In] uint extensionCount, [In] HttpHeader[] initialHeaders, [In] uint initialHeaderCount, out IntPtr additionalHeadersPtr, out uint additionalHeaderCount);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketBeginServerHandshake([In] SafeHandle webSocketHandle, [In] IntPtr subProtocol, [In] IntPtr extensions, [In] uint extensionCount, [In] HttpHeader[] requestHeaders, [In] uint requestHeaderCount, out IntPtr responseHeadersPtr, out uint responseHeaderCount);

		[DllImport("websocket.dll")]
		internal static extern void WebSocketCompleteAction([In] SafeHandle webSocketHandle, [In] IntPtr actionContext, [In] uint bytesTransferred);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketCreateClientHandle([In] Property[] properties, [In] uint propertyCount, out SafeWebSocketHandle webSocketHandle);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketCreateServerHandle([In] Property[] properties, [In] uint propertyCount, out SafeWebSocketHandle webSocketHandle);

		[DllImport("websocket.dll")]
		internal static extern void WebSocketDeleteHandle([In] IntPtr webSocketHandle);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketEndServerHandshake([In] SafeHandle webSocketHandle);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketGetAction([In] SafeHandle webSocketHandle, [In] WebSocketProtocolComponent.ActionQueue actionQueue, [In][Out] Buffer[] dataBuffers, [In][Out] ref uint dataBufferCount, out WebSocketProtocolComponent.Action action, out WebSocketProtocolComponent.BufferType bufferType, out IntPtr applicationContext, out IntPtr actionContext);

		[DllImport("websocket.dll")]
		internal static extern int WebSocketReceive([In] SafeHandle webSocketHandle, [In] IntPtr buffers, [In] IntPtr applicationContext);

		[DllImport("websocket.dll", EntryPoint = "WebSocketSend", ExactSpelling = true)]
		internal static extern int WebSocketSend_Raw([In] SafeHandle webSocketHandle, [In] WebSocketProtocolComponent.BufferType bufferType, [In] ref Buffer buffer, [In] IntPtr applicationContext);

		[DllImport("websocket.dll", EntryPoint = "WebSocketSend", ExactSpelling = true)]
		internal static extern int WebSocketSendWithoutBody_Raw([In] SafeHandle webSocketHandle, [In] WebSocketProtocolComponent.BufferType bufferType, [In] IntPtr buffer, [In] IntPtr applicationContext);
	}

	internal static class HttpApi
	{
		internal struct HTTP_VERSION
		{
			internal ushort MajorVersion;

			internal ushort MinorVersion;
		}

		internal enum HTTP_RESPONSE_INFO_TYPE
		{
			HttpResponseInfoTypeMultipleKnownHeaders,
			HttpResponseInfoTypeAuthenticationProperty,
			HttpResponseInfoTypeQosProperty
		}

		internal struct HTTP_RESPONSE_INFO
		{
			internal HTTP_RESPONSE_INFO_TYPE Type;

			internal uint Length;

			internal unsafe void* pInfo;
		}

		internal struct HTTP_RESPONSE_HEADERS
		{
			internal ushort UnknownHeaderCount;

			internal unsafe HTTP_UNKNOWN_HEADER* pUnknownHeaders;

			internal ushort TrailerCount;

			internal unsafe HTTP_UNKNOWN_HEADER* pTrailers;

			internal HTTP_KNOWN_HEADER KnownHeaders;

			internal HTTP_KNOWN_HEADER KnownHeaders_02;

			internal HTTP_KNOWN_HEADER KnownHeaders_03;

			internal HTTP_KNOWN_HEADER KnownHeaders_04;

			internal HTTP_KNOWN_HEADER KnownHeaders_05;

			internal HTTP_KNOWN_HEADER KnownHeaders_06;

			internal HTTP_KNOWN_HEADER KnownHeaders_07;

			internal HTTP_KNOWN_HEADER KnownHeaders_08;

			internal HTTP_KNOWN_HEADER KnownHeaders_09;

			internal HTTP_KNOWN_HEADER KnownHeaders_10;

			internal HTTP_KNOWN_HEADER KnownHeaders_11;

			internal HTTP_KNOWN_HEADER KnownHeaders_12;

			internal HTTP_KNOWN_HEADER KnownHeaders_13;

			internal HTTP_KNOWN_HEADER KnownHeaders_14;

			internal HTTP_KNOWN_HEADER KnownHeaders_15;

			internal HTTP_KNOWN_HEADER KnownHeaders_16;

			internal HTTP_KNOWN_HEADER KnownHeaders_17;

			internal HTTP_KNOWN_HEADER KnownHeaders_18;

			internal HTTP_KNOWN_HEADER KnownHeaders_19;

			internal HTTP_KNOWN_HEADER KnownHeaders_20;

			internal HTTP_KNOWN_HEADER KnownHeaders_21;

			internal HTTP_KNOWN_HEADER KnownHeaders_22;

			internal HTTP_KNOWN_HEADER KnownHeaders_23;

			internal HTTP_KNOWN_HEADER KnownHeaders_24;

			internal HTTP_KNOWN_HEADER KnownHeaders_25;

			internal HTTP_KNOWN_HEADER KnownHeaders_26;

			internal HTTP_KNOWN_HEADER KnownHeaders_27;

			internal HTTP_KNOWN_HEADER KnownHeaders_28;

			internal HTTP_KNOWN_HEADER KnownHeaders_29;

			internal HTTP_KNOWN_HEADER KnownHeaders_30;
		}

		internal struct HTTP_KNOWN_HEADER
		{
			internal ushort RawValueLength;

			internal unsafe sbyte* pRawValue;
		}

		internal struct HTTP_UNKNOWN_HEADER
		{
			internal ushort NameLength;

			internal ushort RawValueLength;

			internal unsafe sbyte* pName;

			internal unsafe sbyte* pRawValue;
		}

		internal enum HTTP_DATA_CHUNK_TYPE
		{
			HttpDataChunkFromMemory,
			HttpDataChunkFromFileHandle,
			HttpDataChunkFromFragmentCache,
			HttpDataChunkMaximum
		}

		[StructLayout(LayoutKind.Sequential, Size = 32)]
		internal struct HTTP_DATA_CHUNK
		{
			internal HTTP_DATA_CHUNK_TYPE DataChunkType;

			internal uint p0;

			internal unsafe byte* pBuffer;

			internal uint BufferLength;
		}

		internal struct HTTP_RESPONSE
		{
			internal uint Flags;

			internal HTTP_VERSION Version;

			internal ushort StatusCode;

			internal ushort ReasonLength;

			internal unsafe sbyte* pReason;

			internal HTTP_RESPONSE_HEADERS Headers;

			internal ushort EntityChunkCount;

			internal unsafe HTTP_DATA_CHUNK* pEntityChunks;

			internal ushort ResponseInfoCount;

			internal unsafe HTTP_RESPONSE_INFO* pResponseInfo;
		}

		internal enum HTTP_VERB
		{
			HttpVerbUnparsed,
			HttpVerbUnknown,
			HttpVerbInvalid,
			HttpVerbOPTIONS,
			HttpVerbGET,
			HttpVerbHEAD,
			HttpVerbPOST,
			HttpVerbPUT,
			HttpVerbDELETE,
			HttpVerbTRACE,
			HttpVerbCONNECT,
			HttpVerbTRACK,
			HttpVerbMOVE,
			HttpVerbCOPY,
			HttpVerbPROPFIND,
			HttpVerbPROPPATCH,
			HttpVerbMKCOL,
			HttpVerbLOCK,
			HttpVerbUNLOCK,
			HttpVerbSEARCH,
			HttpVerbMaximum
		}

		internal struct SOCKADDR
		{
			internal ushort sa_family;

			internal byte sa_data;

			internal byte sa_data_02;

			internal byte sa_data_03;

			internal byte sa_data_04;

			internal byte sa_data_05;

			internal byte sa_data_06;

			internal byte sa_data_07;

			internal byte sa_data_08;

			internal byte sa_data_09;

			internal byte sa_data_10;

			internal byte sa_data_11;

			internal byte sa_data_12;

			internal byte sa_data_13;

			internal byte sa_data_14;
		}

		internal struct HTTP_TRANSPORT_ADDRESS
		{
			internal unsafe SOCKADDR* pRemoteAddress;

			internal unsafe SOCKADDR* pLocalAddress;
		}

		internal struct HTTP_REQUEST_HEADERS
		{
			internal ushort UnknownHeaderCount;

			internal unsafe HTTP_UNKNOWN_HEADER* pUnknownHeaders;

			internal ushort TrailerCount;

			internal unsafe HTTP_UNKNOWN_HEADER* pTrailers;

			internal HTTP_KNOWN_HEADER KnownHeaders;

			internal HTTP_KNOWN_HEADER KnownHeaders_02;

			internal HTTP_KNOWN_HEADER KnownHeaders_03;

			internal HTTP_KNOWN_HEADER KnownHeaders_04;

			internal HTTP_KNOWN_HEADER KnownHeaders_05;

			internal HTTP_KNOWN_HEADER KnownHeaders_06;

			internal HTTP_KNOWN_HEADER KnownHeaders_07;

			internal HTTP_KNOWN_HEADER KnownHeaders_08;

			internal HTTP_KNOWN_HEADER KnownHeaders_09;

			internal HTTP_KNOWN_HEADER KnownHeaders_10;

			internal HTTP_KNOWN_HEADER KnownHeaders_11;

			internal HTTP_KNOWN_HEADER KnownHeaders_12;

			internal HTTP_KNOWN_HEADER KnownHeaders_13;

			internal HTTP_KNOWN_HEADER KnownHeaders_14;

			internal HTTP_KNOWN_HEADER KnownHeaders_15;

			internal HTTP_KNOWN_HEADER KnownHeaders_16;

			internal HTTP_KNOWN_HEADER KnownHeaders_17;

			internal HTTP_KNOWN_HEADER KnownHeaders_18;

			internal HTTP_KNOWN_HEADER KnownHeaders_19;

			internal HTTP_KNOWN_HEADER KnownHeaders_20;

			internal HTTP_KNOWN_HEADER KnownHeaders_21;

			internal HTTP_KNOWN_HEADER KnownHeaders_22;

			internal HTTP_KNOWN_HEADER KnownHeaders_23;

			internal HTTP_KNOWN_HEADER KnownHeaders_24;

			internal HTTP_KNOWN_HEADER KnownHeaders_25;

			internal HTTP_KNOWN_HEADER KnownHeaders_26;

			internal HTTP_KNOWN_HEADER KnownHeaders_27;

			internal HTTP_KNOWN_HEADER KnownHeaders_28;

			internal HTTP_KNOWN_HEADER KnownHeaders_29;

			internal HTTP_KNOWN_HEADER KnownHeaders_30;

			internal HTTP_KNOWN_HEADER KnownHeaders_31;

			internal HTTP_KNOWN_HEADER KnownHeaders_32;

			internal HTTP_KNOWN_HEADER KnownHeaders_33;

			internal HTTP_KNOWN_HEADER KnownHeaders_34;

			internal HTTP_KNOWN_HEADER KnownHeaders_35;

			internal HTTP_KNOWN_HEADER KnownHeaders_36;

			internal HTTP_KNOWN_HEADER KnownHeaders_37;

			internal HTTP_KNOWN_HEADER KnownHeaders_38;

			internal HTTP_KNOWN_HEADER KnownHeaders_39;

			internal HTTP_KNOWN_HEADER KnownHeaders_40;

			internal HTTP_KNOWN_HEADER KnownHeaders_41;
		}

		internal struct HTTP_SSL_CLIENT_CERT_INFO
		{
			internal uint CertFlags;

			internal uint CertEncodedSize;

			internal unsafe byte* pCertEncoded;

			internal unsafe void* Token;

			internal byte CertDeniedByMapper;
		}

		internal struct HTTP_SSL_INFO
		{
			internal ushort ServerCertKeySize;

			internal ushort ConnectionKeySize;

			internal uint ServerCertIssuerSize;

			internal uint ServerCertSubjectSize;

			internal unsafe sbyte* pServerCertIssuer;

			internal unsafe sbyte* pServerCertSubject;

			internal unsafe HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo;

			internal uint SslClientCertNegotiated;
		}

		internal struct HTTP_REQUEST
		{
			internal uint Flags;

			internal ulong ConnectionId;

			internal ulong RequestId;

			internal ulong UrlContext;

			internal HTTP_VERSION Version;

			internal HTTP_VERB Verb;

			internal ushort UnknownVerbLength;

			internal ushort RawUrlLength;

			internal unsafe sbyte* pUnknownVerb;

			internal unsafe sbyte* pRawUrl;

			internal HTTP_COOKED_URL CookedUrl;

			internal HTTP_TRANSPORT_ADDRESS Address;

			internal HTTP_REQUEST_HEADERS Headers;

			internal ulong BytesReceived;

			internal ushort EntityChunkCount;

			internal unsafe HTTP_DATA_CHUNK* pEntityChunks;

			internal ulong RawConnectionId;

			internal unsafe HTTP_SSL_INFO* pSslInfo;
		}

		internal struct HTTP_COOKED_URL
		{
			internal ushort FullUrlLength;

			internal ushort HostLength;

			internal ushort AbsPathLength;

			internal ushort QueryStringLength;

			internal unsafe ushort* pFullUrl;

			internal unsafe ushort* pHost;

			internal unsafe ushort* pAbsPath;

			internal unsafe ushort* pQueryString;
		}

		internal struct HTTP_REQUEST_CHANNEL_BIND_STATUS
		{
			internal IntPtr ServiceName;

			internal IntPtr ChannelToken;

			internal uint ChannelTokenSize;

			internal uint Flags;
		}

		internal enum HTTP_SERVER_PROPERTY
		{
			HttpServerAuthenticationProperty,
			HttpServerLoggingProperty,
			HttpServerQosProperty,
			HttpServerTimeoutsProperty,
			HttpServerQueueLengthProperty,
			HttpServerStateProperty,
			HttpServer503VerbosityProperty,
			HttpServerBindingProperty,
			HttpServerExtendedAuthenticationProperty,
			HttpServerListenEndpointProperty,
			HttpServerChannelBindProperty,
			HttpServerProtectionLevelProperty
		}

		[Flags]
		internal enum HTTP_FLAGS : uint
		{
			NONE = 0u,
			HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY = 1u,
			HTTP_RECEIVE_SECURE_CHANNEL_TOKEN = 1u,
			HTTP_SEND_RESPONSE_FLAG_DISCONNECT = 1u,
			HTTP_SEND_RESPONSE_FLAG_MORE_DATA = 2u,
			HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA = 4u,
			HTTP_SEND_RESPONSE_FLAG_RAW_HEADER = 4u,
			HTTP_SEND_REQUEST_FLAG_MORE_DATA = 1u,
			HTTP_PROPERTY_FLAG_PRESENT = 1u,
			HTTP_INITIALIZE_SERVER = 1u,
			HTTP_INITIALIZE_CBT = 4u,
			HTTP_SEND_RESPONSE_FLAG_OPAQUE = 0x40u
		}

		internal static class HTTP_REQUEST_HEADER_ID
		{
			private static readonly string[] s_strings = new string[41]
			{
				"Cache-Control", "Connection", "Date", "Keep-Alive", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning",
				"Allow", "Content-Length", "Content-Type", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5", "Content-Range", "Expires", "Last-Modified",
				"Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Authorization", "Cookie", "Expect", "From", "Host", "If-Match",
				"If-Modified-Since", "If-None-Match", "If-Range", "If-Unmodified-Since", "Max-Forwards", "Proxy-Authorization", "Referer", "Range", "Te", "Translate",
				"User-Agent"
			};

			internal static string ToString(int position)
			{
				return s_strings[position];
			}
		}

		internal enum HTTP_TIMEOUT_TYPE
		{
			EntityBody,
			DrainEntityBody,
			RequestQueue,
			IdleConnection,
			HeaderWait,
			MinSendRate
		}

		internal struct HTTP_TIMEOUT_LIMIT_INFO
		{
			internal HTTP_FLAGS Flags;

			internal ushort EntityBody;

			internal ushort DrainEntityBody;

			internal ushort RequestQueue;

			internal ushort IdleConnection;

			internal ushort HeaderWait;

			internal uint MinSendRate;
		}

		internal struct HTTPAPI_VERSION
		{
			internal ushort HttpApiMajorVersion;

			internal ushort HttpApiMinorVersion;
		}

		internal struct HTTP_BINDING_INFO
		{
			internal HTTP_FLAGS Flags;

			internal IntPtr RequestQueueHandle;
		}

		internal sealed class SafeLocalFreeChannelBinding : ChannelBinding
		{
			private int _size;

			public override int Size => _size;

			public static SafeLocalFreeChannelBinding LocalAlloc(int cb)
			{
				SafeLocalFreeChannelBinding safeLocalFreeChannelBinding = new SafeLocalFreeChannelBinding();
				safeLocalFreeChannelBinding.SetHandle(Marshal.AllocHGlobal(cb));
				safeLocalFreeChannelBinding._size = cb;
				return safeLocalFreeChannelBinding;
			}

			protected override bool ReleaseHandle()
			{
				Marshal.FreeHGlobal(handle);
				return true;
			}
		}

		internal static class HTTP_RESPONSE_HEADER_ID
		{
			private static readonly string[] s_strings = new string[30]
			{
				"Cache-Control", "Connection", "Date", "Keep-Alive", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning",
				"Allow", "Content-Length", "Content-Type", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5", "Content-Range", "Expires", "Last-Modified",
				"Accept-Ranges", "Age", "ETag", "Location", "Proxy-Authenticate", "Retry-After", "Server", "Set-Cookie", "Vary", "WWW-Authenticate"
			};

			private static readonly Dictionary<string, int> s_hashtable = CreateTable();

			private static Dictionary<string, int> CreateTable()
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>(30);
				for (int i = 0; i < 30; i++)
				{
					dictionary.Add(s_strings[i], i);
				}
				return dictionary;
			}

			internal static int IndexOfKnownHeader(string headerName)
			{
				if (!s_hashtable.TryGetValue(headerName, out var value))
				{
					return -1;
				}
				return value;
			}
		}

		internal static readonly HTTPAPI_VERSION s_version = new HTTPAPI_VERSION
		{
			HttpApiMajorVersion = 2,
			HttpApiMinorVersion = 0
		};

		internal static readonly bool s_supported = InitHttpApi(s_version);

		internal static IPEndPoint s_any = new IPEndPoint(IPAddress.Any, 0);

		internal static IPEndPoint s_ipv6Any = new IPEndPoint(IPAddress.IPv6Any, 0);

		internal static readonly string[] HttpVerbs = new string[20]
		{
			null, "Unknown", "Invalid", "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE",
			"CONNECT", "TRACK", "MOVE", "COPY", "PROPFIND", "PROPPATCH", "MKCOL", "LOCK", "UNLOCK", "SEARCH"
		};

		private static bool InitHttpApi(HTTPAPI_VERSION version)
		{
			uint num = HttpInitialize(version, 1u, IntPtr.Zero);
			return num == 0;
		}

		[DllImport("httpapi.dll", SetLastError = true)]
		internal static extern uint HttpInitialize(HTTPAPI_VERSION version, uint flags, IntPtr pReserved);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal static extern uint HttpSetUrlGroupProperty(ulong urlGroupId, HTTP_SERVER_PROPERTY serverProperty, IntPtr pPropertyInfo, uint propertyInfoLength);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpCreateServerSession(HTTPAPI_VERSION version, ulong* serverSessionId, uint reserved);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpCreateUrlGroup(ulong serverSessionId, ulong* urlGroupId, uint reserved);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal static extern uint HttpCloseUrlGroup(ulong urlGroupId);

		[DllImport("httpapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern uint HttpCreateRequestQueue(HTTPAPI_VERSION version, string pName, Kernel32.SECURITY_ATTRIBUTES* pSecurityAttributes, uint flags, out HttpRequestQueueV2Handle pReqQueueHandle);

		[DllImport("httpapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern uint HttpAddUrlToUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, ulong context, uint pReserved);

		[DllImport("httpapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern uint HttpRemoveUrlFromUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, uint flags);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpReceiveHttpRequest(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_REQUEST* pRequestBuffer, uint requestBufferLength, uint* pBytesReturned, NativeOverlapped* pOverlapped);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpSendHttpResponse(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_RESPONSE* pHttpResponse, void* pCachePolicy, uint* pBytesSent, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle pRequestBuffer, uint requestBufferLength, NativeOverlapped* pOverlapped, void* pLogData);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpWaitForDisconnect(SafeHandle requestQueueHandle, ulong connectionId, NativeOverlapped* pOverlapped);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpReceiveRequestEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, void* pEntityBuffer, uint entityBufferLength, out uint bytesReturned, NativeOverlapped* pOverlapped);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpSendResponseEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, ushort entityChunkCount, HTTP_DATA_CHUNK* pEntityChunks, uint* pBytesSent, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle pRequestBuffer, uint requestBufferLength, NativeOverlapped* pOverlapped, void* pLogData);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal static extern uint HttpCloseRequestQueue(IntPtr pReqQueueHandle);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal static extern uint HttpCancelHttpRequest(SafeHandle requestQueueHandle, ulong requestId, IntPtr pOverlapped);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal static extern uint HttpCloseServerSession(ulong serverSessionId);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, HTTP_SSL_CLIENT_CERT_INFO* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, NativeOverlapped* pOverlapped);

		[DllImport("httpapi.dll", SetLastError = true)]
		internal unsafe static extern uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, byte* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, NativeOverlapped* pOverlapped);

		private unsafe static string GetKnownHeader(HTTP_REQUEST* request, long fixup, int headerIndex)
		{
			string result = null;
			HTTP_KNOWN_HEADER* ptr = &request->Headers.KnownHeaders + headerIndex;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"HttpApi::GetKnownHeader() pKnownHeader:0x{(IntPtr)ptr}", "GetKnownHeader");
				System.Net.NetEventSource.Info(null, $"HttpApi::GetKnownHeader() pRawValue:0x{(IntPtr)ptr->pRawValue} RawValueLength:{ptr->RawValueLength}", "GetKnownHeader");
			}
			if (ptr->pRawValue != null)
			{
				result = new string(ptr->pRawValue + fixup, 0, ptr->RawValueLength);
			}
			return result;
		}

		internal unsafe static string GetKnownHeader(HTTP_REQUEST* request, int headerIndex)
		{
			return GetKnownHeader(request, 0L, headerIndex);
		}

		private unsafe static string GetVerb(HTTP_REQUEST* request, long fixup)
		{
			string result = null;
			if (request->Verb > HTTP_VERB.HttpVerbUnknown && request->Verb < HTTP_VERB.HttpVerbMaximum)
			{
				result = HttpVerbs[(int)request->Verb];
			}
			else if (request->Verb == HTTP_VERB.HttpVerbUnknown && request->pUnknownVerb != null)
			{
				result = new string(request->pUnknownVerb + fixup, 0, request->UnknownVerbLength);
			}
			return result;
		}

		internal unsafe static string GetVerb(IntPtr memoryBlob, IntPtr originalAddress)
		{
			return GetVerb((HTTP_REQUEST*)memoryBlob.ToPointer(), (byte*)(void*)memoryBlob - (byte*)(void*)originalAddress);
		}

		internal unsafe static WebHeaderCollection GetHeaders(IntPtr memoryBlob, IntPtr originalAddress)
		{
			WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
			byte* ptr = (byte*)(void*)memoryBlob;
			HTTP_REQUEST* ptr2 = (HTTP_REQUEST*)ptr;
			long num = ptr - (byte*)(void*)originalAddress;
			if (ptr2->Headers.UnknownHeaderCount != 0)
			{
				HTTP_UNKNOWN_HEADER* ptr3 = (HTTP_UNKNOWN_HEADER*)(num + (byte*)ptr2->Headers.pUnknownHeaders);
				for (int i = 0; i < ptr2->Headers.UnknownHeaderCount; i++)
				{
					if (ptr3->pName != null && ptr3->NameLength > 0)
					{
						string name = new string(ptr3->pName + num, 0, ptr3->NameLength);
						string value = ((ptr3->pRawValue == null || ptr3->RawValueLength <= 0) ? string.Empty : new string(ptr3->pRawValue + num, 0, ptr3->RawValueLength));
						webHeaderCollection.Add(name, value);
					}
					ptr3++;
				}
			}
			HTTP_KNOWN_HEADER* ptr4 = &ptr2->Headers.KnownHeaders;
			for (int i = 0; i < 41; i++)
			{
				if (ptr4->pRawValue != null)
				{
					string value2 = new string(ptr4->pRawValue + num, 0, ptr4->RawValueLength);
					webHeaderCollection.Add(HTTP_REQUEST_HEADER_ID.ToString(i), value2);
				}
				ptr4++;
			}
			return webHeaderCollection;
		}

		internal unsafe static uint GetChunks(IntPtr memoryBlob, IntPtr originalAddress, ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size)
		{
			uint num = 0u;
			byte* ptr = (byte*)(void*)memoryBlob;
			HTTP_REQUEST* ptr2 = (HTTP_REQUEST*)ptr;
			long num2 = ptr - (byte*)(void*)originalAddress;
			if (ptr2->EntityChunkCount > 0 && dataChunkIndex < ptr2->EntityChunkCount && dataChunkIndex != -1)
			{
				HTTP_DATA_CHUNK* ptr3 = (HTTP_DATA_CHUNK*)(num2 + (byte*)(ptr2->pEntityChunks + dataChunkIndex));
				fixed (byte* ptr4 = buffer)
				{
					byte* ptr5 = ptr4 + offset;
					while (dataChunkIndex < ptr2->EntityChunkCount && num < size)
					{
						if (dataChunkOffset >= ptr3->BufferLength)
						{
							dataChunkOffset = 0u;
							dataChunkIndex++;
							ptr3++;
							continue;
						}
						byte* ptr6 = ptr3->pBuffer + dataChunkOffset + num2;
						uint num3 = ptr3->BufferLength - dataChunkOffset;
						if (num3 > (uint)size)
						{
							num3 = (uint)size;
						}
						for (uint num4 = 0u; num4 < num3; num4++)
						{
							*(ptr5++) = *(ptr6++);
						}
						num += num3;
						dataChunkOffset += num3;
					}
				}
			}
			if (dataChunkIndex == ptr2->EntityChunkCount)
			{
				dataChunkIndex = -1;
			}
			return num;
		}

		internal unsafe static HTTP_VERB GetKnownVerb(IntPtr memoryBlob, IntPtr originalAddress)
		{
			HTTP_VERB result = HTTP_VERB.HttpVerbUnknown;
			HTTP_REQUEST* ptr = (HTTP_REQUEST*)memoryBlob.ToPointer();
			if (ptr->Verb > HTTP_VERB.HttpVerbUnparsed && ptr->Verb < HTTP_VERB.HttpVerbMaximum)
			{
				result = ptr->Verb;
			}
			return result;
		}

		internal unsafe static IPEndPoint GetRemoteEndPoint(IntPtr memoryBlob, IntPtr originalAddress)
		{
			SocketAddress v4address = new SocketAddress(AddressFamily.InterNetwork, 16);
			SocketAddress v6address = new SocketAddress(AddressFamily.InterNetworkV6, 28);
			byte* ptr = (byte*)(void*)memoryBlob;
			HTTP_REQUEST* ptr2 = (HTTP_REQUEST*)ptr;
			IntPtr address = ((ptr2->Address.pRemoteAddress != null) ? ((IntPtr)(ptr - (byte*)(void*)originalAddress + (byte*)ptr2->Address.pRemoteAddress)) : IntPtr.Zero);
			CopyOutAddress(address, ref v4address, ref v6address);
			IPEndPoint result = null;
			if (v4address != null)
			{
				result = new IPEndPoint(IPAddress.Any, 0).Create(v4address) as IPEndPoint;
			}
			else if (v6address != null)
			{
				result = new IPEndPoint(IPAddress.IPv6Any, 0).Create(v6address) as IPEndPoint;
			}
			return result;
		}

		internal unsafe static IPEndPoint GetLocalEndPoint(IntPtr memoryBlob, IntPtr originalAddress)
		{
			SocketAddress v4address = new SocketAddress(AddressFamily.InterNetwork, 16);
			SocketAddress v6address = new SocketAddress(AddressFamily.InterNetworkV6, 28);
			byte* ptr = (byte*)(void*)memoryBlob;
			HTTP_REQUEST* ptr2 = (HTTP_REQUEST*)ptr;
			IntPtr address = ((ptr2->Address.pLocalAddress != null) ? ((IntPtr)(ptr - (byte*)(void*)originalAddress + (byte*)ptr2->Address.pLocalAddress)) : IntPtr.Zero);
			CopyOutAddress(address, ref v4address, ref v6address);
			IPEndPoint result = null;
			if (v4address != null)
			{
				result = s_any.Create(v4address) as IPEndPoint;
			}
			else if (v6address != null)
			{
				result = s_ipv6Any.Create(v6address) as IPEndPoint;
			}
			return result;
		}

		private unsafe static void CopyOutAddress(IntPtr address, ref SocketAddress v4address, ref SocketAddress v6address)
		{
			if (address != IntPtr.Zero)
			{
				switch (*(ushort*)(void*)address)
				{
				case 2:
				{
					v6address = null;
					for (int j = 2; j < 16; j++)
					{
						v4address[j] = ((byte*)(void*)address)[j];
					}
					return;
				}
				case 23:
				{
					v4address = null;
					for (int i = 2; i < 28; i++)
					{
						v6address[i] = ((byte*)(void*)address)[i];
					}
					return;
				}
				}
			}
			v4address = null;
			v6address = null;
		}
	}

	internal static class Kernel32
	{
		[Flags]
		internal enum FileCompletionNotificationModes : byte
		{
			None = 0,
			SkipCompletionPortOnSuccess = 1,
			SkipSetEventOnHandle = 2
		}

		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;

			internal IntPtr lpSecurityDescriptor;

			internal BOOL bInheritHandle;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetFileCompletionNotificationModes(SafeHandle handle, FileCompletionNotificationModes flags);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW", ExactSpelling = true, SetLastError = true)]
		internal static extern IntPtr LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);
	}

	internal static class Crypt32
	{
		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);
	}

	internal enum SECURITY_STATUS
	{
		OK = 0,
		ContinueNeeded = 590610,
		CompleteNeeded = 590611,
		CompAndContinue = 590612,
		ContextExpired = 590615,
		CredentialsNeeded = 590624,
		Renegotiate = 590625,
		OutOfMemory = -2146893056,
		InvalidHandle = -2146893055,
		Unsupported = -2146893054,
		TargetUnknown = -2146893053,
		InternalError = -2146893052,
		PackageNotFound = -2146893051,
		NotOwner = -2146893050,
		CannotInstall = -2146893049,
		InvalidToken = -2146893048,
		CannotPack = -2146893047,
		QopNotSupported = -2146893046,
		NoImpersonation = -2146893045,
		LogonDenied = -2146893044,
		UnknownCredentials = -2146893043,
		NoCredentials = -2146893042,
		MessageAltered = -2146893041,
		OutOfSequence = -2146893040,
		NoAuthenticatingAuthority = -2146893039,
		IncompleteMessage = -2146893032,
		IncompleteCredentials = -2146893024,
		BufferNotEnough = -2146893023,
		WrongPrincipal = -2146893022,
		TimeSkew = -2146893020,
		UntrustedRoot = -2146893019,
		IllegalMessage = -2146893018,
		CertUnknown = -2146893017,
		CertExpired = -2146893016,
		DecryptFailure = -2146893008,
		AlgorithmMismatch = -2146893007,
		SecurityQosFailed = -2146893006,
		SmartcardLogonRequired = -2146892994,
		UnsupportedPreauth = -2146892989,
		BadBinding = -2146892986,
		DowngradeDetected = -2146892976,
		ApplicationProtocolMismatch = -2146892953,
		NoRenegotiation = 590688
	}

	internal static class SspiCli
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct CredHandle
		{
			private IntPtr dwLower;

			private IntPtr dwUpper;

			public bool IsZero
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					if (dwLower == IntPtr.Zero)
					{
						return dwUpper == IntPtr.Zero;
					}
					return false;
				}
			}

			internal void SetToInvalid()
			{
				dwLower = IntPtr.Zero;
				dwUpper = IntPtr.Zero;
			}

			public override string ToString()
			{
				return dwLower.ToString("x") + ":" + dwUpper.ToString("x");
			}
		}

		internal enum ContextAttribute
		{
			SECPKG_ATTR_SIZES = 0,
			SECPKG_ATTR_NAMES = 1,
			SECPKG_ATTR_LIFESPAN = 2,
			SECPKG_ATTR_DCE_INFO = 3,
			SECPKG_ATTR_STREAM_SIZES = 4,
			SECPKG_ATTR_AUTHORITY = 6,
			SECPKG_ATTR_PACKAGE_INFO = 10,
			SECPKG_ATTR_NEGOTIATION_INFO = 12,
			SECPKG_ATTR_UNIQUE_BINDINGS = 25,
			SECPKG_ATTR_ENDPOINT_BINDINGS = 26,
			SECPKG_ATTR_CLIENT_SPECIFIED_TARGET = 27,
			SECPKG_ATTR_APPLICATION_PROTOCOL = 35,
			SECPKG_ATTR_REMOTE_CERT_CONTEXT = 83,
			SECPKG_ATTR_LOCAL_CERT_CONTEXT = 84,
			SECPKG_ATTR_ROOT_STORE = 85,
			SECPKG_ATTR_ISSUER_LIST_EX = 89,
			SECPKG_ATTR_CLIENT_CERT_POLICY = 96,
			SECPKG_ATTR_CONNECTION_INFO = 90,
			SECPKG_ATTR_CIPHER_INFO = 100,
			SECPKG_ATTR_UI_INFO = 104
		}

		[Flags]
		internal enum ContextFlags
		{
			Zero = 0,
			Delegate = 1,
			MutualAuth = 2,
			ReplayDetect = 4,
			SequenceDetect = 8,
			Confidentiality = 0x10,
			UseSessionKey = 0x20,
			AllocateMemory = 0x100,
			Connection = 0x800,
			InitExtendedError = 0x4000,
			AcceptExtendedError = 0x8000,
			InitStream = 0x8000,
			AcceptStream = 0x10000,
			InitIntegrity = 0x10000,
			AcceptIntegrity = 0x20000,
			InitManualCredValidation = 0x80000,
			InitUseSuppliedCreds = 0x80,
			InitIdentify = 0x20000,
			AcceptIdentify = 0x80000,
			ProxyBindings = 0x4000000,
			AllowMissingBindings = 0x10000000,
			UnverifiedTargetName = 0x20000000
		}

		internal enum Endianness
		{
			SECURITY_NETWORK_DREP = 0,
			SECURITY_NATIVE_DREP = 0x10
		}

		internal enum CredentialUse
		{
			SECPKG_CRED_INBOUND = 1,
			SECPKG_CRED_OUTBOUND,
			SECPKG_CRED_BOTH
		}

		internal struct SecBuffer
		{
			public int cbBuffer;

			public System.Net.Security.SecurityBufferType BufferType;

			public IntPtr pvBuffer;

			public unsafe static readonly int Size = sizeof(SecBuffer);
		}

		internal struct SecBufferDesc
		{
			public readonly int ulVersion;

			public readonly int cBuffers;

			public unsafe void* pBuffers;

			public unsafe SecBufferDesc(int count)
			{
				ulVersion = 0;
				cBuffers = count;
				pBuffers = null;
			}
		}

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int QuerySecurityContextToken(ref CredHandle phContext, out System.Net.Security.SecurityContextTokenHandle handle);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int FreeContextBuffer([In] IntPtr contextBuffer);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int FreeCredentialsHandle(ref CredHandle handlePtr);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int DeleteSecurityContext(ref CredHandle handlePtr);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcceptSecurityContext(ref CredHandle credentialHandle, [In] void* inContextPtr, [In] SecBufferDesc* inputBuffer, [In] ContextFlags inFlags, [In] Endianness endianness, ref CredHandle outContextPtr, [In][Out] ref SecBufferDesc outputBuffer, [In][Out] ref ContextFlags attributes, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int QueryContextAttributesW(ref CredHandle contextHandle, [In] ContextAttribute attribute, [In] void* buffer);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int EnumerateSecurityPackagesW(out int pkgnum, out System.Net.Security.SafeFreeContextBuffer_SECURITY handle);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] IntPtr zero, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] System.Net.Security.SafeSspiAuthDataHandle authdata, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int InitializeSecurityContextW(ref CredHandle credentialHandle, [In] void* inContextPtr, [In] byte* targetName, [In] ContextFlags inFlags, [In] int reservedI, [In] Endianness endianness, [In] SecBufferDesc* inputBuffer, [In] int reservedII, ref CredHandle outContextPtr, [In][Out] ref SecBufferDesc outputBuffer, [In][Out] ref ContextFlags attributes, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int CompleteAuthToken([In] void* inContextPtr, [In][Out] ref SecBufferDesc inputBuffers);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SspiFreeAuthIdentity([In] IntPtr authData);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SspiEncodeStringsAsAuthIdentity([In] string userName, [In] string domainName, [In] string password, out System.Net.Security.SafeSspiAuthDataHandle authData);
	}
}
