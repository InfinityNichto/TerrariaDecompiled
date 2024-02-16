using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Net;

public sealed class HttpListenerResponse : IDisposable
{
	private enum ResponseState
	{
		Created,
		ComputedHeaders,
		SentHeaders,
		Closed
	}

	private BoundaryType _boundaryType = BoundaryType.None;

	private CookieCollection _cookies;

	private readonly HttpListenerContext _httpContext;

	private bool _keepAlive = true;

	private HttpResponseStream _responseStream;

	private string _statusDescription;

	private WebHeaderCollection _webHeaders = new WebHeaderCollection();

	private static readonly int[] s_noResponseBody = new int[5] { 100, 101, 204, 205, 304 };

	private ResponseState _responseState;

	private long _contentLength;

	private global::Interop.HttpApi.HTTP_RESPONSE _nativeResponse;

	public WebHeaderCollection Headers
	{
		get
		{
			return _webHeaders;
		}
		set
		{
			_webHeaders = new WebHeaderCollection();
			string[] allKeys = value.AllKeys;
			foreach (string name in allKeys)
			{
				_webHeaders.Add(name, value[name]);
			}
		}
	}

	public Encoding? ContentEncoding { get; set; }

	public string? ContentType
	{
		get
		{
			return Headers["Content-Type"];
		}
		set
		{
			CheckDisposed();
			if (string.IsNullOrEmpty(value))
			{
				Headers.Remove("Content-Type");
			}
			else
			{
				Headers.Set("Content-Type", value);
			}
		}
	}

	private HttpListenerContext HttpListenerContext => _httpContext;

	private HttpListenerRequest HttpListenerRequest => HttpListenerContext.Request;

	internal EntitySendFormat EntitySendFormat
	{
		get
		{
			return (EntitySendFormat)_boundaryType;
		}
		set
		{
			CheckDisposed();
			CheckSentHeaders();
			if (value == EntitySendFormat.Chunked && HttpListenerRequest.ProtocolVersion.Minor == 0)
			{
				throw new ProtocolViolationException(System.SR.net_nochunkuploadonhttp10);
			}
			_boundaryType = (BoundaryType)value;
			if (value != 0)
			{
				_contentLength = -1L;
			}
		}
	}

	public bool SendChunked
	{
		get
		{
			return EntitySendFormat == EntitySendFormat.Chunked;
		}
		set
		{
			EntitySendFormat = (value ? EntitySendFormat.Chunked : EntitySendFormat.ContentLength);
		}
	}

	public long ContentLength64
	{
		get
		{
			return _contentLength;
		}
		set
		{
			CheckDisposed();
			CheckSentHeaders();
			if (value >= 0)
			{
				_contentLength = value;
				_boundaryType = BoundaryType.ContentLength;
				return;
			}
			throw new ArgumentOutOfRangeException("value", System.SR.net_clsmall);
		}
	}

	public CookieCollection Cookies
	{
		get
		{
			return _cookies ?? (_cookies = new CookieCollection());
		}
		set
		{
			_cookies = value;
		}
	}

	public bool KeepAlive
	{
		get
		{
			return _keepAlive;
		}
		set
		{
			CheckDisposed();
			_keepAlive = value;
		}
	}

	public Stream OutputStream
	{
		get
		{
			CheckDisposed();
			EnsureResponseStream();
			return _responseStream;
		}
	}

	public string? RedirectLocation
	{
		get
		{
			return Headers[HttpResponseHeader.Location];
		}
		set
		{
			CheckDisposed();
			if (string.IsNullOrEmpty(value))
			{
				Headers.Remove("Location");
			}
			else
			{
				Headers.Set("Location", value);
			}
		}
	}

	public string StatusDescription
	{
		get
		{
			if (_statusDescription == null)
			{
				_statusDescription = HttpStatusDescription.Get(StatusCode);
			}
			if (_statusDescription == null)
			{
				_statusDescription = string.Empty;
			}
			return _statusDescription;
		}
		set
		{
			CheckDisposed();
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			for (int i = 0; i < value.Length; i++)
			{
				char c = (char)(0xFFu & value[i]);
				if ((c <= '\u001f' && c != '\t') || c == '\u007f')
				{
					throw new ArgumentException(System.SR.net_WebHeaderInvalidControlChars, "value");
				}
			}
			_statusDescription = value;
		}
	}

	public int StatusCode
	{
		get
		{
			return _nativeResponse.StatusCode;
		}
		set
		{
			CheckDisposed();
			if (value < 100 || value > 999)
			{
				throw new ProtocolViolationException(System.SR.net_invalidstatus);
			}
			_nativeResponse.StatusCode = (ushort)value;
		}
	}

