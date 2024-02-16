using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class Http3Connection : HttpConnectionBase
{
	private readonly HttpConnectionPool _pool;

	private readonly HttpAuthority _origin;

	private readonly HttpAuthority _authority;

	private readonly byte[] _altUsedEncodedHeader;

	private QuicConnection _connection;

	private Task _connectionClosedTask;

	private readonly Dictionary<QuicStream, Http3RequestStream> _activeRequests = new Dictionary<QuicStream, Http3RequestStream>();

	private long _lastProcessedStreamId = -1L;

	private QuicStream _clientControl;

	private int _maximumHeadersLength = int.MaxValue;

	private int _haveServerControlStream;

	private int _haveServerQpackDecodeStream;

	private int _haveServerQpackEncodeStream;

	private Exception _abortException;

	public HttpAuthority Authority => _authority;

	public HttpConnectionPool Pool => _pool;

	public byte[] AltUsedEncodedHeaderBytes => _altUsedEncodedHeader;

	public Exception AbortException => Volatile.Read(ref _abortException);

	private object SyncObj => _activeRequests;

	private bool ShuttingDown => _lastProcessedStreamId != -1;

	public Http3Connection(HttpConnectionPool pool, HttpAuthority origin, HttpAuthority authority, QuicConnection connection)
	{
		_pool = pool;
		_origin = origin;
		_authority = authority;
		_connection = connection;
		string text;
		if ((pool.Kind != 0 || authority.Port != 80) && (pool.Kind != HttpConnectionKind.Https || authority.Port != 443))
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 2, invariantCulture);
			handler.AppendFormatted(authority.IdnHost);
			handler.AppendLiteral(":");
			handler.AppendFormatted(authority.Port);
			text = string.Create(invariantCulture, ref handler);
		}
		else
		{
			text = authority.IdnHost;
		}
		string value = text;
		_altUsedEncodedHeader = QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReferenceToArray(KnownHeaders.AltUsed.Name, value);
		SendSettingsAsync();
		AcceptStreamsAsync();
	}

	public override void Dispose()
	{
		lock (SyncObj)
		{
			if (_lastProcessedStreamId == -1)
			{
				_lastProcessedStreamId = long.MaxValue;
				CheckForShutdown();
			}
		}
	}

	private void CheckForShutdown()
	{
		if (_activeRequests.Count != 0 || _connection == null)
		{
			return;
		}
		if (_connectionClosedTask == null)
		{
			_connectionClosedTask = _connection.CloseAsync(256L).AsTask();
		}
		QuicConnection connection = _connection;
		_connection = null;
		_connectionClosedTask.ContinueWith(delegate(Task closeTask)
		{
			if (closeTask.IsFaulted && System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"QuicConnection"} failed to close: {closeTask.Exception.InnerException}", "CheckForShutdown");
			}
			try
			{
				connection.Dispose();
			}
			catch (Exception value)
			{
				Trace($"{"QuicConnection"} failed to dispose: {value}", "CheckForShutdown");
			}
			if (_clientControl != null)
			{
				_clientControl.Dispose();
				_clientControl = null;
			}
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}

	public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		QuicStream quicStream = null;
		Http3RequestStream requestStream = null;
		ValueTask valueTask = default(ValueTask);
		try
		{
			while (true)
			{
				lock (SyncObj)
				{
					if (_connection != null)
					{
						if (_connection.GetRemoteAvailableBidirectionalStreamCount() <= 0)
						{
							valueTask = _connection.WaitForAvailableBidirectionalStreamsAsync(cancellationToken);
							goto IL_00ce;
						}
						quicStream = _connection.OpenBidirectionalStream();
						requestStream = new Http3RequestStream(request, this, quicStream);
						_activeRequests.Add(quicStream, requestStream);
					}
				}
				break;
				IL_00ce:
				await valueTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			if (quicStream == null)
			{
				throw new HttpRequestException(System.SR.net_http_request_aborted, null, RequestRetryType.RetryOnConnectionFailure);
			}
			requestStream.StreamId = quicStream.StreamId;
			bool flag;
			lock (SyncObj)
			{
				flag = _lastProcessedStreamId != -1 && requestStream.StreamId > _lastProcessedStreamId;
			}
			if (flag)
			{
				throw new HttpRequestException(System.SR.net_http_request_aborted, null, RequestRetryType.RetryOnConnectionFailure);
			}
			Task<HttpResponseMessage> task = requestStream.SendAsync(cancellationToken);
			requestStream = null;
			return await task.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (QuicConnectionAbortedException ex)
		{
			Abort(ex);
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_http3_connection_error, ex.ErrorCode), ex, RequestRetryType.RetryOnConnectionFailure);
		}
		finally
		{
			requestStream?.Dispose();
		}
	}

	internal Exception Abort(Exception abortException)
	{
		Exception ex = Interlocked.CompareExchange(ref _abortException, abortException, null);
		if (ex != null)
		{
			if (System.Net.NetEventSource.Log.IsEnabled() && ex != abortException)
			{
				Trace($"{"abortException"}=={abortException}", "Abort");
			}
			return ex;
		}
		_pool.InvalidateHttp3Connection(this);
		Http3ErrorCode errorCode = (abortException as Http3ProtocolException)?.ErrorCode ?? Http3ErrorCode.InternalError;
		lock (SyncObj)
		{
			if (_lastProcessedStreamId == -1)
			{
				_lastProcessedStreamId = long.MaxValue;
			}
			if (_connection != null && _connectionClosedTask == null)
			{
				_connectionClosedTask = _connection.CloseAsync((long)errorCode).AsTask();
			}
			CheckForShutdown();
			return abortException;
		}
	}

	private void OnServerGoAway(long lastProcessedStreamId)
	{
		_pool.InvalidateHttp3Connection(this);
		List<Http3RequestStream> list = new List<Http3RequestStream>();
		lock (SyncObj)
		{
			if (_lastProcessedStreamId != -1 && lastProcessedStreamId > _lastProcessedStreamId)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("HTTP/3 server sent GOAWAY with increasing stream ID. Retried requests may have been double-processed by server.", "OnServerGoAway");
				}
				return;
			}
			_lastProcessedStreamId = lastProcessedStreamId;
			foreach (KeyValuePair<QuicStream, Http3RequestStream> activeRequest in _activeRequests)
			{
				if (activeRequest.Value.StreamId > lastProcessedStreamId)
				{
					list.Add(activeRequest.Value);
				}
			}
			CheckForShutdown();
		}
		foreach (Http3RequestStream item in list)
		{
			item.GoAway();
		}
	}

	public void RemoveStream(QuicStream stream)
	{
		lock (SyncObj)
		{
			bool flag = _activeRequests.Remove(stream);
			if (ShuttingDown)
			{
				CheckForShutdown();
			}
		}
	}

	public override long GetIdleTicks(long nowTicks)
	{
		throw new NotImplementedException("We aren't scavenging HTTP3 connections yet");
	}

	public override void Trace(string message, [CallerMemberName] string memberName = null)
	{
		Trace(0L, message, memberName);
	}

	internal void Trace(long streamId, string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(_pool?.GetHashCode() ?? 0, GetHashCode(), (int)streamId, memberName, message);
	}

	private async Task SendSettingsAsync()
	{
		try
		{
			_clientControl = _connection.OpenUnidirectionalStream();
			await _clientControl.WriteAsync(_pool.Settings.Http3SettingsFrame, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception abortException)
		{
			Abort(abortException);
		}
	}

	public static byte[] BuildSettingsFrame(HttpConnectionSettings settings)
	{
		Span<byte> span = stackalloc byte[12];
		int num = VariableLengthIntegerHelper.WriteInteger(span.Slice(4), (long)settings._maxResponseHeadersLength * 1024L);
		int num2 = 1 + num;
		span[0] = 0;
		span[1] = 4;
		span[2] = (byte)num2;
		span[3] = 6;
		return span.Slice(0, 4 + num).ToArray();
	}

	private async Task AcceptStreamsAsync()
	{
		try
		{
			while (true)
			{
				ValueTask<QuicStream> valueTask;
				lock (SyncObj)
				{
					if (ShuttingDown)
					{
						break;
					}
					valueTask = _connection.AcceptStreamAsync(CancellationToken.None);
				}
				ProcessServerStreamAsync(await valueTask.ConfigureAwait(continueOnCapturedContext: false));
			}
		}
		catch (QuicOperationAbortedException)
		{
		}
		catch (Exception abortException)
		{
			Abort(abortException);
		}
	}

	private async Task ProcessServerStreamAsync(QuicStream stream)
	{
		System.Net.ArrayBuffer buffer = default(System.Net.ArrayBuffer);
		try
		{
			ConfiguredAsyncDisposable configuredAsyncDisposable = stream.ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				if (stream.CanWrite)
				{
					throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
				}
				buffer = new System.Net.ArrayBuffer(32, usePool: true);
				int num;
				try
				{
					num = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (QuicStreamAbortedException)
				{
					num = 0;
				}
				if (num == 0)
				{
					return;
				}
				buffer.Commit(num);
				switch (buffer.ActiveSpan[0])
				{
				case 0:
				{
					if (Interlocked.Exchange(ref _haveServerControlStream, 1) != 0)
					{
						throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
					}
					buffer.Discard(1);
					System.Net.ArrayBuffer buffer2 = buffer;
					buffer = default(System.Net.ArrayBuffer);
					await ProcessServerControlStreamAsync(stream, buffer2).ConfigureAwait(continueOnCapturedContext: false);
					return;
				}
				case 3:
					if (Interlocked.Exchange(ref _haveServerQpackDecodeStream, 1) != 0)
					{
						throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
					}
					buffer.Dispose();
					await stream.CopyToAsync(Stream.Null).ConfigureAwait(continueOnCapturedContext: false);
					return;
				case 2:
					if (Interlocked.Exchange(ref _haveServerQpackEncodeStream, 1) != 0)
					{
						throw new Http3ConnectionException(Http3ErrorCode.StreamCreationError);
					}
					buffer.Dispose();
					await stream.CopyToAsync(Stream.Null).ConfigureAwait(continueOnCapturedContext: false);
					return;
				case 1:
					throw new Http3ConnectionException(Http3ErrorCode.IdError);
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					long value;
					int bytesRead;
					while (!VariableLengthIntegerHelper.TryRead(buffer.ActiveSpan, out value, out bytesRead))
					{
						buffer.EnsureAvailableSpace(8);
						num = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
						if (num == 0)
						{
							value = -1L;
							break;
						}
						buffer.Commit(num);
					}
					System.Net.NetEventSource.Info(this, $"Ignoring server-initiated stream of unknown type {value}.", "ProcessServerStreamAsync");
				}
				stream.AbortWrite(259L);
			}
			finally
			{
				IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
				if (asyncDisposable != null)
				{
					await asyncDisposable.DisposeAsync();
				}
			}
		}
		catch (Exception abortException)
		{
			Abort(abortException);
		}
		finally
		{
			buffer.Dispose();
		}
	}

	private async Task ProcessServerControlStreamAsync(QuicStream stream, System.Net.ArrayBuffer buffer)
	{
		using (buffer)
		{
			Http3FrameType? http3FrameType;
			long settingsPayloadLength2;
			(http3FrameType, settingsPayloadLength2) = await ReadFrameEnvelopeAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (!http3FrameType.HasValue)
			{
				throw new Http3ConnectionException(Http3ErrorCode.ClosedCriticalStream);
			}
			if (http3FrameType != Http3FrameType.Settings)
			{
				throw new Http3ConnectionException(Http3ErrorCode.MissingSettings);
			}
			await ProcessSettingsFrameAsync(settingsPayloadLength2).ConfigureAwait(continueOnCapturedContext: false);
			while (true)
			{
				(http3FrameType, settingsPayloadLength2) = await ReadFrameEnvelopeAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (!http3FrameType.HasValue)
				{
					break;
				}
				Http3FrameType valueOrDefault = http3FrameType.GetValueOrDefault();
				if ((ulong)valueOrDefault <= 13uL)
				{
					switch (valueOrDefault)
					{
					case Http3FrameType.GoAway:
						await ProcessGoAwayFrameAsync(settingsPayloadLength2).ConfigureAwait(continueOnCapturedContext: false);
						continue;
					case Http3FrameType.Settings:
						throw new Http3ConnectionException(Http3ErrorCode.UnexpectedFrame);
					case Http3FrameType.Data:
					case Http3FrameType.Headers:
					case Http3FrameType.ReservedHttp2Priority:
					case Http3FrameType.ReservedHttp2Ping:
					case Http3FrameType.ReservedHttp2WindowUpdate:
					case Http3FrameType.ReservedHttp2Continuation:
					case Http3FrameType.MaxPushId:
						throw new Http3ConnectionException(Http3ErrorCode.UnexpectedFrame);
					case Http3FrameType.CancelPush:
					case Http3FrameType.PushPromise:
						throw new Http3ConnectionException(Http3ErrorCode.IdError);
					}
				}
				await SkipUnknownPayloadAsync(http3FrameType.GetValueOrDefault(), settingsPayloadLength2).ConfigureAwait(continueOnCapturedContext: false);
			}
			bool shuttingDown;
			lock (SyncObj)
			{
				shuttingDown = ShuttingDown;
			}
			if (!shuttingDown)
			{
				throw new Http3ConnectionException(Http3ErrorCode.ClosedCriticalStream);
			}
		}
		async ValueTask ProcessGoAwayFrameAsync(long goawayPayloadLength)
		{
			long value;
			int bytesRead;
			while (!VariableLengthIntegerHelper.TryRead(buffer.ActiveSpan, out value, out bytesRead))
			{
				buffer.EnsureAvailableSpace(8);
				bytesRead = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (bytesRead == 0)
				{
					throw new Http3ConnectionException(Http3ErrorCode.FrameError);
				}
				buffer.Commit(bytesRead);
			}
			buffer.Discard(bytesRead);
			if (bytesRead != goawayPayloadLength)
			{
				throw new Http3ConnectionException(Http3ErrorCode.FrameError);
			}
			OnServerGoAway(value);
		}
		async ValueTask ProcessSettingsFrameAsync(long settingsPayloadLength)
		{
			while (settingsPayloadLength != 0L)
			{
				long a;
				long b;
				int bytesRead2;
				while (!Http3Frame.TryReadIntegerPair(buffer.ActiveSpan, out a, out b, out bytesRead2))
				{
					buffer.EnsureAvailableSpace(16);
					bytesRead2 = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
					if (bytesRead2 == 0)
					{
						throw new Http3ConnectionException(Http3ErrorCode.FrameError);
					}
					buffer.Commit(bytesRead2);
				}
				settingsPayloadLength -= bytesRead2;
				if (settingsPayloadLength < 0)
				{
					throw new Http3ConnectionException(Http3ErrorCode.FrameError);
				}
				buffer.Discard(bytesRead2);
				switch ((Http3SettingType)a)
				{
				case Http3SettingType.MaxHeaderListSize:
					_maximumHeadersLength = (int)Math.Min(b, 2147483647L);
					break;
				case Http3SettingType.ReservedHttp2EnablePush:
				case Http3SettingType.ReservedHttp2MaxConcurrentStreams:
				case Http3SettingType.ReservedHttp2InitialWindowSize:
				case Http3SettingType.ReservedHttp2MaxFrameSize:
					throw new Http3ConnectionException(Http3ErrorCode.SettingsError);
				}
			}
		}
		async ValueTask<(Http3FrameType? frameType, long payloadLength)> ReadFrameEnvelopeAsync()
		{
			long a2;
			long b2;
			int bytesRead3;
			while (!Http3Frame.TryReadIntegerPair(buffer.ActiveSpan, out a2, out b2, out bytesRead3))
			{
				buffer.EnsureAvailableSpace(16);
				bytesRead3 = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (bytesRead3 == 0)
				{
					if (buffer.ActiveLength == 0)
					{
						return (null, 0L);
					}
					throw new Http3ConnectionException(Http3ErrorCode.FrameError);
				}
				buffer.Commit(bytesRead3);
			}
			buffer.Discard(bytesRead3);
			return ((Http3FrameType)a2, b2);
		}
		async ValueTask SkipUnknownPayloadAsync(Http3FrameType frameType, long payloadLength)
		{
			while (payloadLength != 0L)
			{
				if (buffer.ActiveLength == 0)
				{
					int num = await stream.ReadAsync(buffer.AvailableMemory, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
					if (num == 0)
					{
						throw new Http3ConnectionException(Http3ErrorCode.FrameError);
					}
					buffer.Commit(num);
				}
				long num2 = Math.Min(payloadLength, buffer.ActiveLength);
				buffer.Discard((int)num2);
				payloadLength -= num2;
			}
		}
	}
}
