using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class Http3RequestStream : IHttpHeadersHandler, IAsyncDisposable, IDisposable
{
	private sealed class Http3ReadStream : HttpBaseStream
	{
		private Http3RequestStream _stream;

		private HttpResponseMessage _response;

		public override bool CanRead => _stream != null;

		public override bool CanWrite => false;

		public Http3ReadStream(Http3RequestStream stream)
		{
			_stream = stream;
			_response = stream._response;
		}

		~Http3ReadStream()
		{
			Dispose(disposing: false);
		}

		protected override void Dispose(bool disposing)
		{
			Http3RequestStream http3RequestStream = Interlocked.Exchange(ref _stream, null);
			if (http3RequestStream != null)
			{
				if (disposing)
				{
					http3RequestStream.Dispose();
				}
				else
				{
					http3RequestStream._connection.RemoveStream(http3RequestStream._stream);
					http3RequestStream._connection = null;
				}
				_response = null;
				base.Dispose(disposing);
			}
		}

		public override async ValueTask DisposeAsync()
		{
			Http3RequestStream http3RequestStream = Interlocked.Exchange(ref _stream, null);
			if (http3RequestStream != null)
			{
				await http3RequestStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				_response = null;
				await base.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		public override int Read(Span<byte> buffer)
		{
			if (_stream == null)
			{
				throw new ObjectDisposedException("Http3RequestStream");
			}
			return _stream.ReadResponseContent(_response, buffer);
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (_stream == null)
			{
				return ValueTask.FromException<int>(new ObjectDisposedException("Http3RequestStream"));
			}
			return _stream.ReadResponseContentAsync(_response, buffer, cancellationToken);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
	}

	private sealed class Http3WriteStream : HttpBaseStream
	{
		private Http3RequestStream _stream;

		public override bool CanRead => false;

		public override bool CanWrite => _stream != null;

		public Http3WriteStream(Http3RequestStream stream)
		{
			_stream = stream;
		}

		protected override void Dispose(bool disposing)
		{
			_stream = null;
			base.Dispose(disposing);
		}

		public override int Read(Span<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			if (_stream == null)
			{
				return ValueTask.FromException(new ObjectDisposedException("Http3WriteStream"));
			}
			return _stream.WriteRequestContentAsync(buffer, cancellationToken);
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (_stream == null)
			{
				return Task.FromException(new ObjectDisposedException("Http3WriteStream"));
			}
			return _stream.FlushSendBufferAsync(endStream: false, cancellationToken).AsTask();
		}
	}

	private enum HeaderState
	{
		StatusHeader,
		SkipExpect100Headers,
		ResponseHeaders,
		TrailingHeaders
	}

	private readonly HttpRequestMessage _request;

	private Http3Connection _connection;

	private long _streamId = -1L;

	private QuicStream _stream;

	private System.Net.ArrayBuffer _sendBuffer;

	private readonly ReadOnlyMemory<byte>[] _gatheredSendBuffer = new ReadOnlyMemory<byte>[2];

	private System.Net.ArrayBuffer _recvBuffer;

	private TaskCompletionSource<bool> _expect100ContinueCompletionSource;

	private bool _disposed;

	private CancellationTokenSource _goawayCancellationSource;

	private CancellationToken _goawayCancellationToken;

	private HttpResponseMessage _response;

	private QPackDecoder _headerDecoder;

	private HeaderState _headerState;

	private long _headerBudgetRemaining;

	private string[] _headerValues = Array.Empty<string>();

	private List<(HeaderDescriptor name, string value)> _trailingHeaders;

	private long _responseDataPayloadRemaining;

	private long _requestContentLengthRemaining;

	private bool _singleDataFrameWritten;

	public long StreamId
	{
		get
		{
			return Volatile.Read(ref _streamId);
		}
		set
		{
			Volatile.Write(ref _streamId, value);
		}
	}

	public Http3RequestStream(HttpRequestMessage request, Http3Connection connection, QuicStream stream)
	{
		_request = request;
		_connection = connection;
		_stream = stream;
		_sendBuffer = new System.Net.ArrayBuffer(64, usePool: true);
		_recvBuffer = new System.Net.ArrayBuffer(64, usePool: true);
		_headerBudgetRemaining = (long)connection.Pool.Settings._maxResponseHeadersLength * 1024L;
		_headerDecoder = new QPackDecoder((int)Math.Min(2147483647L, _headerBudgetRemaining));
		_goawayCancellationSource = new CancellationTokenSource();
		_goawayCancellationToken = _goawayCancellationSource.Token;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			AbortStream();
			_stream.Dispose();
			DisposeSyncHelper();
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			_disposed = true;
			AbortStream();
			await _stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			DisposeSyncHelper();
		}
	}

	private void DisposeSyncHelper()
	{
		_connection.RemoveStream(_stream);
		_sendBuffer.Dispose();
		_recvBuffer.Dispose();
		Interlocked.Exchange(ref _goawayCancellationSource, null)?.Dispose();
	}

	public void GoAway()
	{
		using CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref _goawayCancellationSource, null);
		cancellationTokenSource?.Cancel();
	}

	public async Task<HttpResponseMessage> SendAsync(CancellationToken cancellationToken)
	{
		bool disposeSelf = true;
		using CancellationTokenSource requestCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _goawayCancellationToken);
		HttpResponseMessage result;
		try
		{
			_ = 4;
			try
			{
				BufferHeaders(_request);
				if (_request.HasHeaders && _request.Headers.ExpectContinue == true)
				{
					_expect100ContinueCompletionSource = new TaskCompletionSource<bool>();
				}
				if (_expect100ContinueCompletionSource != null || _request.Content == null)
				{
					await FlushSendBufferAsync(_request.Content == null, requestCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				HttpContent? content = _request.Content;
				CancellationToken cancellationToken2 = ((content != null && !content.AllowDuplex) ? requestCancellationSource.Token : default(CancellationToken));
				Task sendContentTask = ((_request.Content != null) ? SendContentAsync(_request.Content, cancellationToken2) : Task.CompletedTask);
				Task readResponseTask = ReadResponseAsync(requestCancellationSource.Token);
				bool sendContentObserved = false;
				int num;
				if (!sendContentTask.IsCompleted)
				{
					HttpContent? content2 = _request.Content;
					num = ((content2 == null || !content2.AllowDuplex) ? 1 : 0);
				}
				else
				{
					num = 1;
				}
				bool flag = (byte)num != 0;
				bool flag2 = flag;
				if (!flag2)
				{
					flag2 = await Task.WhenAny(sendContentTask, readResponseTask).ConfigureAwait(continueOnCapturedContext: false) == sendContentTask;
				}
				if (flag2 || sendContentTask.IsCompleted)
				{
					try
					{
						await sendContentTask.ConfigureAwait(continueOnCapturedContext: false);
						sendContentObserved = true;
					}
					catch
					{
						_connection.LogExceptions(readResponseTask);
						throw;
					}
				}
				else
				{
					_connection.LogExceptions(sendContentTask);
				}
				await readResponseTask.ConfigureAwait(continueOnCapturedContext: false);
				Interlocked.Exchange(ref _goawayCancellationSource, null)?.Dispose();
				HttpConnectionResponseContent responseContent = (HttpConnectionResponseContent)_response.Content;
				bool useEmptyResponseContent = responseContent.Headers.ContentLength == 0 && sendContentObserved;
				if (useEmptyResponseContent)
				{
					await DrainContentLength0Frames(requestCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
					responseContent.SetStream(EmptyReadStream.Instance);
				}
				else
				{
					responseContent.SetStream(new Http3ReadStream(this));
				}
				if (_connection.Pool.Settings._useCookies)
				{
					CookieHelper.ProcessReceivedCookies(_response, _connection.Pool.Settings._cookieContainer);
				}
				HttpResponseMessage response = _response;
				_response = null;
				disposeSelf = useEmptyResponseContent;
				result = response;
			}
			catch (QuicStreamAbortedException ex) when (ex.ErrorCode == 272)
			{
				throw new HttpRequestException(System.SR.net_http_retry_on_older_version, ex, RequestRetryType.RetryOnLowerHttpVersion);
			}
			catch (QuicStreamAbortedException ex2) when (ex2.ErrorCode == 267)
			{
				throw new HttpRequestException(System.SR.net_http_request_aborted, ex2, RequestRetryType.RetryOnConnectionFailure);
			}
			catch (QuicStreamAbortedException ex3)
			{
				Exception abortException = _connection.AbortException;
				throw new HttpRequestException(System.SR.net_http_client_execution_error, abortException ?? ex3);
			}
			catch (QuicConnectionAbortedException abortException2)
			{
				Exception inner = _connection.Abort(abortException2);
				throw new HttpRequestException(System.SR.net_http_client_execution_error, inner);
			}
			catch (OperationCanceledException ex4) when (ex4.CancellationToken == requestCancellationSource.Token)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					_stream.AbortWrite(268L);
					throw new OperationCanceledException(ex4.Message, ex4, cancellationToken);
				}
				throw new HttpRequestException(System.SR.net_http_request_aborted, ex4, RequestRetryType.RetryOnConnectionFailure);
			}
			catch (Http3ConnectionException ex5)
			{
				_connection.Abort(ex5);
				throw new HttpRequestException(System.SR.net_http_client_execution_error, ex5);
			}
			catch (Exception ex6)
			{
				_stream.AbortWrite(258L);
				if (ex6 is HttpRequestException)
				{
					throw;
				}
				throw new HttpRequestException(System.SR.net_http_client_execution_error, ex6);
			}
		}
		finally
		{
			if (disposeSelf)
			{
				await DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		return result;
	}

	private async Task ReadResponseAsync(CancellationToken cancellationToken)
	{
		do
		{
			_headerState = HeaderState.StatusHeader;
			var (http3FrameType, headersLength) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (http3FrameType != Http3FrameType.Headers)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Expected HEADERS as first response frame; received {http3FrameType}.", "ReadResponseAsync");
				}
				throw new HttpRequestException(System.SR.net_http_invalid_response);
			}
			await ReadHeadersAsync(headersLength, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		while (_response.StatusCode < HttpStatusCode.OK);
		_headerState = HeaderState.TrailingHeaders;
	}

	private async Task SendContentAsync(HttpContent content, CancellationToken cancellationToken)
	{
		if (_expect100ContinueCompletionSource != null)
		{
			Timer timer = null;
			try
			{
				if (_connection.Pool.Settings._expect100ContinueTimeout != Timeout.InfiniteTimeSpan)
				{
					timer = new Timer(delegate(object o)
					{
						((Http3RequestStream)o)._expect100ContinueCompletionSource.TrySetResult(result: true);
					}, this, _connection.Pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
				}
				if (!(await _expect100ContinueCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false)))
				{
					return;
				}
			}
			finally
			{
				if (timer != null)
				{
					await timer.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		_requestContentLengthRemaining = content.Headers.ContentLength ?? (-1);
		using (Http3WriteStream writeStream = new Http3WriteStream(this))
		{
			await content.CopyToAsync(writeStream, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		_requestContentLengthRemaining = 0L;
		if (_sendBuffer.ActiveLength != 0)
		{
			await FlushSendBufferAsync(endStream: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			_stream.Shutdown();
		}
	}

	private async ValueTask WriteRequestContentAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		if (buffer.Length == 0)
		{
			return;
		}
		long requestContentLengthRemaining = _requestContentLengthRemaining;
		if (requestContentLengthRemaining != -1)
		{
			if (buffer.Length > _requestContentLengthRemaining)
			{
				string net_http_content_write_larger_than_content_length = System.SR.net_http_content_write_larger_than_content_length;
				throw new IOException(net_http_content_write_larger_than_content_length, new HttpRequestException(net_http_content_write_larger_than_content_length));
			}
			_requestContentLengthRemaining -= buffer.Length;
			if (!_singleDataFrameWritten)
			{
				BufferFrameEnvelope(Http3FrameType.Data, requestContentLengthRemaining);
				_gatheredSendBuffer[0] = _sendBuffer.ActiveMemory;
				_gatheredSendBuffer[1] = buffer;
				await _stream.WriteAsync(_gatheredSendBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_sendBuffer.Discard(_sendBuffer.ActiveLength);
				_singleDataFrameWritten = true;
			}
			else
			{
				await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		else
		{
			BufferFrameEnvelope(Http3FrameType.Data, buffer.Length);
			_gatheredSendBuffer[0] = _sendBuffer.ActiveMemory;
			_gatheredSendBuffer[1] = buffer;
			await _stream.WriteAsync(_gatheredSendBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_sendBuffer.Discard(_sendBuffer.ActiveLength);
		}
	}

	private async ValueTask FlushSendBufferAsync(bool endStream, CancellationToken cancellationToken)
	{
		await _stream.WriteAsync(_sendBuffer.ActiveMemory, endStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_sendBuffer.Discard(_sendBuffer.ActiveLength);
		await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async ValueTask DrainContentLength0Frames(CancellationToken cancellationToken)
	{
		while (true)
		{
			var (http3FrameType, num) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (http3FrameType.HasValue)
			{
				Http3FrameType valueOrDefault = http3FrameType.GetValueOrDefault();
				if (valueOrDefault == Http3FrameType.Data)
				{
					if (num != 0L)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace("Response content exceeded Content-Length.", "DrainContentLength0Frames");
						}
						throw new HttpRequestException(System.SR.net_http_invalid_response);
					}
					continue;
				}
				if (valueOrDefault != Http3FrameType.Headers)
				{
					break;
				}
				_trailingHeaders = new List<(HeaderDescriptor, string)>();
				await ReadHeadersAsync(num, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			CopyTrailersToResponseMessage(_response);
			break;
		}
	}

	private void CopyTrailersToResponseMessage(HttpResponseMessage responseMessage)
	{
		List<(HeaderDescriptor name, string value)> trailingHeaders = _trailingHeaders;
		if (trailingHeaders == null || trailingHeaders.Count <= 0)
		{
			return;
		}
		foreach (var (descriptor, value) in _trailingHeaders)
		{
			responseMessage.TrailingHeaders.TryAddWithoutValidation(descriptor, value);
		}
		_trailingHeaders.Clear();
	}

	private void BufferHeaders(HttpRequestMessage request)
	{
		_sendBuffer.Commit(9);
		_sendBuffer.EnsureAvailableSpace(2);
		_sendBuffer.AvailableSpan[0] = 0;
		_sendBuffer.AvailableSpan[1] = 0;
		_sendBuffer.Commit(2);
		HttpMethod httpMethod = HttpMethod.Normalize(request.Method);
		BufferBytes(httpMethod.Http3EncodedBytes);
		BufferIndexedHeader(23);
		if (request.HasHeaders && request.Headers.Host != null)
		{
			BufferLiteralHeaderWithStaticNameReference(0, request.Headers.Host);
		}
		else
		{
			BufferBytes(_connection.Pool._http3EncodedAuthorityHostHeader);
		}
		string pathAndQuery = request.RequestUri.PathAndQuery;
		if (pathAndQuery == "/")
		{
			BufferIndexedHeader(1);
		}
		else
		{
			BufferLiteralHeaderWithStaticNameReference(1, pathAndQuery);
		}
		BufferBytes(_connection.AltUsedEncodedHeaderBytes);
		if (request.HasHeaders)
		{
			if (request.HasHeaders && request.Headers.TransferEncodingChunked == true)
			{
				request.Headers.TransferEncodingChunked = false;
			}
			BufferHeaderCollection(request.Headers);
		}
		if (_connection.Pool.Settings._useCookies)
		{
			string cookieHeader = _connection.Pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
			if (cookieHeader != string.Empty)
			{
				Encoding valueEncoding = _connection.Pool.Settings._requestHeaderEncodingSelector?.Invoke("Cookie", request);
				BufferLiteralHeaderWithStaticNameReference(5, cookieHeader, valueEncoding);
			}
		}
		if (request.Content == null)
		{
			if (httpMethod.MustHaveRequestBody)
			{
				BufferIndexedHeader(4);
			}
		}
		else
		{
			BufferHeaderCollection(request.Content.Headers);
		}
		int num = _sendBuffer.ActiveLength - 9;
		int byteCount = VariableLengthIntegerHelper.GetByteCount(num);
		_sendBuffer.Discard(9 - byteCount - 1);
		_sendBuffer.ActiveSpan[0] = 1;
		int num2 = VariableLengthIntegerHelper.WriteInteger(_sendBuffer.ActiveSpan.Slice(1, byteCount), num);
	}

	private void BufferHeaderCollection(HttpHeaders headers)
	{
		if (headers.HeaderStore == null)
		{
			return;
		}
		HeaderEncodingSelector<HttpRequestMessage> requestHeaderEncodingSelector = _connection.Pool.Settings._requestHeaderEncodingSelector;
		foreach (KeyValuePair<HeaderDescriptor, object> item in headers.HeaderStore)
		{
			int storeValuesIntoStringArray = HttpHeaders.GetStoreValuesIntoStringArray(item.Key, item.Value, ref _headerValues);
			ReadOnlySpan<string> readOnlySpan = _headerValues.AsSpan(0, storeValuesIntoStringArray);
			Encoding valueEncoding = requestHeaderEncodingSelector?.Invoke(item.Key.Name, _request);
			KnownHeader knownHeader = item.Key.KnownHeader;
			if (knownHeader != null)
			{
				if (knownHeader == KnownHeaders.Host || knownHeader == KnownHeaders.Connection || knownHeader == KnownHeaders.Upgrade || knownHeader == KnownHeaders.ProxyConnection)
				{
					continue;
				}
				if (item.Key.KnownHeader == KnownHeaders.TE)
				{
					ReadOnlySpan<string> readOnlySpan2 = readOnlySpan;
					for (int i = 0; i < readOnlySpan2.Length; i++)
					{
						string text = readOnlySpan2[i];
						if (string.Equals(text, "trailers", StringComparison.OrdinalIgnoreCase))
						{
							BufferLiteralHeaderWithoutNameReference("TE", text, valueEncoding);
							break;
						}
					}
				}
				else
				{
					BufferBytes(knownHeader.Http3EncodedName);
					string separator = null;
					if (readOnlySpan.Length > 1)
					{
						HttpHeaderParser parser = item.Key.Parser;
						separator = ((parser == null || !parser.SupportsMultipleValues) ? ", " : parser.Separator);
					}
					BufferLiteralHeaderValues(readOnlySpan, separator, valueEncoding);
				}
			}
			else
			{
				BufferLiteralHeaderWithoutNameReference(item.Key.Name, readOnlySpan, ", ", valueEncoding);
			}
		}
	}

	private void BufferIndexedHeader(int index)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeStaticIndexedHeaderField(index, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderWithStaticNameReference(int nameIndex, string value, Encoding valueEncoding = null)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReference(nameIndex, value, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderWithoutNameReference(string name, ReadOnlySpan<string> values, string separator, Encoding valueEncoding)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, values, separator, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderWithoutNameReference(string name, string value, Encoding valueEncoding)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, value, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderValues(ReadOnlySpan<string> values, string separator, Encoding valueEncoding)
	{
		int length;
		while (!QPackEncoder.EncodeValueString(values, separator, valueEncoding, _sendBuffer.AvailableSpan, out length))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(length);
	}

	private void BufferFrameEnvelope(Http3FrameType frameType, long payloadLength)
	{
		int bytesWritten;
		while (!Http3Frame.TryWriteFrameEnvelope(frameType, payloadLength, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferBytes(ReadOnlySpan<byte> span)
	{
		_sendBuffer.EnsureAvailableSpace(span.Length);
		span.CopyTo(_sendBuffer.AvailableSpan);
		_sendBuffer.Commit(span.Length);
	}

	private async ValueTask<(Http3FrameType? frameType, long payloadLength)> ReadFrameEnvelopeAsync(CancellationToken cancellationToken)
	{
		while (true)
		{
			if (!Http3Frame.TryReadIntegerPair(_recvBuffer.ActiveSpan, out var a, out var b, out var bytesRead))
			{
				_recvBuffer.EnsureAvailableSpace(16);
				bytesRead = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (bytesRead == 0)
				{
					break;
				}
				_recvBuffer.Commit(bytesRead);
				continue;
			}
			_recvBuffer.Discard(bytesRead);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Received frame {a} of length {b}.", "ReadFrameEnvelopeAsync");
			}
			Http3FrameType http3FrameType = (Http3FrameType)a;
			if ((ulong)http3FrameType <= 13uL)
			{
				switch (http3FrameType)
				{
				case Http3FrameType.Data:
				case Http3FrameType.Headers:
					return ((Http3FrameType)a, b);
				case Http3FrameType.ReservedHttp2Priority:
				case Http3FrameType.Settings:
				case Http3FrameType.ReservedHttp2Ping:
				case Http3FrameType.GoAway:
				case Http3FrameType.ReservedHttp2WindowUpdate:
				case Http3FrameType.ReservedHttp2Continuation:
				case Http3FrameType.MaxPushId:
					throw new Http3ConnectionException(Http3ErrorCode.UnexpectedFrame);
				case Http3FrameType.CancelPush:
				case Http3FrameType.PushPromise:
					throw new Http3ConnectionException(Http3ErrorCode.IdError);
				}
			}
			await SkipUnknownPayloadAsync(b, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (_recvBuffer.ActiveLength == 0)
		{
			return (null, 0L);
		}
		throw new HttpRequestException(System.SR.net_http_invalid_response_premature_eof);
	}

	private async ValueTask ReadHeadersAsync(long headersLength, CancellationToken cancellationToken)
	{
		if (headersLength > _headerBudgetRemaining)
		{
			_stream.AbortWrite(263L);
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_response_headers_exceeded_length, (long)_connection.Pool.Settings._maxResponseHeadersLength * 1024L));
		}
		_headerBudgetRemaining -= headersLength;
		while (headersLength != 0L)
		{
			if (_recvBuffer.ActiveLength == 0)
			{
				_recvBuffer.EnsureAvailableSpace(1);
				int num = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Server closed response stream before entire header payload could be read. {headersLength:N0} bytes remaining.", "ReadHeadersAsync");
					}
					throw new HttpRequestException(System.SR.net_http_invalid_response_premature_eof);
				}
				_recvBuffer.Commit(num);
			}
			int num2 = (int)Math.Min(headersLength, _recvBuffer.ActiveLength);
			_headerDecoder.Decode(_recvBuffer.ActiveSpan.Slice(0, num2), this);
			_recvBuffer.Discard(num2);
			headersLength -= num2;
		}
		_headerDecoder.Reset();
	}

	void IHttpHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
	{
		if (!HeaderDescriptor.TryGet(name, out var descriptor))
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
		}
		OnHeader(null, descriptor, null, value);
	}

	void IHttpHeadersHandler.OnStaticIndexedHeader(int index)
	{
		GetStaticQPackHeader(index, out var descriptor, out var knownValue);
		OnHeader(index, descriptor, knownValue, default(ReadOnlySpan<byte>));
	}

	void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
	{
		GetStaticQPackHeader(index, out var descriptor, out var _);
		OnHeader(index, descriptor, null, value);
	}

	private void GetStaticQPackHeader(int index, out HeaderDescriptor descriptor, out string knownValue)
	{
		if (!HeaderDescriptor.TryGetStaticQPackHeader(index, out descriptor, out knownValue))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Response contains invalid static header index '{index}'.", "GetStaticQPackHeader");
			}
			throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
		}
	}

	private void OnHeader(int? staticIndex, HeaderDescriptor descriptor, string staticValue, ReadOnlySpan<byte> literalValue)
	{
		if (descriptor.Name[0] == ':')
		{
			if (descriptor.KnownHeader != KnownHeaders.PseudoStatus)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Received unknown pseudo-header '" + descriptor.Name + "'.", "OnHeader");
				}
				throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
			}
			if (_headerState != 0)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Received extra status header.", "OnHeader");
				}
				throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
			}
			int num = ((staticValue == null) ? HttpConnectionBase.ParseStatusCode(literalValue) : (staticIndex switch
			{
				24 => 103, 
				25 => 200, 
				26 => 304, 
				27 => 404, 
				28 => 503, 
				63 => 100, 
				64 => 204, 
				65 => 206, 
				66 => 302, 
				67 => 400, 
				68 => 403, 
				69 => 421, 
				70 => 425, 
				71 => 500, 
				_ => ParseStatusCode(staticIndex, staticValue), 
			}));
			_response = new HttpResponseMessage
			{
				Version = HttpVersion.Version30,
				RequestMessage = _request,
				Content = new HttpConnectionResponseContent(),
				StatusCode = (HttpStatusCode)num
			};
			if (num < 200)
			{
				_headerState = HeaderState.SkipExpect100Headers;
				if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueCompletionSource != null)
				{
					_expect100ContinueCompletionSource.TrySetResult(result: true);
				}
				return;
			}
			_headerState = HeaderState.ResponseHeaders;
			if (_expect100ContinueCompletionSource != null)
			{
				bool result = num < 300;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Expecting 100 Continue but received final status {num}.", "OnHeader");
				}
				_expect100ContinueCompletionSource.TrySetResult(result);
			}
		}
		else
		{
			if (_headerState == HeaderState.SkipExpect100Headers)
			{
				return;
			}
			string text = staticValue;
			if (text == null)
			{
				Encoding valueEncoding = _connection.Pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _request);
				text = _connection.GetResponseHeaderValueWithCaching(descriptor, literalValue, valueEncoding);
			}
			switch (_headerState)
			{
			case HeaderState.StatusHeader:
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Received headers without :status.", "OnHeader");
				}
				throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
			case HeaderState.ResponseHeaders:
				if (descriptor.HeaderType.HasFlag(HttpHeaderType.Content))
				{
					_response.Content.Headers.TryAddWithoutValidation(descriptor, text);
				}
				else
				{
					_response.Headers.TryAddWithoutValidation(descriptor.HeaderType.HasFlag(HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, text);
				}
				break;
			case HeaderState.TrailingHeaders:
				_trailingHeaders.Add((descriptor.HeaderType.HasFlag(HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, text));
				break;
			case HeaderState.SkipExpect100Headers:
				break;
			}
		}
		int ParseStatusCode(int? index, string value)
		{
			string message = $"Unexpected QPACK table reference for Status code: index={index} value='{value}'";
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace(message, "OnHeader");
			}
			return HttpConnectionBase.ParseStatusCode(Encoding.ASCII.GetBytes(value));
		}
	}

	private async ValueTask SkipUnknownPayloadAsync(long payloadLength, CancellationToken cancellationToken)
	{
		while (payloadLength != 0L)
		{
			if (_recvBuffer.ActiveLength == 0)
			{
				_recvBuffer.EnsureAvailableSpace(1);
				int num = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					throw new Http3ConnectionException(Http3ErrorCode.FrameError);
				}
				_recvBuffer.Commit(num);
			}
			long num2 = Math.Min(payloadLength, _recvBuffer.ActiveLength);
			_recvBuffer.Discard((int)num2);
			payloadLength -= num2;
		}
	}

	private int ReadResponseContent(HttpResponseMessage response, Span<byte> buffer)
	{
		try
		{
			int num = 0;
			while (buffer.Length != 0 && (_responseDataPayloadRemaining > 0 || ReadNextDataFrameAsync(response, CancellationToken.None).AsTask().GetAwaiter().GetResult()))
			{
				if (_recvBuffer.ActiveLength != 0)
				{
					int num2 = (int)Math.Min(buffer.Length, Math.Min(_responseDataPayloadRemaining, _recvBuffer.ActiveLength));
					Span<byte> span = _recvBuffer.ActiveSpan;
					span = span.Slice(0, num2);
					span.CopyTo(buffer);
					num += num2;
					_responseDataPayloadRemaining -= num2;
					_recvBuffer.Discard(num2);
					buffer = buffer.Slice(num2);
					if (_responseDataPayloadRemaining == 0L && _recvBuffer.ActiveLength == 0)
					{
						break;
					}
					continue;
				}
				int length = (int)Math.Min(buffer.Length, _responseDataPayloadRemaining);
				int num3 = _stream.Read(buffer.Slice(0, length));
				if (num3 == 0)
				{
					throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _responseDataPayloadRemaining));
				}
				num += num3;
				_responseDataPayloadRemaining -= num3;
				buffer = buffer.Slice(num3);
				break;
			}
			return num;
		}
		catch (Exception ex)
		{
			HandleReadResponseContentException(ex, CancellationToken.None);
			return 0;
		}
	}

	private async ValueTask<int> ReadResponseContentAsync(HttpResponseMessage response, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			int totalBytesRead = 0;
			while (buffer.Length != 0)
			{
				bool flag = _responseDataPayloadRemaining <= 0;
				bool flag2 = flag;
				if (flag2)
				{
					flag2 = !(await ReadNextDataFrameAsync(response, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
				}
				if (flag2)
				{
					break;
				}
				if (_recvBuffer.ActiveLength != 0)
				{
					int num = (int)Math.Min(buffer.Length, Math.Min(_responseDataPayloadRemaining, _recvBuffer.ActiveLength));
					Span<byte> span = _recvBuffer.ActiveSpan;
					span = span.Slice(0, num);
					span.CopyTo(buffer.Span);
					totalBytesRead += num;
					_responseDataPayloadRemaining -= num;
					_recvBuffer.Discard(num);
					buffer = buffer.Slice(num);
					if (_responseDataPayloadRemaining == 0L && _recvBuffer.ActiveLength == 0)
					{
						break;
					}
					continue;
				}
				int length = (int)Math.Min(buffer.Length, _responseDataPayloadRemaining);
				int num2 = await _stream.ReadAsync(buffer.Slice(0, length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num2 == 0)
				{
					throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _responseDataPayloadRemaining));
				}
				totalBytesRead += num2;
				_responseDataPayloadRemaining -= num2;
				buffer = buffer.Slice(num2);
				break;
			}
			return totalBytesRead;
		}
		catch (Exception ex)
		{
			HandleReadResponseContentException(ex, cancellationToken);
			return 0;
		}
	}

	private void HandleReadResponseContentException(Exception ex, CancellationToken cancellationToken)
	{
		if (!(ex is QuicStreamAbortedException) && !(ex is QuicOperationAbortedException))
		{
			if (!(ex is QuicConnectionAbortedException))
			{
				if (!(ex is Http3ConnectionException))
				{
					if (ex is OperationCanceledException ex2 && ex2.CancellationToken == cancellationToken)
					{
						_stream.AbortRead(268L);
						ExceptionDispatchInfo.Throw(ex);
						return;
					}
					_stream.AbortRead(258L);
					throw new IOException(System.SR.net_http_client_execution_error, new HttpRequestException(System.SR.net_http_client_execution_error, ex));
				}
				_connection.Abort(ex);
				throw new IOException(System.SR.net_http_client_execution_error, new HttpRequestException(System.SR.net_http_client_execution_error, ex));
			}
			Exception inner = _connection.Abort(ex);
			throw new IOException(System.SR.net_http_client_execution_error, new HttpRequestException(System.SR.net_http_client_execution_error, inner));
		}
		throw new IOException(System.SR.net_http_client_execution_error, new HttpRequestException(System.SR.net_http_client_execution_error, ex));
	}

	private async ValueTask<bool> ReadNextDataFrameAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (_responseDataPayloadRemaining == -1)
		{
			return false;
		}
		while (true)
		{
			var (http3FrameType, num) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			switch (http3FrameType)
			{
			default:
				continue;
			case Http3FrameType.Data:
				goto IL_00d6;
			case Http3FrameType.Headers:
				_trailingHeaders = new List<(HeaderDescriptor, string)>();
				await ReadHeadersAsync(num, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case null:
				break;
			}
			break;
			IL_00d6:
			if (num != 0L)
			{
				_responseDataPayloadRemaining = num;
				return true;
			}
		}
		CopyTrailersToResponseMessage(response);
		_responseDataPayloadRemaining = -1L;
		return false;
	}

	public void Trace(string message, [CallerMemberName] string memberName = null)
	{
		_connection.Trace(StreamId, message, memberName);
	}

	private void AbortStream()
	{
		if (_requestContentLengthRemaining != 0L)
		{
			_stream.AbortWrite(268L);
		}
		if (_responseDataPayloadRemaining != -1)
		{
			_stream.AbortRead(268L);
		}
	}
}