	public Version ProtocolVersion
	{
		get
		{
			return new Version(_nativeResponse.Version.MajorVersion, _nativeResponse.Version.MinorVersion);
		}
		set
		{
			CheckDisposed();
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
			{
				throw new ArgumentException(System.SR.net_wrongversion, "value");
			}
			_nativeResponse.Version.MajorVersion = (ushort)value.Major;
			_nativeResponse.Version.MinorVersion = (ushort)value.Minor;
		}
	}

	internal BoundaryType BoundaryType => _boundaryType;

	internal bool ComputedHeaders => _responseState >= ResponseState.ComputedHeaders;

	internal bool SentHeaders => _responseState >= ResponseState.SentHeaders;

	private bool Disposed => _responseState >= ResponseState.Closed;

	private static bool CanSendResponseBody(int responseCode)
	{
		for (int i = 0; i < s_noResponseBody.Length; i++)
		{
			if (responseCode == s_noResponseBody[i])
			{
				return false;
			}
		}
		return true;
	}

	public void AddHeader(string name, string value)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"name={name}, value={value}", "AddHeader");
		}
		Headers.Set(name, value);
	}

	public void AppendHeader(string name, string value)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"name={name}, value={value}", "AppendHeader");
		}
		Headers.Add(name, value);
	}

	public void AppendCookie(Cookie cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException("cookie");
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"cookie: {cookie}", "AppendCookie");
		}
		Cookies.Add(cookie);
	}

	private void ComputeCookies()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, FormattableStringFactory.Create("Entering Set-Cookie: {0}, Set-Cookie2: {1}", Headers[HttpResponseHeader.SetCookie], Headers["Set-Cookie2"]), "ComputeCookies");
		}
		if (_cookies != null)
		{
			string text = null;
			string text2 = null;
			for (int i = 0; i < _cookies.Count; i++)
			{
				Cookie cookie = _cookies[i];
				string text3 = cookie.ToServerString();
				if (text3 != null && text3.Length != 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"Now looking at index:{i} cookie: {cookie}", "ComputeCookies");
					}
					if (cookie.IsRfc2965Variant())
					{
						text = ((text == null) ? text3 : (text + ", " + text3));
					}
					else
					{
						text2 = ((text2 == null) ? text3 : (text2 + ", " + text3));
					}
				}
			}
			if (!string.IsNullOrEmpty(text2))
			{
				Headers.Set("Set-Cookie", text2);
				if (string.IsNullOrEmpty(text))
				{
					Headers.Remove("Set-Cookie2");
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				Headers.Set("Set-Cookie2", text);
				if (string.IsNullOrEmpty(text2))
				{
					Headers.Remove("Set-Cookie");
				}
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, FormattableStringFactory.Create("Exiting Set-Cookie: {0} Set-Cookie2: {1}", Headers[HttpResponseHeader.SetCookie], Headers["Set-Cookie2"]), "ComputeCookies");
		}
	}

	public void Redirect(string url)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"url={url}", "Redirect");
		}
		Headers[HttpResponseHeader.Location] = url;
		StatusCode = 302;
		StatusDescription = HttpStatusDescription.Get(StatusCode);
	}

	public void SetCookie(Cookie cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException("cookie");
		}
		Cookie cookie2 = cookie.Clone();
		int num = Cookies.InternalAdd(cookie2, isStrict: true);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"cookie: {cookie}", "SetCookie");
		}
		if (num != 1)
		{
			throw new ArgumentException(System.SR.net_cookie_exists, "cookie");
		}
	}

	void IDisposable.Dispose()
	{
		Dispose();
	}

	private void CheckDisposed()
	{
		if (Disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	private void CheckSentHeaders()
	{
		if (SentHeaders)
		{
			throw new InvalidOperationException(System.SR.net_rspsubmitted);
		}
	}

	internal HttpListenerResponse()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, ".ctor");
		}
		_nativeResponse = default(global::Interop.HttpApi.HTTP_RESPONSE);
		_nativeResponse.StatusCode = 200;
		_nativeResponse.Version.MajorVersion = 1;
		_nativeResponse.Version.MinorVersion = 1;
		_responseState = ResponseState.Created;
	}

	internal HttpListenerResponse(HttpListenerContext httpContext)
		: this()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, httpContext, ".ctor");
		}
		_httpContext = httpContext;
	}

	public void CopyFrom(HttpListenerResponse templateResponse)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"templateResponse {templateResponse}", "CopyFrom");
		}
		_nativeResponse = default(global::Interop.HttpApi.HTTP_RESPONSE);
		_responseState = ResponseState.Created;
		_webHeaders = templateResponse._webHeaders;
		_boundaryType = templateResponse._boundaryType;
		_contentLength = templateResponse._contentLength;
		_nativeResponse.StatusCode = templateResponse._nativeResponse.StatusCode;
		_nativeResponse.Version.MajorVersion = templateResponse._nativeResponse.Version.MajorVersion;
		_nativeResponse.Version.MinorVersion = templateResponse._nativeResponse.Version.MinorVersion;
		_statusDescription = templateResponse._statusDescription;
		_keepAlive = templateResponse._keepAlive;
	}

	public void Abort()
	{
		if (!Disposed)
		{
			_responseState = ResponseState.Closed;
			HttpListenerContext.Abort();
		}
	}

	public void Close()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "Close");
		}
		((IDisposable)this).Dispose();
	}

	public void Close(byte[] responseEntity, bool willBlock)
	{
		CheckDisposed();
		if (responseEntity == null)
		{
			throw new ArgumentNullException("responseEntity");
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"ResponseState:{_responseState}, BoundaryType:{_boundaryType}, ContentLength:{_contentLength}", "Close");
		}
		if (!SentHeaders && _boundaryType != BoundaryType.Chunked)
		{
			ContentLength64 = responseEntity.Length;
		}
		EnsureResponseStream();
		if (willBlock)
		{
			try
			{
				_responseStream.Write(responseEntity, 0, responseEntity.Length);
				return;
			}
			catch (Win32Exception)
			{
				return;
			}
			finally
			{
				_responseStream.Close();
				_responseState = ResponseState.Closed;
				HttpListenerContext.Close();
			}
		}
		_responseStream.BeginWrite(responseEntity, 0, responseEntity.Length, NonBlockingCloseCallback, null);
	}

	private void Dispose()
	{
		if (!Disposed)
		{
			EnsureResponseStream();
			_responseStream.Close();
			_responseState = ResponseState.Closed;
			HttpListenerContext.Close();
		}
	}

	private void EnsureResponseStream()
	{
		if (_responseStream == null)
		{
			_responseStream = new HttpResponseStream(HttpListenerContext);
		}
	}

	private void NonBlockingCloseCallback(IAsyncResult asyncResult)
	{
		try
		{
			_responseStream.EndWrite(asyncResult);
		}
		catch (Win32Exception)
		{
		}
		finally
		{
			_responseStream.Close();
			HttpListenerContext.Close();
			_responseState = ResponseState.Closed;
		}
	}

	internal unsafe uint SendHeaders(global::Interop.HttpApi.HTTP_DATA_CHUNK* pDataChunk, HttpResponseStreamAsyncResult asyncResult, global::Interop.HttpApi.HTTP_FLAGS flags, bool isWebSocketHandshake)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"pDataChunk: {(IntPtr)pDataChunk}, asyncResult: {asyncResult}", "SendHeaders");
		}
		if (StatusCode == 401)
		{
			HttpListenerContext.SetAuthenticationHeaders();
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			StringBuilder stringBuilder = new StringBuilder("HttpListenerResponse Headers:\n");
			for (int i = 0; i < Headers.Count; i++)
			{
				stringBuilder.Append('\t');
				stringBuilder.Append(Headers.GetKey(i));
				stringBuilder.Append(" : ");
				stringBuilder.Append(Headers.Get(i));
				stringBuilder.Append('\n');
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, stringBuilder.ToString(), "SendHeaders");
			}
		}
		_responseState = ResponseState.SentHeaders;
		List<GCHandle> pinnedHeaders = SerializeHeaders(ref _nativeResponse.Headers, isWebSocketHandshake);
		uint num;
		try
		{
			if (pDataChunk != null)
			{
				_nativeResponse.EntityChunkCount = 1;
				_nativeResponse.pEntityChunks = pDataChunk;
			}
			else if (asyncResult != null && asyncResult.pDataChunks != null)
			{
				_nativeResponse.EntityChunkCount = asyncResult.dataChunkCount;
				_nativeResponse.pEntityChunks = asyncResult.pDataChunks;
			}
			else
			{
				_nativeResponse.EntityChunkCount = 0;
				_nativeResponse.pEntityChunks = null;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpSendHttpResponse flags:" + flags, "SendHeaders");
			}
			Unsafe.SkipInit(out uint numBytes);
			if (StatusDescription.Length > 0)
			{
				byte[] array = new byte[WebHeaderEncoding.GetByteCount(StatusDescription)];
				fixed (byte* pReason = &array[0])
				{
					_nativeResponse.ReasonLength = (ushort)array.Length;
					WebHeaderEncoding.GetBytes(StatusDescription, 0, array.Length, array, 0);
					_nativeResponse.pReason = (sbyte*)pReason;
					fixed (global::Interop.HttpApi.HTTP_RESPONSE* pHttpResponse = &_nativeResponse)
					{
						num = global::Interop.HttpApi.HttpSendHttpResponse(HttpListenerContext.RequestQueueHandle, HttpListenerRequest.RequestId, (uint)flags, pHttpResponse, null, &numBytes, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.Zero, 0u, (asyncResult == null) ? null : asyncResult._pOverlapped, null);
						if (asyncResult != null && num == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
						{
							asyncResult.IOCompleted(num, numBytes);
						}
					}
				}
			}
			else
			{
				fixed (global::Interop.HttpApi.HTTP_RESPONSE* pHttpResponse2 = &_nativeResponse)
				{
					num = global::Interop.HttpApi.HttpSendHttpResponse(HttpListenerContext.RequestQueueHandle, HttpListenerRequest.RequestId, (uint)flags, pHttpResponse2, null, &numBytes, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.Zero, 0u, (asyncResult == null) ? null : asyncResult._pOverlapped, null);
					if (asyncResult != null && num == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
					{
						asyncResult.IOCompleted(num, numBytes);
					}
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpSendHttpResponse returned:" + num, "SendHeaders");
			}
		}
		finally
		{
			FreePinnedHeaders(pinnedHeaders);
		}
		return num;
	}

	internal global::Interop.HttpApi.HTTP_FLAGS ComputeHeaders()
	{
		global::Interop.HttpApi.HTTP_FLAGS hTTP_FLAGS = global::Interop.HttpApi.HTTP_FLAGS.NONE;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "ComputeHeaders");
		}
		_responseState = ResponseState.ComputedHeaders;
		ComputeCoreHeaders();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"flags: {hTTP_FLAGS} _boundaryType: {_boundaryType} _contentLength: {_contentLength} _keepAlive: {_keepAlive}", "ComputeHeaders");
		}
		if (_boundaryType == BoundaryType.None)
		{
			if (HttpListenerRequest.ProtocolVersion.Minor == 0)
			{
				_keepAlive = false;
			}
			else
			{
				_boundaryType = BoundaryType.Chunked;
			}
			if (CanSendResponseBody(_httpContext.Response.StatusCode))
			{
				_contentLength = -1L;
			}
			else
			{
				ContentLength64 = 0L;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"flags:{hTTP_FLAGS} _BoundaryType:{_boundaryType} _contentLength:{_contentLength} _keepAlive: {_keepAlive}", "ComputeHeaders");
		}
		if (_boundaryType == BoundaryType.ContentLength)
		{
			Headers[HttpResponseHeader.ContentLength] = _contentLength.ToString("D", NumberFormatInfo.InvariantInfo);
			if (_contentLength == 0L)
			{
				hTTP_FLAGS = global::Interop.HttpApi.HTTP_FLAGS.NONE;
			}
		}
		else if (_boundaryType == BoundaryType.Chunked)
		{
			Headers[HttpResponseHeader.TransferEncoding] = "chunked";
		}
		else if (_boundaryType == BoundaryType.None)
		{
			hTTP_FLAGS = global::Interop.HttpApi.HTTP_FLAGS.NONE;
		}
		else
		{
			_keepAlive = false;
		}
		if (!_keepAlive)
		{
			Headers.Add(HttpResponseHeader.Connection, "close");
			if (hTTP_FLAGS == global::Interop.HttpApi.HTTP_FLAGS.NONE)
			{
				hTTP_FLAGS = global::Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
			}
		}
		else if (HttpListenerRequest.ProtocolVersion.Minor == 0)
		{
			Headers[HttpResponseHeader.KeepAlive] = "true";
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"flags:{hTTP_FLAGS} _BoundaryType:{_boundaryType} _contentLength:{_contentLength} _keepAlive: {_keepAlive}", "ComputeHeaders");
		}
		return hTTP_FLAGS;
	}

	internal void ComputeCoreHeaders()
	{
		if (HttpListenerContext.MutualAuthentication != null && HttpListenerContext.MutualAuthentication.Length > 0)
		{
			Headers[HttpResponseHeader.WwwAuthenticate] = HttpListenerContext.MutualAuthentication;
		}
		ComputeCookies();
	}

	private unsafe List<GCHandle> SerializeHeaders(ref global::Interop.HttpApi.HTTP_RESPONSE_HEADERS headers, bool isWebSocketHandshake)
	{
		global::Interop.HttpApi.HTTP_UNKNOWN_HEADER[] array = null;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "SerializeHeaders(HTTP_RESPONSE_HEADERS)", "SerializeHeaders");
		}
		if (Headers.Count == 0)
		{
			return null;
		}
		byte[] array2 = null;
		List<GCHandle> list = new List<GCHandle>();
		int num = 0;
		for (int i = 0; i < Headers.Count; i++)
		{
			string key = Headers.GetKey(i);
			int num2 = global::Interop.HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(key);
			if (num2 == 27 || (isWebSocketHandshake && num2 == 1))
			{
				num2 = -1;
			}
			if (num2 == -1)
			{
				string[] values = Headers.GetValues(i);
				num += values.Length;
			}
		}
		try
		{
			fixed (global::Interop.HttpApi.HTTP_KNOWN_HEADER* ptr = &headers.KnownHeaders)
			{
				for (int j = 0; j < Headers.Count; j++)
				{
					string key = Headers.GetKey(j);
					string text = Headers.Get(j);
					int num2 = global::Interop.HttpApi.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(key);
					if (num2 == 27 || (isWebSocketHandshake && num2 == 1))
					{
						num2 = -1;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"index={j},headers.count={Headers.Count},headerName:{key},lookup:{num2} headerValue:{text}", "SerializeHeaders");
					}
					if (num2 == -1)
					{
						if (array == null)
						{
							array = new global::Interop.HttpApi.HTTP_UNKNOWN_HEADER[num];
							GCHandle item = GCHandle.Alloc(array, GCHandleType.Pinned);
							list.Add(item);
							headers.pUnknownHeaders = (global::Interop.HttpApi.HTTP_UNKNOWN_HEADER*)(void*)item.AddrOfPinnedObject();
						}
						string[] values2 = Headers.GetValues(j);
						for (int k = 0; k < values2.Length; k++)
						{
							array2 = new byte[WebHeaderEncoding.GetByteCount(key)];
							array[headers.UnknownHeaderCount].NameLength = (ushort)array2.Length;
							WebHeaderEncoding.GetBytes(key, 0, array2.Length, array2, 0);
							GCHandle item = GCHandle.Alloc(array2, GCHandleType.Pinned);
							list.Add(item);
							array[headers.UnknownHeaderCount].pName = (sbyte*)(void*)item.AddrOfPinnedObject();
							text = values2[k];
							array2 = new byte[WebHeaderEncoding.GetByteCount(text)];
							array[headers.UnknownHeaderCount].RawValueLength = (ushort)array2.Length;
							WebHeaderEncoding.GetBytes(text, 0, array2.Length, array2, 0);
							item = GCHandle.Alloc(array2, GCHandleType.Pinned);
							list.Add(item);
							array[headers.UnknownHeaderCount].pRawValue = (sbyte*)(void*)item.AddrOfPinnedObject();
							headers.UnknownHeaderCount++;
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, "UnknownHeaderCount:" + headers.UnknownHeaderCount, "SerializeHeaders");
							}
						}
						continue;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"HttpResponseHeader[{num2}]:{(HttpResponseHeader)num2} headerValue:{text}", "SerializeHeaders");
					}
					if (text != null)
					{
						array2 = new byte[WebHeaderEncoding.GetByteCount(text)];
						ptr[num2].RawValueLength = (ushort)array2.Length;
						WebHeaderEncoding.GetBytes(text, 0, array2.Length, array2, 0);
						GCHandle item = GCHandle.Alloc(array2, GCHandleType.Pinned);
						list.Add(item);
						ptr[num2].pRawValue = (sbyte*)(void*)item.AddrOfPinnedObject();
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"pRawValue:{(IntPtr)ptr[num2].pRawValue} RawValueLength:{ptr[num2].RawValueLength} lookup: {num2}", "SerializeHeaders");
						}
					}
				}
				return list;
			}
		}
		catch
		{
			FreePinnedHeaders(list);
			throw;
		}
	}

	private void FreePinnedHeaders(List<GCHandle> pinnedHeaders)
	{
		if (pinnedHeaders == null)
		{
			return;
		}
		foreach (GCHandle pinnedHeader in pinnedHeaders)
		{
			if (pinnedHeader.IsAllocated)
			{
				pinnedHeader.Free();
			}
		}
	}

	internal void CancelLastWrite(SafeHandle requestQueueHandle)
	{
		_responseStream?.CancelLastWrite(requestQueueHandle);
	}
}
