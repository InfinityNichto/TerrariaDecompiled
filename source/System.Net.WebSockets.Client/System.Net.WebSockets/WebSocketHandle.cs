using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets;

internal sealed class WebSocketHandle
{
	private sealed class DefaultWebProxy : IWebProxy
	{
		public static DefaultWebProxy Instance { get; } = new DefaultWebProxy();


		public ICredentials Credentials
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public Uri GetProxy(Uri destination)
		{
			throw new NotSupportedException();
		}

		public bool IsBypassed(Uri host)
		{
			throw new NotSupportedException();
		}
	}

	private static SocketsHttpHandler s_defaultHandler;

	private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();

	private WebSocketState _state = WebSocketState.Connecting;

	private WebSocketDeflateOptions _negotiatedDeflateOptions;

	public WebSocket WebSocket { get; private set; }

	public WebSocketState State => WebSocket?.State ?? _state;

	public static ClientWebSocketOptions CreateDefaultOptions()
	{
		return new ClientWebSocketOptions
		{
			Proxy = DefaultWebProxy.Instance
		};
	}

	public void Dispose()
	{
		_state = WebSocketState.Closed;
		WebSocket?.Dispose();
	}

	public void Abort()
	{
		_abortSource.Cancel();
		WebSocket?.Abort();
	}

	public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
	{
		HttpResponseMessage response = null;
		SocketsHttpHandler handler = null;
		bool disposeHandler = true;
		try
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
			WebHeaderCollection requestHeaders = options._requestHeaders;
			if (requestHeaders != null && requestHeaders.Count > 0)
			{
				foreach (string requestHeader in options.RequestHeaders)
				{
					httpRequestMessage.Headers.TryAddWithoutValidation(requestHeader, options.RequestHeaders[requestHeader]);
				}
			}
			KeyValuePair<string, string> secKeyAndSecWebSocketAccept = CreateSecKeyAndSecWebSocketAccept();
			AddWebSocketHeaders(httpRequestMessage, secKeyAndSecWebSocketAccept.Key, options);
			if (options.Credentials == null && !options.UseDefaultCredentials && options.Proxy == null && options.Cookies == null && options.RemoteCertificateValidationCallback == null)
			{
				X509CertificateCollection clientCertificates = options._clientCertificates;
				if (clientCertificates != null && clientCertificates.Count == 0)
				{
					disposeHandler = false;
					handler = s_defaultHandler;
					if (handler == null)
					{
						handler = new SocketsHttpHandler
						{
							PooledConnectionLifetime = TimeSpan.Zero,
							UseProxy = false,
							UseCookies = false
						};
						if (Interlocked.CompareExchange(ref s_defaultHandler, handler, null) != null)
						{
							handler.Dispose();
							handler = s_defaultHandler;
						}
					}
					goto IL_02ee;
				}
			}
			handler = new SocketsHttpHandler
			{
				PooledConnectionLifetime = TimeSpan.Zero,
				CookieContainer = options.Cookies,
				UseCookies = (options.Cookies != null),
				SslOptions = 
				{
					RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback
				}
			};
			if (options.UseDefaultCredentials)
			{
				handler.Credentials = CredentialCache.DefaultCredentials;
			}
			else
			{
				handler.Credentials = options.Credentials;
			}
			if (options.Proxy == null)
			{
				handler.UseProxy = false;
			}
			else if (options.Proxy != DefaultWebProxy.Instance)
			{
				handler.Proxy = options.Proxy;
			}
			X509CertificateCollection clientCertificates2 = options._clientCertificates;
			if (clientCertificates2 != null && clientCertificates2.Count > 0)
			{
				handler.SslOptions.ClientCertificates = new X509Certificate2Collection();
				handler.SslOptions.ClientCertificates.AddRange(options.ClientCertificates);
			}
			goto IL_02ee;
			IL_02ee:
			CancellationTokenSource externalAndAbortCancellation;
			CancellationTokenSource cancellationTokenSource2;
			if (cancellationToken.CanBeCanceled)
			{
				CancellationTokenSource cancellationTokenSource;
				externalAndAbortCancellation = (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abortSource.Token));
				cancellationTokenSource2 = cancellationTokenSource;
			}
			else
			{
				cancellationTokenSource2 = null;
				externalAndAbortCancellation = _abortSource;
			}
			using (cancellationTokenSource2)
			{
				response = await new HttpMessageInvoker(handler).SendAsync(httpRequestMessage, externalAndAbortCancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
				externalAndAbortCancellation.Token.ThrowIfCancellationRequested();
			}
			if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
			{
				throw new WebSocketException(WebSocketError.NotAWebSocket, System.SR.Format(System.SR.net_WebSockets_Connect101Expected, (int)response.StatusCode));
			}
			ValidateHeader(response.Headers, "Connection", "Upgrade");
			ValidateHeader(response.Headers, "Upgrade", "websocket");
			ValidateHeader(response.Headers, "Sec-WebSocket-Accept", secKeyAndSecWebSocketAccept.Value);
			string text = null;
			if (response.Headers.TryGetValues("Sec-WebSocket-Protocol", out IEnumerable<string> values))
			{
				string[] array = (string[])values;
				if (array.Length != 0 && !string.IsNullOrEmpty(array[0]))
				{
					if (options._requestedSubProtocols != null)
					{
						foreach (string requestedSubProtocol in options._requestedSubProtocols)
						{
							if (requestedSubProtocol.Equals(array[0], StringComparison.OrdinalIgnoreCase))
							{
								text = requestedSubProtocol;
								break;
							}
						}
					}
					if (text == null)
					{
						throw new WebSocketException(WebSocketError.UnsupportedProtocol, System.SR.Format(System.SR.net_WebSockets_AcceptUnsupportedProtocol, string.Join(", ", options.RequestedSubProtocols), string.Join(", ", array)));
					}
				}
			}
			WebSocketDeflateOptions webSocketDeflateOptions = null;
			if (options.DangerousDeflateOptions != null && response.Headers.TryGetValues("Sec-WebSocket-Extensions", out IEnumerable<string> values2))
			{
				foreach (string item in values2)
				{
					ReadOnlySpan<char> readOnlySpan = item;
					if (readOnlySpan.TrimStart().StartsWith("permessage-deflate"))
					{
						webSocketDeflateOptions = ParseDeflateOptions(readOnlySpan, options.DangerousDeflateOptions);
						break;
					}
				}
			}
			if (response.Content == null)
			{
				throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
			}
			Stream stream = response.Content.ReadAsStream();
			WebSocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
			{
				IsServer = false,
				SubProtocol = text,
				KeepAliveInterval = options.KeepAliveInterval,
				DangerousDeflateOptions = webSocketDeflateOptions
			});
			_negotiatedDeflateOptions = webSocketDeflateOptions;
		}
		catch (Exception ex)
		{
			if (_state < WebSocketState.Closed)
			{
				_state = WebSocketState.Closed;
			}
			Abort();
			response?.Dispose();
			if (ex is WebSocketException || (ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
			{
				throw;
			}
			throw new WebSocketException(WebSocketError.Faulted, System.SR.net_webstatus_ConnectFailure, ex);
		}
		finally
		{
			if (disposeHandler)
			{
				handler?.Dispose();
			}
		}
	}

	private static WebSocketDeflateOptions ParseDeflateOptions(ReadOnlySpan<char> extension, WebSocketDeflateOptions original)
	{
		WebSocketDeflateOptions webSocketDeflateOptions = new WebSocketDeflateOptions();
		while (true)
		{
			int num = extension.IndexOf(';');
			ReadOnlySpan<char> span;
			ReadOnlySpan<char> readOnlySpan;
			if (num < 0)
			{
				span = extension;
			}
			else
			{
				readOnlySpan = extension;
				span = readOnlySpan[..num];
			}
			ReadOnlySpan<char> readOnlySpan2 = span.Trim();
			if (readOnlySpan2.Length > 0)
			{
				if (readOnlySpan2.SequenceEqual("client_no_context_takeover"))
				{
					webSocketDeflateOptions.ClientContextTakeover = false;
				}
				else if (readOnlySpan2.SequenceEqual("server_no_context_takeover"))
				{
					webSocketDeflateOptions.ServerContextTakeover = false;
				}
				else if (readOnlySpan2.StartsWith("client_max_window_bits"))
				{
					webSocketDeflateOptions.ClientMaxWindowBits = ParseWindowBits(readOnlySpan2);
				}
				else if (readOnlySpan2.StartsWith("server_max_window_bits"))
				{
					webSocketDeflateOptions.ServerMaxWindowBits = ParseWindowBits(readOnlySpan2);
				}
			}
			if (num < 0)
			{
				break;
			}
			readOnlySpan = extension;
			extension = readOnlySpan[(num + 1)..];
		}
		if (webSocketDeflateOptions.ClientMaxWindowBits > original.ClientMaxWindowBits)
		{
			throw new WebSocketException(string.Format(System.SR.net_WebSockets_ClientWindowBitsNegotiationFailure, original.ClientMaxWindowBits, webSocketDeflateOptions.ClientMaxWindowBits));
		}
		if (webSocketDeflateOptions.ServerMaxWindowBits > original.ServerMaxWindowBits)
		{
			throw new WebSocketException(string.Format(System.SR.net_WebSockets_ServerWindowBitsNegotiationFailure, original.ServerMaxWindowBits, webSocketDeflateOptions.ServerMaxWindowBits));
		}
		return webSocketDeflateOptions;
		static int ParseWindowBits(ReadOnlySpan<char> value)
		{
			int num2 = value.IndexOf('=');
			if (num2 < 0 || !int.TryParse(value.Slice(num2 + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result < 9 || result > 15)
			{
				throw new WebSocketException(WebSocketError.HeaderError, System.SR.Format(System.SR.net_WebSockets_InvalidResponseHeader, "permessage-deflate", value.ToString()));
			}
			return result;
		}
	}

	private static void AddWebSocketHeaders(HttpRequestMessage request, string secKey, ClientWebSocketOptions options)
	{
		request.Headers.TryAddWithoutValidation("Connection", "Upgrade");
		request.Headers.TryAddWithoutValidation("Upgrade", "websocket");
		request.Headers.TryAddWithoutValidation("Sec-WebSocket-Version", "13");
		request.Headers.TryAddWithoutValidation("Sec-WebSocket-Key", secKey);
		List<string> requestedSubProtocols = options._requestedSubProtocols;
		if (requestedSubProtocols != null && requestedSubProtocols.Count > 0)
		{
			request.Headers.TryAddWithoutValidation("Sec-WebSocket-Protocol", string.Join(", ", options.RequestedSubProtocols));
		}
		if (options.DangerousDeflateOptions != null)
		{
			request.Headers.TryAddWithoutValidation("Sec-WebSocket-Extensions", GetDeflateOptions(options.DangerousDeflateOptions));
		}
		static string GetDeflateOptions(WebSocketDeflateOptions options)
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			stringBuilder.Append("permessage-deflate").Append("; ");
			if (options.ClientMaxWindowBits != 15)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2, invariantCulture);
				handler.AppendFormatted("client_max_window_bits");
				handler.AppendLiteral("=");
				handler.AppendFormatted(options.ClientMaxWindowBits);
				stringBuilder3.Append(provider, ref handler);
			}
			else
			{
				stringBuilder.Append("client_max_window_bits");
			}
			if (!options.ClientContextTakeover)
			{
				stringBuilder.Append("; ").Append("client_no_context_takeover");
			}
			if (options.ServerMaxWindowBits != 15)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider2 = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2, invariantCulture);
				handler2.AppendLiteral("; ");
				handler2.AppendFormatted("server_max_window_bits");
				handler2.AppendLiteral("=");
				handler2.AppendFormatted(options.ServerMaxWindowBits);
				stringBuilder4.Append(provider2, ref handler2);
			}
			if (!options.ServerContextTakeover)
			{
				stringBuilder.Append("; ").Append("server_no_context_takeover");
			}
			return stringBuilder.ToString();
		}
	}

	private static KeyValuePair<string, string> CreateSecKeyAndSecWebSocketAccept()
	{
		ReadOnlySpan<byte> readOnlySpan = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"u8;
		Span<byte> span = stackalloc byte[24 + readOnlySpan.Length];
		bool flag = Guid.NewGuid().TryWriteBytes(span);
		string text = Convert.ToBase64String(span.Slice(0, 16));
		for (int i = 0; i < text.Length; i++)
		{
			span[i] = (byte)text[i];
		}
		readOnlySpan.CopyTo(span.Slice(text.Length));
		SHA1.TryHashData(span, span, out var bytesWritten);
		return new KeyValuePair<string, string>(text, Convert.ToBase64String(span.Slice(0, bytesWritten)));
	}

	private static void ValidateHeader(HttpHeaders headers, string name, string expectedValue)
	{
		if (headers.NonValidated.TryGetValues(name, out var values))
		{
			if (values.Count == 1)
			{
				using HeaderStringValues.Enumerator enumerator = values.GetEnumerator();
				if (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					if (string.Equals(current, expectedValue, StringComparison.OrdinalIgnoreCase))
					{
						return;
					}
				}
			}
			throw new WebSocketException(WebSocketError.HeaderError, System.SR.Format(System.SR.net_WebSockets_InvalidResponseHeader, name, values));
		}
		throw new WebSocketException(WebSocketError.Faulted, System.SR.Format(System.SR.net_WebSockets_MissingResponseHeader, name));
	}
}
