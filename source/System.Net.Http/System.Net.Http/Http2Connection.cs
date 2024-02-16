using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http;

internal sealed class Http2Connection : HttpConnectionBase
{
	internal enum KeepAliveState
	{
		None,
		PingSent
	}

	private sealed class NopHeadersHandler : IHttpHeadersHandler
	{
		public static readonly NopHeadersHandler Instance = new NopHeadersHandler();

		void IHttpHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
		{
		}

		void IHttpHeadersHandler.OnStaticIndexedHeader(int index)
		{
		}

		void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
		{
		}
	}

	private abstract class WriteQueueEntry : TaskCompletionSource
	{
		private readonly CancellationTokenRegistration _cancellationRegistration;

		public int WriteBytes { get; }

		public WriteQueueEntry(int writeBytes, CancellationToken cancellationToken)
			: base(TaskCreationOptions.RunContinuationsAsynchronously)
		{
			WriteBytes = writeBytes;
			_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken cancellationToken)
			{
				bool flag = ((WriteQueueEntry)s).TrySetCanceled(cancellationToken);
			}, this);
		}

		public bool TryDisableCancellation()
		{
			_cancellationRegistration.Dispose();
			return !base.Task.IsCanceled;
		}

		public abstract bool InvokeWriteAction(Memory<byte> writeBuffer);
	}

	private sealed class WriteQueueEntry<T> : WriteQueueEntry
	{
		private readonly T _state;

		private readonly Func<T, Memory<byte>, bool> _writeAction;

		public WriteQueueEntry(int writeBytes, T state, Func<T, Memory<byte>, bool> writeAction, CancellationToken cancellationToken)
			: base(writeBytes, cancellationToken)
		{
			_state = state;
			_writeAction = writeAction;
		}

		public override bool InvokeWriteAction(Memory<byte> writeBuffer)
		{
			return _writeAction(_state, writeBuffer);
		}
	}

	private enum FrameType : byte
	{
		Data = 0,
		Headers = 1,
		Priority = 2,
		RstStream = 3,
		Settings = 4,
		PushPromise = 5,
		Ping = 6,
		GoAway = 7,
		WindowUpdate = 8,
		Continuation = 9,
		AltSvc = 10,
		Last = 10
	}

	private readonly struct FrameHeader
	{
		public readonly int PayloadLength;

		public readonly FrameType Type;

		public readonly FrameFlags Flags;

		public readonly int StreamId;

		public bool PaddedFlag => (Flags & FrameFlags.Padded) != 0;

		public bool AckFlag => (Flags & FrameFlags.EndStream) != 0;

		public bool EndHeadersFlag => (Flags & FrameFlags.EndHeaders) != 0;

		public bool EndStreamFlag => (Flags & FrameFlags.EndStream) != 0;

		public bool PriorityFlag => (Flags & FrameFlags.Priority) != 0;

		public FrameHeader(int payloadLength, FrameType type, FrameFlags flags, int streamId)
		{
			PayloadLength = payloadLength;
			Type = type;
			Flags = flags;
			StreamId = streamId;
		}

		public static FrameHeader ReadFrom(ReadOnlySpan<byte> buffer)
		{
			FrameFlags flags = (FrameFlags)buffer[4];
			int payloadLength = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
			FrameType type = (FrameType)buffer[3];
			int streamId = (int)(BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(5)) & 0x7FFFFFFF);
			return new FrameHeader(payloadLength, type, flags, streamId);
		}

		public static void WriteTo(Span<byte> destination, int payloadLength, FrameType type, FrameFlags flags, int streamId)
		{
			BinaryPrimitives.WriteInt32BigEndian(destination.Slice(5), streamId);
			destination[4] = (byte)flags;
			destination[0] = (byte)((payloadLength & 0xFF0000) >> 16);
			destination[1] = (byte)((payloadLength & 0xFF00) >> 8);
			destination[2] = (byte)((uint)payloadLength & 0xFFu);
			destination[3] = (byte)type;
		}

		public override string ToString()
		{
			return $"StreamId={StreamId}; Type={Type}; Flags={Flags}; PayloadLength={PayloadLength}";
		}
	}

	[Flags]
	private enum FrameFlags : byte
	{
		None = 0,
		EndStream = 1,
		Ack = 1,
		EndHeaders = 4,
		Padded = 8,
		Priority = 0x20,
		ValidBits = 0x2D
	}

	private enum SettingId : ushort
	{
		HeaderTableSize = 1,
		EnablePush,
		MaxConcurrentStreams,
		InitialWindowSize,
		MaxFrameSize,
		MaxHeaderListSize
	}

	private sealed class Http2Stream : IValueTaskSource, IHttpHeadersHandler, IHttpTrace
	{
		private enum ResponseProtocolState : byte
		{
			ExpectingStatus,
			ExpectingIgnoredHeaders,
			ExpectingHeaders,
			ExpectingData,
			ExpectingTrailingHeaders,
			Complete,
			Aborted
		}

		private enum StreamCompletionState : byte
		{
			InProgress,
			Completed,
			Failed
		}

		private sealed class Http2ReadStream : HttpBaseStream
		{
			private Http2Stream _http2Stream;

			private readonly HttpResponseMessage _responseMessage;

			public override bool CanRead => _http2Stream != null;

			public override bool CanWrite => false;

			public Http2ReadStream(Http2Stream http2Stream)
			{
				_http2Stream = http2Stream;
				_responseMessage = _http2Stream._response;
			}

			~Http2ReadStream()
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					_http2Stream?.Trace("", "Finalize");
				}
				try
				{
					Dispose(disposing: false);
				}
				catch (Exception value)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						_http2Stream?.Trace($"Error: {value}", "Finalize");
					}
				}
			}

			protected override void Dispose(bool disposing)
			{
				Http2Stream http2Stream = Interlocked.Exchange(ref _http2Stream, null);
				if (http2Stream != null)
				{
					http2Stream.CloseResponseBody();
					base.Dispose(disposing);
				}
			}

			public override int Read(Span<byte> destination)
			{
				Http2Stream http2Stream = _http2Stream ?? throw new ObjectDisposedException("Http2ReadStream");
				return http2Stream.ReadData(destination, _responseMessage);
			}

			public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
			{
				Http2Stream http2Stream = _http2Stream;
				if (http2Stream == null)
				{
					return ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("Http2ReadStream")));
				}
				if (cancellationToken.IsCancellationRequested)
				{
					return ValueTask.FromCanceled<int>(cancellationToken);
				}
				return http2Stream.ReadDataAsync(destination, _responseMessage, cancellationToken);
			}

			public override void CopyTo(Stream destination, int bufferSize)
			{
				Stream.ValidateCopyToArguments(destination, bufferSize);
				Http2Stream http2Stream = _http2Stream ?? throw ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("Http2ReadStream"));
				http2Stream.CopyTo(_responseMessage, destination, bufferSize);
			}

			public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
			{
				Stream.ValidateCopyToArguments(destination, bufferSize);
				Http2Stream http2Stream = _http2Stream;
				if (http2Stream != null)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						return http2Stream.CopyToAsync(_responseMessage, destination, bufferSize, cancellationToken);
					}
					return Task.FromCanceled<int>(cancellationToken);
				}
				return Task.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("Http2ReadStream")));
			}

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				throw new NotSupportedException(System.SR.net_http_content_readonly_stream);
			}

			public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class Http2WriteStream : HttpBaseStream
		{
			private Http2Stream _http2Stream;

			public long BytesWritten { get; private set; }

			public override bool CanRead => false;

			public override bool CanWrite => _http2Stream != null;

			public Http2WriteStream(Http2Stream http2Stream)
			{
				_http2Stream = http2Stream;
			}

			protected override void Dispose(bool disposing)
			{
				Http2Stream http2Stream = Interlocked.Exchange(ref _http2Stream, null);
				if (http2Stream != null)
				{
					base.Dispose(disposing);
				}
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
				BytesWritten += buffer.Length;
				return _http2Stream?.SendDataAsync(buffer, cancellationToken) ?? ValueTask.FromException(new ObjectDisposedException("Http2WriteStream"));
			}

			public override Task FlushAsync(CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled(cancellationToken);
				}
				Http2Stream http2Stream = _http2Stream;
				if (http2Stream == null)
				{
					return Task.CompletedTask;
				}
				return http2Stream._connection.FlushAsync(cancellationToken);
			}
		}

		private readonly Http2Connection _connection;

		private readonly HttpRequestMessage _request;

		private HttpResponseMessage _response;

		private HttpResponseHeaders _trailers;

		private System.Net.MultiArrayBuffer _responseBuffer;

		private Http2StreamWindowManager _windowManager;

		private CreditWaiter _creditWaiter;

		private int _availableCredit;

		private readonly object _creditSyncObject = new object();

		private StreamCompletionState _requestCompletionState;

		private StreamCompletionState _responseCompletionState;

		private ResponseProtocolState _responseProtocolState;

		private Exception _resetException;

		private bool _canRetry;

		private bool _requestBodyAbandoned;

		private ManualResetValueTaskSourceCore<bool> _waitSource = new ManualResetValueTaskSourceCore<bool>
		{
			RunContinuationsAsynchronously = true
		};

		private CancellationTokenRegistration _waitSourceCancellation;

		private bool _hasWaiter;

		private readonly CancellationTokenSource _requestBodyCancellationSource;

		private readonly TaskCompletionSource<bool> _expect100ContinueWaiter;

		private int _headerBudgetRemaining;

		private static readonly int[] s_hpackStaticStatusCodeTable = new int[7] { 200, 204, 206, 304, 400, 404, 500 };

		private static readonly (HeaderDescriptor descriptor, byte[] value)[] s_hpackStaticHeaderTable = new(HeaderDescriptor, byte[])[47]
		{
			(KnownHeaders.AcceptCharset.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.AcceptEncoding.Descriptor, Encoding.ASCII.GetBytes("gzip, deflate")),
			(KnownHeaders.AcceptLanguage.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.AcceptRanges.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Accept.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.AccessControlAllowOrigin.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Age.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Allow.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Authorization.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.CacheControl.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentDisposition.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentEncoding.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentLanguage.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentLength.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentLocation.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentRange.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentType.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Cookie.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Date.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ETag.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Expect.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Expires.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.From.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Host.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfMatch.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfModifiedSince.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfNoneMatch.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfRange.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfUnmodifiedSince.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.LastModified.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Link.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Location.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.MaxForwards.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ProxyAuthenticate.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ProxyAuthorization.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Range.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Referer.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Refresh.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.RetryAfter.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Server.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.SetCookie.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.StrictTransportSecurity.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.TransferEncoding.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.UserAgent.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Vary.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Via.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.WWWAuthenticate.Descriptor, Array.Empty<byte>())
		};

		private static ReadOnlySpan<byte> StatusHeaderName => ":status"u8;

		private object SyncObject => this;

		public int StreamId { get; private set; }

		public bool SendRequestFinished => _requestCompletionState != StreamCompletionState.InProgress;

		public bool ExpectResponseData => _responseProtocolState == ResponseProtocolState.ExpectingData;

		public Http2Connection Connection => _connection;

		public Http2Stream(HttpRequestMessage request, Http2Connection connection)
		{
			_request = request;
			_connection = connection;
			_requestCompletionState = StreamCompletionState.InProgress;
			_responseCompletionState = StreamCompletionState.InProgress;
			_responseProtocolState = ResponseProtocolState.ExpectingStatus;
			_responseBuffer = new System.Net.MultiArrayBuffer(1024);
			_windowManager = new Http2StreamWindowManager(connection, this);
			_headerBudgetRemaining = connection._pool.Settings._maxResponseHeadersLength * 1024;
			if (_request.Content == null)
			{
				_requestCompletionState = StreamCompletionState.Completed;
			}
			else
			{
				_requestBodyCancellationSource = new CancellationTokenSource();
				if (_request.HasHeaders && _request.Headers.ExpectContinue == true)
				{
					_expect100ContinueWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				}
			}
			_response = new HttpResponseMessage
			{
				Version = HttpVersion.Version20,
				RequestMessage = _request,
				Content = new HttpConnectionResponseContent()
			};
		}

		public void Initialize(int streamId, int initialWindowSize)
		{
			StreamId = streamId;
			_availableCredit = initialWindowSize;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{_request}, {"initialWindowSize"}={initialWindowSize}", "Initialize");
			}
		}

		public HttpResponseMessage GetAndClearResponse()
		{
			HttpResponseMessage response = _response;
			_response = null;
			return response;
		}

		public async Task SendRequestBodyAsync(CancellationToken cancellationToken)
		{
			if (_request.Content == null)
			{
				return;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{_request.Content}", "SendRequestBodyAsync");
			}
			CancellationTokenRegistration linkedRegistration = default(CancellationTokenRegistration);
			try
			{
				bool flag = true;
				if (_expect100ContinueWaiter != null)
				{
					linkedRegistration = RegisterRequestBodyCancellation(cancellationToken);
					flag = await WaitFor100ContinueAsync(_requestBodyCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (flag)
				{
					using Http2WriteStream writeStream = new Http2WriteStream(this);
					if (HttpTelemetry.Log.IsEnabled())
					{
						HttpTelemetry.Log.RequestContentStart();
					}
					ValueTask valueTask = _request.Content.InternalCopyToAsync(writeStream, null, _requestBodyCancellationSource.Token);
					if (valueTask.IsCompleted)
					{
						valueTask.GetAwaiter().GetResult();
					}
					else
					{
						if (linkedRegistration.Equals(default(CancellationTokenRegistration)))
						{
							linkedRegistration = RegisterRequestBodyCancellation(cancellationToken);
						}
						await valueTask.ConfigureAwait(continueOnCapturedContext: false);
					}
					if (HttpTelemetry.Log.IsEnabled())
					{
						HttpTelemetry.Log.RequestContentStop(writeStream.BytesWritten);
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Finished sending request body.", "SendRequestBodyAsync");
				}
			}
			catch (Exception value)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Failed to send request body: {value}", "SendRequestBodyAsync");
				}
				bool flag2;
				lock (SyncObject)
				{
					if (_requestBodyAbandoned)
					{
						_requestCompletionState = StreamCompletionState.Completed;
						Complete();
						return;
					}
					(bool signalWaiter, bool sendReset) tuple = CancelResponseBody();
					(flag2, _) = tuple;
					_ = tuple.sendReset;
					_requestCompletionState = StreamCompletionState.Failed;
					SendReset();
					Complete();
				}
				if (flag2)
				{
					_waitSource.SetResult(result: true);
				}
				throw;
			}
			finally
			{
				linkedRegistration.Dispose();
			}
			bool flag3 = false;
			lock (SyncObject)
			{
				_requestCompletionState = StreamCompletionState.Completed;
				bool flag4 = false;
				if (_responseCompletionState != 0)
				{
					flag3 = _responseCompletionState == StreamCompletionState.Failed;
					flag4 = true;
				}
				if (flag3)
				{
					SendReset();
				}
				else
				{
					_connection.LogExceptions(_connection.SendEndStreamAsync(StreamId));
				}
				if (flag4)
				{
					Complete();
				}
			}
		}

		public async ValueTask<bool> WaitFor100ContinueAsync(CancellationToken cancellationToken)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Waiting to send request body content for 100-Continue.", "WaitFor100ContinueAsync");
			}
			TaskCompletionSource<bool> expect100ContinueWaiter = _expect100ContinueWaiter;
			using (cancellationToken.UnsafeRegister(delegate(object s)
			{
				((TaskCompletionSource<bool>)s).TrySetResult(result: false);
			}, expect100ContinueWaiter))
			{
				ConfiguredAsyncDisposable configuredAsyncDisposable = new Timer(delegate(object s)
				{
					Http2Stream http2Stream = (Http2Stream)s;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						http2Stream.Trace("100-Continue timer expired.", "WaitFor100ContinueAsync");
					}
					http2Stream._expect100ContinueWaiter?.TrySetResult(result: true);
				}, this, _connection._pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan).ConfigureAwait(continueOnCapturedContext: false);
				bool result;
				try
				{
					bool flag = await expect100ContinueWaiter.Task.ConfigureAwait(continueOnCapturedContext: false);
					CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
					result = flag;
				}
				finally
				{
					IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
					if (asyncDisposable != null)
					{
						await asyncDisposable.DisposeAsync();
					}
				}
				return result;
			}
		}

		private void SendReset()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Stream reset. Request={_requestCompletionState}, Response={_responseCompletionState}.", "SendReset");
			}
			if (_resetException == null)
			{
				_connection.LogExceptions(_connection.SendRstStreamAsync(StreamId, Http2ProtocolErrorCode.Cancel));
			}
		}

		private void Complete()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Stream complete. Request={_requestCompletionState}, Response={_responseCompletionState}.", "Complete");
			}
			_connection.RemoveStream(this);
			lock (_creditSyncObject)
			{
				CreditWaiter creditWaiter = _creditWaiter;
				if (creditWaiter != null)
				{
					creditWaiter.Dispose();
					_creditWaiter = null;
				}
			}
		}

		private void Cancel()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("", "Cancel");
			}
			CancellationTokenSource cancellationTokenSource = null;
			bool flag = false;
			bool flag2 = false;
			lock (SyncObject)
			{
				if (_requestCompletionState == StreamCompletionState.InProgress)
				{
					cancellationTokenSource = _requestBodyCancellationSource;
				}
				(flag, flag2) = CancelResponseBody();
			}
			cancellationTokenSource?.Cancel();
			lock (SyncObject)
			{
				if (flag2)
				{
					SendReset();
					Complete();
				}
			}
			if (flag)
			{
				_waitSource.SetResult(result: true);
			}
		}

		private (bool signalWaiter, bool sendReset) CancelResponseBody()
		{
			bool item = false;
			if (_responseCompletionState == StreamCompletionState.InProgress)
			{
				_responseCompletionState = StreamCompletionState.Failed;
				if (_requestCompletionState != 0)
				{
					item = true;
				}
			}
			_responseBuffer.DiscardAll();
			_responseProtocolState = ResponseProtocolState.Aborted;
			bool hasWaiter = _hasWaiter;
			_hasWaiter = false;
			return (signalWaiter: hasWaiter, sendReset: item);
		}

		public void OnWindowUpdate(int amount)
		{
			lock (_creditSyncObject)
			{
				checked
				{
					_availableCredit += amount;
				}
				if (_availableCredit > 0 && _creditWaiter != null)
				{
					int num = Math.Min(_availableCredit, _creditWaiter.Amount);
					if (_creditWaiter.TrySetResult(num))
					{
						_availableCredit -= num;
					}
				}
			}
		}

		void IHttpHeadersHandler.OnStaticIndexedHeader(int index)
		{
			if (index <= 7)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Invalid request pseudo-header ID {index}.", "OnStaticIndexedHeader");
				}
				throw new HttpRequestException(System.SR.net_http_invalid_response);
			}
			if (index <= 14)
			{
				int statusCode = s_hpackStaticStatusCodeTable[index - 8];
				OnStatus(statusCode);
			}
			else
			{
				var (descriptor, array) = s_hpackStaticHeaderTable[index - 15];
				OnHeader(descriptor, array);
			}
		}

		void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
		{
			if (index <= 7)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Invalid request pseudo-header ID {index}.", "OnStaticIndexedHeader");
				}
				throw new HttpRequestException(System.SR.net_http_invalid_response);
			}
			if (index <= 14)
			{
				int statusCode = HttpConnectionBase.ParseStatusCode(value);
				OnStatus(statusCode);
			}
			else
			{
				HeaderDescriptor item = s_hpackStaticHeaderTable[index - 15].descriptor;
				OnHeader(item, value);
			}
		}

		private void AdjustHeaderBudget(int amount)
		{
			_headerBudgetRemaining -= amount;
			if (_headerBudgetRemaining < 0)
			{
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_response_headers_exceeded_length, (long)_connection._pool.Settings._maxResponseHeadersLength * 1024L));
			}
		}

		private void OnStatus(int statusCode)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Status code is {statusCode}", "OnStatus");
			}
			AdjustHeaderBudget(10);
			lock (SyncObject)
			{
				if (_responseProtocolState == ResponseProtocolState.Aborted)
				{
					return;
				}
				if (_responseProtocolState == ResponseProtocolState.ExpectingHeaders)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("Received extra status header.", "OnStatus");
					}
					throw new HttpRequestException(System.SR.net_http_invalid_response_multiple_status_codes);
				}
				if (_responseProtocolState != 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Status pseudo-header received in {_responseProtocolState} state.", "OnStatus");
					}
					throw new HttpRequestException(System.SR.net_http_invalid_response_pseudo_header_in_trailer);
				}
				_response.StatusCode = (HttpStatusCode)statusCode;
				if (statusCode < 200)
				{
					_responseProtocolState = ResponseProtocolState.ExpectingIgnoredHeaders;
					if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueWaiter != null)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace("Received 100-Continue status.", "OnStatus");
						}
						_expect100ContinueWaiter.TrySetResult(result: true);
					}
					return;
				}
				_responseProtocolState = ResponseProtocolState.ExpectingHeaders;
				if (_expect100ContinueWaiter != null)
				{
					bool result = statusCode < 300;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Expecting 100 Continue but received final status {statusCode}.", "OnStatus");
					}
					_expect100ContinueWaiter.TrySetResult(result);
				}
			}
		}

		private void OnHeader(HeaderDescriptor descriptor, ReadOnlySpan<byte> value)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace(descriptor.Name + ": " + Encoding.ASCII.GetString(value), "OnHeader");
			}
			AdjustHeaderBudget(descriptor.Name.Length + value.Length);
			lock (SyncObject)
			{
				if (_responseProtocolState == ResponseProtocolState.Aborted || _responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders)
				{
					return;
				}
				if (_responseProtocolState != ResponseProtocolState.ExpectingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingTrailingHeaders)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("Received header before status.", "OnHeader");
					}
					throw new HttpRequestException(System.SR.net_http_invalid_response);
				}
				Encoding valueEncoding = _connection._pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _request);
				if (_responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
				{
					string headerValue = descriptor.GetHeaderValue(value, valueEncoding);
					_trailers.TryAddWithoutValidation(((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, headerValue);
				}
				else if ((descriptor.HeaderType & HttpHeaderType.Content) == HttpHeaderType.Content)
				{
					string headerValue2 = descriptor.GetHeaderValue(value, valueEncoding);
					_response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue2);
				}
				else
				{
					string responseHeaderValueWithCaching = _connection.GetResponseHeaderValueWithCaching(descriptor, value, valueEncoding);
					_response.Headers.TryAddWithoutValidation(((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, responseHeaderValueWithCaching);
				}
			}
		}

		public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
		{
			if (name[0] == 58)
			{
				if (!name.SequenceEqual(StatusHeaderName))
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("Invalid response pseudo-header '" + Encoding.ASCII.GetString(name) + "'.", "OnHeader");
					}
					throw new HttpRequestException(System.SR.net_http_invalid_response);
				}
				int statusCode = HttpConnectionBase.ParseStatusCode(value);
				OnStatus(statusCode);
			}
			else
			{
				if (!HeaderDescriptor.TryGet(name, out var descriptor))
				{
					throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
				}
				OnHeader(descriptor, value);
			}
		}

		public void OnHeadersStart()
		{
			lock (SyncObject)
			{
				switch (_responseProtocolState)
				{
				case ResponseProtocolState.ExpectingData:
					_responseProtocolState = ResponseProtocolState.ExpectingTrailingHeaders;
					if (_trailers == null)
					{
						_trailers = new HttpResponseHeaders(containsTrailingHeaders: true);
					}
					break;
				default:
					ThrowProtocolError();
					break;
				case ResponseProtocolState.ExpectingStatus:
				case ResponseProtocolState.Aborted:
					break;
				}
			}
		}

		public void OnHeadersComplete(bool endStream)
		{
			bool hasWaiter;
			lock (SyncObject)
			{
				switch (_responseProtocolState)
				{
				case ResponseProtocolState.Aborted:
					return;
				case ResponseProtocolState.ExpectingHeaders:
					_responseProtocolState = (endStream ? ResponseProtocolState.Complete : ResponseProtocolState.ExpectingData);
					break;
				case ResponseProtocolState.ExpectingTrailingHeaders:
					if (!endStream)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace("Trailing headers received without endStream", "OnHeadersComplete");
						}
						ThrowProtocolError();
					}
					_responseProtocolState = ResponseProtocolState.Complete;
					break;
				case ResponseProtocolState.ExpectingIgnoredHeaders:
					if (endStream)
					{
						ThrowProtocolError();
					}
					_responseProtocolState = ResponseProtocolState.ExpectingStatus;
					return;
				default:
					ThrowProtocolError();
					break;
				}
				if (endStream)
				{
					_responseCompletionState = StreamCompletionState.Completed;
					if (_requestCompletionState == StreamCompletionState.Completed)
					{
						Complete();
					}
				}
				if (_responseProtocolState == ResponseProtocolState.ExpectingData)
				{
					_windowManager.Start();
				}
				hasWaiter = _hasWaiter;
				_hasWaiter = false;
			}
			if (hasWaiter)
			{
				_waitSource.SetResult(result: true);
			}
		}

		public void OnResponseData(ReadOnlySpan<byte> buffer, bool endStream)
		{
			bool hasWaiter;
			lock (SyncObject)
			{
				switch (_responseProtocolState)
				{
				case ResponseProtocolState.Aborted:
					return;
				default:
					ThrowProtocolError();
					break;
				case ResponseProtocolState.ExpectingData:
					break;
				}
				if (_responseBuffer.ActiveMemory.Length + buffer.Length > _windowManager.StreamWindowSize)
				{
					ThrowProtocolError(Http2ProtocolErrorCode.FlowControlError);
				}
				_responseBuffer.EnsureAvailableSpace(buffer.Length);
				_responseBuffer.AvailableMemory.CopyFrom(buffer);
				_responseBuffer.Commit(buffer.Length);
				if (endStream)
				{
					_responseProtocolState = ResponseProtocolState.Complete;
					_responseCompletionState = StreamCompletionState.Completed;
					if (_requestCompletionState == StreamCompletionState.Completed)
					{
						Complete();
					}
				}
				hasWaiter = _hasWaiter;
				_hasWaiter = false;
			}
			if (hasWaiter)
			{
				_waitSource.SetResult(result: true);
			}
		}

		public void OnReset(Exception resetException, Http2ProtocolErrorCode? resetStreamErrorCode = null, bool canRetry = false)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"resetException"}={resetException}, {"resetStreamErrorCode"}={resetStreamErrorCode}", "OnReset");
			}
			bool flag = false;
			CancellationTokenSource cancellationTokenSource = null;
			lock (SyncObject)
			{
				if ((_requestCompletionState == StreamCompletionState.Completed && _responseCompletionState == StreamCompletionState.Completed) || _resetException != null)
				{
					return;
				}
				if (canRetry && _responseProtocolState != 0)
				{
					canRetry = false;
				}
				if (resetStreamErrorCode == Http2ProtocolErrorCode.NoError && _responseCompletionState == StreamCompletionState.Completed)
				{
					if (_requestCompletionState == StreamCompletionState.InProgress)
					{
						_requestBodyAbandoned = true;
						cancellationTokenSource = _requestBodyCancellationSource;
					}
				}
				else
				{
					_resetException = resetException;
					_canRetry = canRetry;
					flag = true;
				}
			}
			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Cancel();
			}
			else
			{
				Cancel();
			}
		}

		private void CheckResponseBodyState()
		{
			Exception resetException = _resetException;
			if (resetException != null)
			{
				if (_canRetry)
				{
					ThrowRetry(System.SR.net_http_request_aborted, resetException);
				}
				ThrowRequestAborted(resetException);
			}
			if (_responseProtocolState == ResponseProtocolState.Aborted)
			{
				ThrowRequestAborted();
			}
		}

		private (bool wait, bool isEmptyResponse) TryEnsureHeaders()
		{
			lock (SyncObject)
			{
				CheckResponseBodyState();
				if (_responseProtocolState == ResponseProtocolState.ExpectingHeaders || _responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders || _responseProtocolState == ResponseProtocolState.ExpectingStatus)
				{
					_hasWaiter = true;
					_waitSource.Reset();
					return (wait: true, isEmptyResponse: false);
				}
				if (_responseProtocolState == ResponseProtocolState.ExpectingData || _responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
				{
					return (wait: false, isEmptyResponse: false);
				}
				return (wait: false, isEmptyResponse: _responseBuffer.IsEmpty);
			}
		}

		public async Task ReadResponseHeadersAsync(CancellationToken cancellationToken)
		{
			bool flag2;
			try
			{
				if (HttpTelemetry.Log.IsEnabled())
				{
					HttpTelemetry.Log.ResponseHeadersStart();
				}
				bool flag;
				(flag, flag2) = TryEnsureHeaders();
				if (flag)
				{
					await WaitForDataAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					(bool wait, bool isEmptyResponse) tuple2 = TryEnsureHeaders();
					_ = tuple2.wait;
					flag2 = tuple2.isEmptyResponse;
				}
				if (HttpTelemetry.Log.IsEnabled())
				{
					HttpTelemetry.Log.ResponseHeadersStop();
				}
			}
			catch
			{
				Cancel();
				throw;
			}
			HttpConnectionResponseContent httpConnectionResponseContent = (HttpConnectionResponseContent)_response.Content;
			if (flag2)
			{
				MoveTrailersToResponseMessage(_response);
				httpConnectionResponseContent.SetStream(EmptyReadStream.Instance);
			}
			else
			{
				httpConnectionResponseContent.SetStream(new Http2ReadStream(this));
			}
			if (_connection._pool.Settings._useCookies)
			{
				CookieHelper.ProcessReceivedCookies(_response, _connection._pool.Settings._cookieContainer);
			}
		}

		private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer, bool partOfSyncRead = false)
		{
			lock (SyncObject)
			{
				CheckResponseBodyState();
				if (!_responseBuffer.IsEmpty)
				{
					System.Net.MultiMemory activeMemory = _responseBuffer.ActiveMemory;
					int num = Math.Min(buffer.Length, activeMemory.Length);
					activeMemory.Slice(0, num).CopyTo(buffer);
					_responseBuffer.Discard(num);
					return (wait: false, bytesRead: num);
				}
				if (_responseProtocolState == ResponseProtocolState.Complete)
				{
					return (wait: false, bytesRead: 0);
				}
				_hasWaiter = true;
				_waitSource.Reset();
				_waitSource.RunContinuationsAsynchronously = !partOfSyncRead;
				return (wait: true, bytesRead: 0);
			}
		}

		public int ReadData(Span<byte> buffer, HttpResponseMessage responseMessage)
		{
			if (buffer.Length == 0)
			{
				return 0;
			}
			int num;
			bool flag;
			(flag, num) = TryReadFromBuffer(buffer, partOfSyncRead: true);
			if (flag)
			{
				WaitForData();
				(flag, num) = TryReadFromBuffer(buffer, partOfSyncRead: true);
			}
			if (num != 0)
			{
				_windowManager.AdjustWindow(num, this);
			}
			else
			{
				MoveTrailersToResponseMessage(responseMessage);
			}
			return num;
		}

		public async ValueTask<int> ReadDataAsync(Memory<byte> buffer, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
		{
			if (buffer.Length == 0)
			{
				return 0;
			}
			var (flag, num) = TryReadFromBuffer(buffer.Span);
			if (flag)
			{
				await WaitForDataAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				(bool wait, int bytesRead) tuple2 = TryReadFromBuffer(buffer.Span);
				_ = tuple2.wait;
				num = tuple2.bytesRead;
			}
			if (num != 0)
			{
				_windowManager.AdjustWindow(num, this);
			}
			else
			{
				MoveTrailersToResponseMessage(responseMessage);
			}
			return num;
		}

		public void CopyTo(HttpResponseMessage responseMessage, Stream destination, int bufferSize)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(bufferSize);
			try
			{
				while (true)
				{
					int num;
					bool flag;
					(flag, num) = TryReadFromBuffer(array, partOfSyncRead: true);
					if (flag)
					{
						WaitForData();
						(flag, num) = TryReadFromBuffer(array, partOfSyncRead: true);
					}
					if (num == 0)
					{
						break;
					}
					_windowManager.AdjustWindow(num, this);
					destination.Write(new ReadOnlySpan<byte>(array, 0, num));
				}
				MoveTrailersToResponseMessage(responseMessage);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}

		public async Task CopyToAsync(HttpResponseMessage responseMessage, Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
			try
			{
				while (true)
				{
					var (flag, num) = TryReadFromBuffer(buffer);
					if (flag)
					{
						await WaitForDataAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						(bool wait, int bytesRead) tuple2 = TryReadFromBuffer(buffer);
						_ = tuple2.wait;
						num = tuple2.bytesRead;
					}
					if (num == 0)
					{
						break;
					}
					_windowManager.AdjustWindow(num, this);
					await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				MoveTrailersToResponseMessage(responseMessage);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		private void MoveTrailersToResponseMessage(HttpResponseMessage responseMessage)
		{
			if (_trailers != null)
			{
				responseMessage.StoreReceivedTrailingHeaders(_trailers);
			}
		}

		private async ValueTask SendDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration linkedRegistration = ((cancellationToken.CanBeCanceled && cancellationToken != _requestBodyCancellationSource.Token) ? RegisterRequestBodyCancellation(cancellationToken) : default(CancellationTokenRegistration));
			try
			{
				while (buffer.Length > 0)
				{
					int num = -1;
					bool flush = false;
					lock (_creditSyncObject)
					{
						if (_availableCredit > 0)
						{
							num = Math.Min(buffer.Length, _availableCredit);
							_availableCredit -= num;
							if (_availableCredit == 0)
							{
								flush = true;
							}
						}
						else
						{
							if (_creditWaiter == null)
							{
								_creditWaiter = new CreditWaiter(_requestBodyCancellationSource.Token);
							}
							else
							{
								_creditWaiter.ResetForAwait(_requestBodyCancellationSource.Token);
							}
							_creditWaiter.Amount = buffer.Length;
						}
					}
					if (num == -1)
					{
						num = await _creditWaiter.AsValueTask().ConfigureAwait(continueOnCapturedContext: false);
						lock (_creditSyncObject)
						{
							if (_availableCredit == 0)
							{
								flush = true;
							}
						}
					}
					ReadOnlyMemory<byte> buffer2;
					(buffer2, buffer) = SplitBuffer(buffer, num);
					await _connection.SendStreamDataAsync(StreamId, buffer2, flush, _requestBodyCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == _requestBodyCancellationSource.Token)
			{
				lock (SyncObject)
				{
					Exception resetException = _resetException;
					if (resetException != null)
					{
						if (_canRetry)
						{
							ThrowRetry(System.SR.net_http_request_aborted, resetException);
						}
						ThrowRequestAborted(resetException);
					}
				}
				throw;
			}
			finally
			{
				linkedRegistration.Dispose();
			}
		}

		private void CloseResponseBody()
		{
			bool flag = false;
			lock (SyncObject)
			{
				if (_responseBuffer.IsEmpty && _responseProtocolState == ResponseProtocolState.Complete)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				Cancel();
			}
			lock (SyncObject)
			{
				_responseBuffer.Dispose();
			}
		}

		private CancellationTokenRegistration RegisterRequestBodyCancellation(CancellationToken cancellationToken)
		{
			return cancellationToken.UnsafeRegister(delegate(object s)
			{
				((CancellationTokenSource)s).Cancel();
			}, _requestBodyCancellationSource);
		}

		ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
		{
			return _waitSource.GetStatus(token);
		}

		void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_waitSource.OnCompleted(continuation, state, token, flags);
		}

		void IValueTaskSource.GetResult(short token)
		{
			_waitSourceCancellation.Dispose();
			_waitSourceCancellation = default(CancellationTokenRegistration);
			_waitSource.GetResult(token);
		}

		private void WaitForData()
		{
			new ValueTask(this, _waitSource.Version).AsTask().GetAwaiter().GetResult();
		}

		private ValueTask WaitForDataAsync(CancellationToken cancellationToken)
		{
			_waitSourceCancellation = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken cancellationToken)
			{
				Http2Stream http2Stream = (Http2Stream)s;
				bool hasWaiter;
				lock (http2Stream.SyncObject)
				{
					hasWaiter = http2Stream._hasWaiter;
					http2Stream._hasWaiter = false;
				}
				if (hasWaiter)
				{
					http2Stream._waitSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(CancellationHelper.CreateOperationCanceledException(null, cancellationToken)));
				}
			}, this);
			return new ValueTask(this, _waitSource.Version);
		}

		public void Trace(string message, [CallerMemberName] string memberName = null)
		{
			_connection.Trace(StreamId, message, memberName);
		}
	}

	private struct Http2StreamWindowManager
	{
		private static readonly double StopWatchToTimesSpan = 10000000.0 / (double)Stopwatch.Frequency;

		private int _deliveredBytes;

		private int _streamWindowSize;

		private long _lastWindowUpdate;

		private static double WindowScaleThresholdMultiplier => GlobalHttpSettings.SocketsHttpHandler.Http2StreamWindowScaleThresholdMultiplier;

		private static int MaxStreamWindowSize => GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize;

		private static bool WindowScalingEnabled => !GlobalHttpSettings.SocketsHttpHandler.DisableDynamicHttp2WindowSizing;

		internal int StreamWindowThreshold => _streamWindowSize / 8;

		internal int StreamWindowSize => _streamWindowSize;

		public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
		{
			HttpConnectionSettings settings = connection._pool.Settings;
			_streamWindowSize = settings._initialHttp2StreamWindowSize;
			_deliveredBytes = 0;
			_lastWindowUpdate = 0L;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				stream.Trace($"[FlowControl] InitialClientStreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}, WindowScaleThresholdMultiplier: {WindowScaleThresholdMultiplier}", ".ctor");
			}
		}

		public void Start()
		{
			_lastWindowUpdate = Stopwatch.GetTimestamp();
		}

		public void AdjustWindow(int bytesConsumed, Http2Stream stream)
		{
			if (stream.ExpectResponseData)
			{
				if (WindowScalingEnabled)
				{
					AdjustWindowDynamic(bytesConsumed, stream);
				}
				else
				{
					AjdustWindowStatic(bytesConsumed, stream);
				}
			}
		}

		private void AjdustWindowStatic(int bytesConsumed, Http2Stream stream)
		{
			_deliveredBytes += bytesConsumed;
			if (_deliveredBytes >= StreamWindowThreshold)
			{
				int deliveredBytes = _deliveredBytes;
				_deliveredBytes = 0;
				Http2Connection connection = stream.Connection;
				Task task = connection.SendWindowUpdateAsync(stream.StreamId, deliveredBytes);
				connection.LogExceptions(task);
			}
		}

		private void AdjustWindowDynamic(int bytesConsumed, Http2Stream stream)
		{
			_deliveredBytes += bytesConsumed;
			if (_deliveredBytes < StreamWindowThreshold)
			{
				return;
			}
			int num = _deliveredBytes;
			long timestamp = Stopwatch.GetTimestamp();
			Http2Connection connection = stream.Connection;
			TimeSpan minRtt = connection._rttEstimator.MinRtt;
			if (minRtt > TimeSpan.Zero && _streamWindowSize < MaxStreamWindowSize)
			{
				TimeSpan timeSpan = StopwatchTicksToTimeSpan(timestamp - _lastWindowUpdate);
				if ((double)_deliveredBytes * (double)minRtt.Ticks > (double)(_streamWindowSize * timeSpan.Ticks) * WindowScaleThresholdMultiplier)
				{
					int num2 = Math.Min(MaxStreamWindowSize, _streamWindowSize * 2);
					num += num2 - _streamWindowSize;
					_streamWindowSize = num2;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						stream.Trace($"[FlowControl] Updated Stream Window. StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}", "AdjustWindowDynamic");
					}
					if (_streamWindowSize == MaxStreamWindowSize && System.Net.NetEventSource.Log.IsEnabled())
					{
						stream.Trace($"[FlowControl] StreamWindowSize reached the configured maximum of {MaxStreamWindowSize}.", "AdjustWindowDynamic");
					}
				}
			}
			_deliveredBytes = 0;
			Task task = connection.SendWindowUpdateAsync(stream.StreamId, num);
			connection.LogExceptions(task);
			_lastWindowUpdate = timestamp;
		}

		private static TimeSpan StopwatchTicksToTimeSpan(long stopwatchTicks)
		{
			long ticks = (long)(StopWatchToTimesSpan * (double)stopwatchTicks);
			return new TimeSpan(ticks);
		}
	}

	private struct RttEstimator
	{
		private enum State
		{
			Disabled,
			Init,
			Waiting,
			PingSent,
			TerminatingMayReceivePingAck
		}

		private static readonly long PingIntervalInTicks = (long)(2.0 * (double)Stopwatch.Frequency);

		private State _state;

		private long _pingSentTimestamp;

		private long _pingCounter;

		private int _initialBurst;

		private long _minRtt;

		public TimeSpan MinRtt => new TimeSpan(_minRtt);

		public static RttEstimator Create()
		{
			RttEstimator result = default(RttEstimator);
			result._state = ((!GlobalHttpSettings.SocketsHttpHandler.DisableDynamicHttp2WindowSizing) ? State.Init : State.Disabled);
			result._initialBurst = 4;
			return result;
		}

		internal void OnInitialSettingsSent()
		{
			if (_state != 0)
			{
				_pingSentTimestamp = Stopwatch.GetTimestamp();
			}
		}

		internal void OnInitialSettingsAckReceived(Http2Connection connection)
		{
			if (_state != 0)
			{
				RefreshRtt(connection);
				_state = State.Waiting;
			}
		}

		internal void OnDataOrHeadersReceived(Http2Connection connection)
		{
			if (_state != State.Waiting)
			{
				return;
			}
			long timestamp = Stopwatch.GetTimestamp();
			bool flag = _initialBurst > 0;
			if (flag || timestamp - _pingSentTimestamp > PingIntervalInTicks)
			{
				if (flag)
				{
					_initialBurst--;
				}
				_pingCounter--;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"[FlowControl] Sending RTT PING with payload {_pingCounter}", "OnDataOrHeadersReceived");
				}
				connection.LogExceptions(connection.SendPingAsync(_pingCounter));
				_pingSentTimestamp = timestamp;
				_state = State.PingSent;
			}
		}

		internal void OnPingAckReceived(long payload, Http2Connection connection)
		{
			if (_state != State.PingSent && _state != State.TerminatingMayReceivePingAck)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"[FlowControl] Unexpected PING ACK in state {_state}", "OnPingAckReceived");
				}
				ThrowProtocolError();
			}
			if (_state == State.TerminatingMayReceivePingAck)
			{
				_state = State.Disabled;
				return;
			}
			if (_pingCounter != payload)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"[FlowControl] Unexpected RTT PING ACK payload {payload}, should be {_pingCounter}.", "OnPingAckReceived");
				}
				ThrowProtocolError();
			}
			RefreshRtt(connection);
			_state = State.Waiting;
		}

		internal void OnGoAwayReceived()
		{
			if (_state == State.PingSent)
			{
				_state = State.TerminatingMayReceivePingAck;
			}
			else
			{
				_state = State.Disabled;
			}
		}

		private void RefreshRtt(Http2Connection connection)
		{
			long num = Stopwatch.GetTimestamp() - _pingSentTimestamp;
			long val = ((_minRtt == 0L) ? long.MaxValue : _minRtt);
			long value = Math.Min(val, TimeSpan.FromSeconds((double)num / (double)Stopwatch.Frequency).Ticks);
			Interlocked.Exchange(ref _minRtt, value);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace($"[FlowControl] Updated MinRtt: {MinRtt.TotalMilliseconds} ms", "RefreshRtt");
			}
		}
	}

	private readonly HttpConnectionPool _pool;

	private readonly Stream _stream;

	private System.Net.ArrayBuffer _incomingBuffer;

	private System.Net.ArrayBuffer _outgoingBuffer;

	[ThreadStatic]
	private static string[] t_headerValues;

	private readonly HPackDecoder _hpackDecoder;

	private readonly Dictionary<int, Http2Stream> _httpStreams;

	private readonly CreditManager _connectionWindow;

	private RttEstimator _rttEstimator;

	private int _nextStream;

	private bool _expectingSettingsAck;

	private int _initialServerStreamWindowSize;

	private int _pendingWindowUpdate;

	private long _idleSinceTickCount;

	private uint _maxConcurrentStreams;

	private uint _streamsInUse;

	private TaskCompletionSource<bool> _availableStreamsWaiter;

	private readonly Channel<WriteQueueEntry> _writeChannel;

	private bool _lastPendingWriterShouldFlush;

	private bool _shutdown;

	private TaskCompletionSource _shutdownWaiter;

	private Exception _abortException;

	private bool _disposed;

	private int _markedByTelemetryStatus;

	private static readonly byte[] s_http2ConnectionPreface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

	private static readonly UnboundedChannelOptions s_channelOptions = new UnboundedChannelOptions
	{
		SingleReader = true
	};

	private readonly long _keepAlivePingDelay;

	private readonly long _keepAlivePingTimeout;

	private readonly HttpKeepAlivePingPolicy _keepAlivePingPolicy;

	private long _keepAlivePingPayload;

	private long _nextPingRequestTimestamp;

	private long _keepAlivePingTimeoutTimestamp;

	private volatile KeepAliveState _keepAliveState;

	private object SyncObject => _httpStreams;

	public Http2Connection(HttpConnectionPool pool, Stream stream)
	{
		_pool = pool;
		_stream = stream;
		_incomingBuffer = new System.Net.ArrayBuffer(4096);
		_outgoingBuffer = new System.Net.ArrayBuffer(4096);
		_hpackDecoder = new HPackDecoder(4096, pool.Settings._maxResponseHeadersLength * 1024);
		_httpStreams = new Dictionary<int, Http2Stream>();
		_connectionWindow = new CreditManager(this, "_connectionWindow", 65535);
		_rttEstimator = RttEstimator.Create();
		_writeChannel = Channel.CreateUnbounded<WriteQueueEntry>(s_channelOptions);
		_nextStream = 1;
		_initialServerStreamWindowSize = 65535;
		_maxConcurrentStreams = 100u;
		_streamsInUse = 0u;
		_pendingWindowUpdate = 0;
		_idleSinceTickCount = Environment.TickCount64;
		_keepAlivePingDelay = TimeSpanToMs(_pool.Settings._keepAlivePingDelay);
		_keepAlivePingTimeout = TimeSpanToMs(_pool.Settings._keepAlivePingTimeout);
		_nextPingRequestTimestamp = Environment.TickCount64 + _keepAlivePingDelay;
		_keepAlivePingPolicy = _pool.Settings._keepAlivePingPolicy;
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.Http20ConnectionEstablished();
			_markedByTelemetryStatus = 1;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			TraceConnection(_stream);
		}
		static long TimeSpanToMs(TimeSpan value)
		{
			double totalMilliseconds = value.TotalMilliseconds;
			return (long)((totalMilliseconds > 2147483647.0) ? 2147483647.0 : totalMilliseconds);
		}
	}

	~Http2Connection()
	{
		Dispose();
	}

	public async ValueTask SetupAsync()
	{
		try
		{
			_outgoingBuffer.EnsureAvailableSpace(s_http2ConnectionPreface.Length + 9 + 6 + 9 + 4);
			s_http2ConnectionPreface.AsSpan().CopyTo(_outgoingBuffer.AvailableSpan);
			_outgoingBuffer.Commit(s_http2ConnectionPreface.Length);
			FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, 12, FrameType.Settings, FrameFlags.None, 0);
			_outgoingBuffer.Commit(9);
			BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, 2);
			_outgoingBuffer.Commit(2);
			BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, 0u);
			_outgoingBuffer.Commit(4);
			BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, 4);
			_outgoingBuffer.Commit(2);
			BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, (uint)_pool.Settings._initialHttp2StreamWindowSize);
			_outgoingBuffer.Commit(4);
			uint value = 67043329u;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Initial connection-level WINDOW_UPDATE, windowUpdateAmount={value}", "SetupAsync");
			}
			FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, 4, FrameType.WindowUpdate, FrameFlags.None, 0);
			_outgoingBuffer.Commit(9);
			BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, value);
			_outgoingBuffer.Commit(4);
			await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(continueOnCapturedContext: false);
			_rttEstimator.OnInitialSettingsSent();
			_outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);
			_expectingSettingsAck = true;
		}
		catch (Exception innerException)
		{
			Dispose();
			throw new IOException(System.SR.net_http_http2_connection_not_established, innerException);
		}
		ProcessIncomingFramesAsync();
		ProcessOutgoingFramesAsync();
	}

	public ValueTask WaitForShutdownAsync()
	{
		lock (SyncObject)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("Http2Connection");
			}
			if (_shutdown)
			{
				return default(ValueTask);
			}
			if (_shutdownWaiter == null)
			{
				_shutdownWaiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			}
			return new ValueTask(_shutdownWaiter.Task);
		}
	}

	private void Shutdown()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"_shutdown"}={_shutdown}, {"_abortException"}={_abortException}", "Shutdown");
		}
		SignalAvailableStreamsWaiter(result: false);
		SignalShutdownWaiter();
		_shutdown = true;
	}

	private void SignalShutdownWaiter()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"_shutdownWaiter"}?={_shutdownWaiter != null}", "SignalShutdownWaiter");
		}
		if (_shutdownWaiter != null)
		{
			_shutdownWaiter.SetResult();
			_shutdownWaiter = null;
		}
	}

	public bool TryReserveStream()
	{
		lock (SyncObject)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("Http2Connection");
			}
			if (_shutdown)
			{
				return false;
			}
			if (_streamsInUse < _maxConcurrentStreams)
			{
				_streamsInUse++;
				return true;
			}
		}
		return false;
	}

	public void ReleaseStream()
	{
		lock (SyncObject)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"_streamsInUse"}={_streamsInUse}", "ReleaseStream");
			}
			_streamsInUse--;
			if (_streamsInUse < _maxConcurrentStreams)
			{
				SignalAvailableStreamsWaiter(result: true);
			}
			if (_streamsInUse == 0)
			{
				_idleSinceTickCount = Environment.TickCount64;
				if (_disposed)
				{
					FinalTeardown();
				}
			}
		}
	}

	public ValueTask<bool> WaitForAvailableStreamsAsync()
	{
		lock (SyncObject)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("Http2Connection");
			}
			if (_shutdown)
			{
				return ValueTask.FromResult(result: false);
			}
			if (_streamsInUse < _maxConcurrentStreams)
			{
				return ValueTask.FromResult(result: true);
			}
			_availableStreamsWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			return new ValueTask<bool>(_availableStreamsWaiter.Task);
		}
	}

	private void SignalAvailableStreamsWaiter(bool result)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"result"}={result}, {"_availableStreamsWaiter"}?={_availableStreamsWaiter != null}", "SignalAvailableStreamsWaiter");
		}
		if (_availableStreamsWaiter != null)
		{
			_availableStreamsWaiter.SetResult(result);
			_availableStreamsWaiter = null;
		}
	}

	private async Task FlushOutgoingBytesAsync()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"ActiveLength"}={_outgoingBuffer.ActiveLength}", "FlushOutgoingBytesAsync");
		}
		if (_outgoingBuffer.ActiveLength > 0)
		{
			try
			{
				await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception abortException)
			{
				Abort(abortException);
			}
			_lastPendingWriterShouldFlush = false;
			_outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);
		}
	}

	private async ValueTask<FrameHeader> ReadFrameAsync(bool initialFrame = false)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"initialFrame"}={initialFrame}", "ReadFrameAsync");
		}
		if (_incomingBuffer.ActiveLength < 9)
		{
			_incomingBuffer.EnsureAvailableSpace(9 - _incomingBuffer.ActiveLength);
			do
			{
				int num = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
				_incomingBuffer.Commit(num);
				if (num == 0)
				{
					if (_incomingBuffer.ActiveLength == 0)
					{
						ThrowMissingFrame();
					}
					else
					{
						ThrowPrematureEOF(9);
					}
				}
			}
			while (_incomingBuffer.ActiveLength < 9);
		}
		FrameHeader frameHeader = FrameHeader.ReadFrom(_incomingBuffer.ActiveSpan);
		if (frameHeader.PayloadLength > 16384)
		{
			if (initialFrame && System.Net.NetEventSource.Log.IsEnabled())
			{
				string @string = Encoding.ASCII.GetString(_incomingBuffer.ActiveSpan.Slice(0, Math.Min(20, _incomingBuffer.ActiveLength)));
				Trace("HTTP/2 handshake failed. Server returned " + @string, "ReadFrameAsync");
			}
			_incomingBuffer.Discard(9);
			ThrowProtocolError(initialFrame ? Http2ProtocolErrorCode.ProtocolError : Http2ProtocolErrorCode.FrameSizeError);
		}
		_incomingBuffer.Discard(9);
		if (_incomingBuffer.ActiveLength < frameHeader.PayloadLength)
		{
			_incomingBuffer.EnsureAvailableSpace(frameHeader.PayloadLength - _incomingBuffer.ActiveLength);
			do
			{
				int num2 = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
				_incomingBuffer.Commit(num2);
				if (num2 == 0)
				{
					ThrowPrematureEOF(frameHeader.PayloadLength);
				}
			}
			while (_incomingBuffer.ActiveLength < frameHeader.PayloadLength);
		}
		return frameHeader;
		static void ThrowMissingFrame()
		{
			throw new IOException(System.SR.net_http_invalid_response_missing_frame);
		}
		void ThrowPrematureEOF(int requiredBytes)
		{
			throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, requiredBytes - _incomingBuffer.ActiveLength));
		}
	}

	private async Task ProcessIncomingFramesAsync()
	{
		_ = 3;
		try
		{
			try
			{
				FrameHeader frameHeader = await ReadFrameAsync(initialFrame: true).ConfigureAwait(continueOnCapturedContext: false);
				if (frameHeader.Type != FrameType.Settings || frameHeader.AckFlag)
				{
					ThrowProtocolError();
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Frame 0: {frameHeader}.", "ProcessIncomingFramesAsync");
				}
				ProcessSettingsFrame(frameHeader, initialFrame: true);
			}
			catch (IOException innerException)
			{
				throw new IOException(System.SR.net_http_http2_connection_not_established, innerException);
			}
			long frameNum = 1L;
			while (true)
			{
				if (_incomingBuffer.ActiveLength < 9)
				{
					_incomingBuffer.EnsureAvailableSpace(9 - _incomingBuffer.ActiveLength);
					int num;
					do
					{
						num = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
						_incomingBuffer.Commit(num);
					}
					while (num != 0 && _incomingBuffer.ActiveLength < 9);
				}
				FrameHeader frameHeader = await ReadFrameAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Frame {frameNum}: {frameHeader}.", "ProcessIncomingFramesAsync");
				}
				RefreshPingTimestamp();
				switch (frameHeader.Type)
				{
				case FrameType.Headers:
					await ProcessHeadersFrame(frameHeader).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case FrameType.Data:
					ProcessDataFrame(frameHeader);
					break;
				case FrameType.Settings:
					ProcessSettingsFrame(frameHeader);
					break;
				case FrameType.Priority:
					ProcessPriorityFrame(frameHeader);
					break;
				case FrameType.Ping:
					ProcessPingFrame(frameHeader);
					break;
				case FrameType.WindowUpdate:
					ProcessWindowUpdateFrame(frameHeader);
					break;
				case FrameType.RstStream:
					ProcessRstStreamFrame(frameHeader);
					break;
				case FrameType.GoAway:
					ProcessGoAwayFrame(frameHeader);
					break;
				case FrameType.AltSvc:
					ProcessAltSvcFrame(frameHeader);
					break;
				default:
					ThrowProtocolError();
					break;
				}
				frameNum++;
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("ProcessIncomingFramesAsync: " + ex.Message, "ProcessIncomingFramesAsync");
			}
			Abort(ex);
		}
	}

	private Http2Stream GetStream(int streamId)
	{
		if (streamId <= 0 || streamId >= _nextStream)
		{
			ThrowProtocolError();
		}
		lock (SyncObject)
		{
			if (!_httpStreams.TryGetValue(streamId, out var value))
			{
				return null;
			}
			return value;
		}
	}

	private async ValueTask ProcessHeadersFrame(FrameHeader frameHeader)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{frameHeader}", "ProcessHeadersFrame");
		}
		bool endStream = frameHeader.EndStreamFlag;
		int streamId = frameHeader.StreamId;
		Http2Stream http2Stream = GetStream(streamId);
		IHttpHeadersHandler headersHandler;
		if (http2Stream != null)
		{
			http2Stream.OnHeadersStart();
			_rttEstimator.OnDataOrHeadersReceived(this);
			headersHandler = http2Stream;
		}
		else
		{
			headersHandler = NopHeadersHandler.Instance;
		}
		_hpackDecoder.Decode(GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.PaddedFlag, frameHeader.PriorityFlag), frameHeader.EndHeadersFlag, headersHandler);
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		while (!frameHeader.EndHeadersFlag)
		{
			frameHeader = await ReadFrameAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (frameHeader.Type != FrameType.Continuation || frameHeader.StreamId != streamId)
			{
				ThrowProtocolError();
			}
			_hpackDecoder.Decode(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.EndHeadersFlag, headersHandler);
			_incomingBuffer.Discard(frameHeader.PayloadLength);
		}
		_hpackDecoder.CompleteDecode();
		http2Stream?.OnHeadersComplete(endStream);
	}

	private ReadOnlySpan<byte> GetFrameData(ReadOnlySpan<byte> frameData, bool hasPad, bool hasPriority)
	{
		if (hasPad)
		{
			if (frameData.Length == 0)
			{
				ThrowProtocolError();
			}
			int num = frameData[0];
			frameData = frameData.Slice(1);
			if (frameData.Length < num)
			{
				ThrowProtocolError();
			}
			frameData = frameData.Slice(0, frameData.Length - num);
		}
		if (hasPriority)
		{
			if (frameData.Length < 5)
			{
				ThrowProtocolError();
			}
			frameData = frameData.Slice(5);
		}
		return frameData;
	}

	private void ProcessAltSvcFrame(FrameHeader frameHeader)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{frameHeader}", "ProcessAltSvcFrame");
		}
		ReadOnlySpan<byte> readOnlySpan = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength);
		if (BinaryPrimitives.TryReadUInt16BigEndian(readOnlySpan, out var value))
		{
			readOnlySpan = readOnlySpan.Slice(2);
			if ((frameHeader.StreamId != 0 && value == 0) || (frameHeader.StreamId == 0 && readOnlySpan.Length >= value && readOnlySpan.Slice(0, value).SequenceEqual(_pool.Http2AltSvcOriginUri)))
			{
				readOnlySpan = readOnlySpan.Slice(value);
				string @string = Encoding.ASCII.GetString(readOnlySpan);
				_pool.HandleAltSvc(new string[1] { @string }, null);
			}
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessDataFrame(FrameHeader frameHeader)
	{
		Http2Stream stream = GetStream(frameHeader.StreamId);
		ReadOnlySpan<byte> frameData = GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.PaddedFlag, hasPriority: false);
		if (stream != null)
		{
			bool endStreamFlag = frameHeader.EndStreamFlag;
			stream.OnResponseData(frameData, endStreamFlag);
			if (!endStreamFlag && frameData.Length > 0)
			{
				_rttEstimator.OnDataOrHeadersReceived(this);
			}
		}
		if (frameData.Length > 0)
		{
			ExtendWindow(frameData.Length);
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessSettingsFrame(FrameHeader frameHeader, bool initialFrame = false)
	{
		if (frameHeader.StreamId != 0)
		{
			ThrowProtocolError();
		}
		if (frameHeader.AckFlag)
		{
			if (frameHeader.PayloadLength != 0)
			{
				ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
			}
			if (!_expectingSettingsAck)
			{
				ThrowProtocolError();
			}
			_expectingSettingsAck = false;
			_rttEstimator.OnInitialSettingsAckReceived(this);
			return;
		}
		if (frameHeader.PayloadLength % 6 != 0)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		ReadOnlySpan<byte> source = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength);
		bool flag = false;
		while (source.Length > 0)
		{
			ushort num = BinaryPrimitives.ReadUInt16BigEndian(source);
			source = source.Slice(2);
			uint num2 = BinaryPrimitives.ReadUInt32BigEndian(source);
			source = source.Slice(4);
			switch ((SettingId)num)
			{
			case SettingId.MaxConcurrentStreams:
				ChangeMaxConcurrentStreams(num2);
				flag = true;
				break;
			case SettingId.InitialWindowSize:
				if (num2 > int.MaxValue)
				{
					ThrowProtocolError(Http2ProtocolErrorCode.FlowControlError);
				}
				ChangeInitialWindowSize((int)num2);
				break;
			case SettingId.MaxFrameSize:
				if (num2 < 16384 || num2 > 16777215)
				{
					ThrowProtocolError();
				}
				break;
			}
		}
		if (initialFrame && !flag)
		{
			ChangeMaxConcurrentStreams(2147483647u);
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		LogExceptions(SendSettingsAckAsync());
	}

	private void ChangeMaxConcurrentStreams(uint newValue)
	{
		lock (SyncObject)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"newValue"}={newValue}, {"_streamsInUse"}={_streamsInUse}, {"_availableStreamsWaiter"}?={_availableStreamsWaiter != null}", "ChangeMaxConcurrentStreams");
			}
			_maxConcurrentStreams = newValue;
			if (_streamsInUse < _maxConcurrentStreams)
			{
				SignalAvailableStreamsWaiter(result: true);
			}
		}
	}

	private void ChangeInitialWindowSize(int newSize)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"newSize"}={newSize}", "ChangeInitialWindowSize");
		}
		lock (SyncObject)
		{
			int amount = newSize - _initialServerStreamWindowSize;
			_initialServerStreamWindowSize = newSize;
			foreach (KeyValuePair<int, Http2Stream> httpStream in _httpStreams)
			{
				httpStream.Value.OnWindowUpdate(amount);
			}
		}
	}

	private void ProcessPriorityFrame(FrameHeader frameHeader)
	{
		if (frameHeader.StreamId == 0 || frameHeader.PayloadLength != 5)
		{
			ThrowProtocolError();
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessPingFrame(FrameHeader frameHeader)
	{
		if (frameHeader.StreamId != 0)
		{
			ThrowProtocolError();
		}
		if (frameHeader.PayloadLength != 8)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		ReadOnlySpan<byte> source = _incomingBuffer.ActiveSpan.Slice(0, 8);
		long num = BinaryPrimitives.ReadInt64BigEndian(source);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received PING frame, content:{num} ack: {frameHeader.AckFlag}", "ProcessPingFrame");
		}
		if (frameHeader.AckFlag)
		{
			ProcessPingAck(num);
		}
		else
		{
			LogExceptions(SendPingAsync(num, isAck: true));
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessWindowUpdateFrame(FrameHeader frameHeader)
	{
		if (frameHeader.PayloadLength != 4)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		int num = BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{frameHeader}. {"amount"}={num}", "ProcessWindowUpdateFrame");
		}
		if (num == 0)
		{
			ThrowProtocolError();
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		if (frameHeader.StreamId == 0)
		{
			_connectionWindow.AdjustCredit(num);
		}
		else
		{
			GetStream(frameHeader.StreamId)?.OnWindowUpdate(num);
		}
	}

	private void ProcessRstStreamFrame(FrameHeader frameHeader)
	{
		if (frameHeader.PayloadLength != 4)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		if (frameHeader.StreamId == 0)
		{
			ThrowProtocolError();
		}
		Http2Stream stream = GetStream(frameHeader.StreamId);
		if (stream == null)
		{
			_incomingBuffer.Discard(frameHeader.PayloadLength);
			return;
		}
		Http2ProtocolErrorCode http2ProtocolErrorCode = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace(frameHeader.StreamId, $"{"protocolError"}={http2ProtocolErrorCode}", "ProcessRstStreamFrame");
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		if (http2ProtocolErrorCode == Http2ProtocolErrorCode.RefusedStream)
		{
			stream.OnReset(new Http2StreamException(http2ProtocolErrorCode), http2ProtocolErrorCode, canRetry: true);
		}
		else
		{
			stream.OnReset(new Http2StreamException(http2ProtocolErrorCode), http2ProtocolErrorCode);
		}
	}

	private void ProcessGoAwayFrame(FrameHeader frameHeader)
	{
		if (frameHeader.PayloadLength < 8)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		if (frameHeader.StreamId != 0)
		{
			ThrowProtocolError();
		}
		int num = (int)(BinaryPrimitives.ReadUInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF);
		Http2ProtocolErrorCode http2ProtocolErrorCode = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan.Slice(4));
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace(frameHeader.StreamId, $"{"lastStreamId"}={num}, {"errorCode"}={http2ProtocolErrorCode}", "ProcessGoAwayFrame");
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		Exception resetException = new Http2ConnectionException(http2ProtocolErrorCode);
		_rttEstimator.OnGoAwayReceived();
		List<Http2Stream> list = new List<Http2Stream>();
		lock (SyncObject)
		{
			Shutdown();
			foreach (KeyValuePair<int, Http2Stream> httpStream in _httpStreams)
			{
				int key = httpStream.Key;
				if (key > num)
				{
					list.Add(httpStream.Value);
				}
			}
		}
		foreach (Http2Stream item in list)
		{
			item.OnReset(resetException, null, canRetry: true);
		}
	}

	internal Task FlushAsync(CancellationToken cancellationToken)
	{
		return PerformWriteAsync(0, 0, (int _, Memory<byte> __) => true, cancellationToken);
	}

	private Task PerformWriteAsync<T>(int writeBytes, T state, Func<T, Memory<byte>, bool> writeAction, CancellationToken cancellationToken = default(CancellationToken))
	{
		WriteQueueEntry writeQueueEntry = new WriteQueueEntry<T>(writeBytes, state, writeAction, cancellationToken);
		if (!_writeChannel.Writer.TryWrite(writeQueueEntry))
		{
			if (_abortException != null)
			{
				return Task.FromException(GetRequestAbortedException(_abortException));
			}
			return Task.FromException(new ObjectDisposedException("Http2Connection"));
		}
		return writeQueueEntry.Task;
	}

	private async Task ProcessOutgoingFramesAsync()
	{
		_ = 2;
		try
		{
			while (await _writeChannel.Reader.WaitToReadAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				WriteQueueEntry writeEntry;
				while (_writeChannel.Reader.TryRead(out writeEntry))
				{
					if (_abortException != null)
					{
						if (writeEntry.TryDisableCancellation())
						{
							writeEntry.SetException(_abortException);
						}
						continue;
					}
					int writeBytes = writeEntry.WriteBytes;
					int capacity = _outgoingBuffer.Capacity;
					if (capacity >= 32768)
					{
						int activeLength = _outgoingBuffer.ActiveLength;
						if (writeBytes >= capacity - activeLength)
						{
							await FlushOutgoingBytesAsync().ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					if (!writeEntry.TryDisableCancellation())
					{
						continue;
					}
					_outgoingBuffer.EnsureAvailableSpace(writeBytes);
					try
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace($"{"writeBytes"}={writeBytes}", "ProcessOutgoingFramesAsync");
						}
						bool flag = writeEntry.InvokeWriteAction(_outgoingBuffer.AvailableMemorySliced(writeBytes));
						writeEntry.SetResult();
						_outgoingBuffer.Commit(writeBytes);
						_lastPendingWriterShouldFlush |= flag;
					}
					catch (Exception exception)
					{
						writeEntry.SetException(exception);
					}
				}
				if (_lastPendingWriterShouldFlush)
				{
					await FlushOutgoingBytesAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch (Exception value)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Unexpected exception in {"ProcessOutgoingFramesAsync"}: {value}", "ProcessOutgoingFramesAsync");
			}
		}
	}

	private Task SendSettingsAckAsync()
	{
		return PerformWriteAsync(9, this, delegate(Http2Connection thisRef, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				thisRef.Trace("Started writing.", "SendSettingsAckAsync");
			}
			FrameHeader.WriteTo(writeBuffer.Span, 0, FrameType.Settings, FrameFlags.EndStream, 0);
			return true;
		});
	}

	private Task SendPingAsync(long pingContent, bool isAck = false)
	{
		return PerformWriteAsync(17, (this, pingContent, isAck), delegate((Http2Connection thisRef, long pingContent, bool isAck) state, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				state.thisRef.Trace($"Started writing. {"pingContent"}={state.pingContent}", "SendPingAsync");
			}
			Span<byte> span = writeBuffer.Span;
			FrameHeader.WriteTo(span, 8, FrameType.Ping, state.isAck ? FrameFlags.EndStream : FrameFlags.None, 0);
			BinaryPrimitives.WriteInt64BigEndian(span.Slice(9), state.pingContent);
			return true;
		});
	}

	private Task SendRstStreamAsync(int streamId, Http2ProtocolErrorCode errorCode)
	{
		return PerformWriteAsync(13, (this, streamId, errorCode), delegate((Http2Connection thisRef, int streamId, Http2ProtocolErrorCode errorCode) s, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				s.thisRef.Trace(s.streamId, $"Started writing. {"errorCode"}={s.errorCode}", "SendRstStreamAsync");
			}
			Span<byte> span = writeBuffer.Span;
			FrameHeader.WriteTo(span, 4, FrameType.RstStream, FrameFlags.None, s.streamId);
			BinaryPrimitives.WriteInt32BigEndian(span.Slice(9), (int)s.errorCode);
			return true;
		});
	}

	internal void HeartBeat()
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			VerifyKeepAlive();
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("HeartBeat: " + ex.Message, "HeartBeat");
			}
			Abort(ex);
		}
	}

	private static (ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) SplitBuffer(ReadOnlyMemory<byte> buffer, int maxSize)
	{
		if (buffer.Length <= maxSize)
		{
			return (first: buffer, rest: Memory<byte>.Empty);
		}
		return (first: buffer.Slice(0, maxSize), rest: buffer.Slice(maxSize));
	}

	private void WriteIndexedHeader(int index, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"index"}={index}", "WriteIndexedHeader");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeIndexedHeaderField(index, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteIndexedHeader(int index, string value, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"index"}={index}, {"value"}={value}", "WriteIndexedHeader");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(index, value, null, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteLiteralHeader(string name, ReadOnlySpan<string> values, Encoding valueEncoding, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"name"}={name}, {"values"}={string.Join(", ", values.ToArray())}", "WriteLiteralHeader");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, values, ", ", valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteLiteralHeaderValues(ReadOnlySpan<string> values, string separator, Encoding valueEncoding, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("values=" + string.Join(separator, values.ToArray()), "WriteLiteralHeaderValues");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeStringLiterals(values, separator, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteLiteralHeaderValue(string value, Encoding valueEncoding, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("value=" + value, "WriteLiteralHeaderValue");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeStringLiteral(value, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteBytes(ReadOnlySpan<byte> bytes, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"Length"}={bytes.Length}", "WriteBytes");
		}
		if (bytes.Length > headerBuffer.AvailableLength)
		{
			headerBuffer.EnsureAvailableSpace(bytes.Length);
		}
		bytes.CopyTo(headerBuffer.AvailableSpan);
		headerBuffer.Commit(bytes.Length);
	}

	private void WriteHeaderCollection(HttpRequestMessage request, HttpHeaders headers, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("", "WriteHeaderCollection");
		}
		if (headers.HeaderStore == null)
		{
			return;
		}
		HeaderEncodingSelector<HttpRequestMessage> requestHeaderEncodingSelector = _pool.Settings._requestHeaderEncodingSelector;
		foreach (KeyValuePair<HeaderDescriptor, object> item in headers.HeaderStore)
		{
			int storeValuesIntoStringArray = HttpHeaders.GetStoreValuesIntoStringArray(item.Key, item.Value, ref t_headerValues);
			ReadOnlySpan<string> readOnlySpan = t_headerValues.AsSpan(0, storeValuesIntoStringArray);
			Encoding valueEncoding = requestHeaderEncodingSelector?.Invoke(item.Key.Name, request);
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
							WriteBytes(knownHeader.Http2EncodedName, ref headerBuffer);
							WriteLiteralHeaderValue(text, valueEncoding, ref headerBuffer);
							break;
						}
					}
				}
				else
				{
					WriteBytes(knownHeader.Http2EncodedName, ref headerBuffer);
					string separator = null;
					if (readOnlySpan.Length > 1)
					{
						HttpHeaderParser parser = item.Key.Parser;
						separator = ((parser == null || !parser.SupportsMultipleValues) ? ", " : parser.Separator);
					}
					WriteLiteralHeaderValues(readOnlySpan, separator, valueEncoding, ref headerBuffer);
				}
			}
			else
			{
				WriteLiteralHeader(item.Key.Name, readOnlySpan, valueEncoding, ref headerBuffer);
			}
		}
	}

	private void WriteHeaders(HttpRequestMessage request, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("", "WriteHeaders");
		}
		if (request.HasHeaders && request.Headers.TransferEncodingChunked == true)
		{
			request.Headers.TransferEncodingChunked = false;
		}
		HttpMethod httpMethod = HttpMethod.Normalize(request.Method);
		if ((object)httpMethod == HttpMethod.Get)
		{
			WriteIndexedHeader(2, ref headerBuffer);
		}
		else if ((object)httpMethod == HttpMethod.Post)
		{
			WriteIndexedHeader(3, ref headerBuffer);
		}
		else
		{
			WriteIndexedHeader(2, httpMethod.Method, ref headerBuffer);
		}
		WriteIndexedHeader((_stream is SslStream) ? 7 : 6, ref headerBuffer);
		if (request.HasHeaders && request.Headers.Host != null)
		{
			WriteIndexedHeader(1, request.Headers.Host, ref headerBuffer);
		}
		else
		{
			WriteBytes(_pool._http2EncodedAuthorityHostHeader, ref headerBuffer);
		}
		string pathAndQuery = request.RequestUri.PathAndQuery;
		if (pathAndQuery == "/")
		{
			WriteIndexedHeader(4, ref headerBuffer);
		}
		else
		{
			WriteIndexedHeader(4, pathAndQuery, ref headerBuffer);
		}
		if (request.HasHeaders)
		{
			WriteHeaderCollection(request, request.Headers, ref headerBuffer);
		}
		if (_pool.Settings._useCookies)
		{
			string cookieHeader = _pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
			if (cookieHeader != string.Empty)
			{
				WriteBytes(KnownHeaders.Cookie.Http2EncodedName, ref headerBuffer);
				Encoding valueEncoding = _pool.Settings._requestHeaderEncodingSelector?.Invoke(KnownHeaders.Cookie.Name, request);
				WriteLiteralHeaderValue(cookieHeader, valueEncoding, ref headerBuffer);
			}
		}
		if (request.Content == null)
		{
			if (httpMethod.MustHaveRequestBody)
			{
				WriteBytes(KnownHeaders.ContentLength.Http2EncodedName, ref headerBuffer);
				WriteLiteralHeaderValue("0", null, ref headerBuffer);
			}
		}
		else
		{
			WriteHeaderCollection(request, request.Content.Headers, ref headerBuffer);
		}
	}

	private void AddStream(Http2Stream http2Stream)
	{
		lock (SyncObject)
		{
			if (_nextStream == int.MaxValue)
			{
				Shutdown();
			}
			if (_abortException != null)
			{
				throw GetRequestAbortedException(_abortException);
			}
			if (_shutdown)
			{
				ThrowRetry(System.SR.net_http_server_shutdown);
			}
			if (_streamsInUse > _maxConcurrentStreams)
			{
				ThrowRetry(System.SR.net_http_request_aborted);
			}
			http2Stream.Initialize(_nextStream, _initialServerStreamWindowSize);
			_nextStream += 2;
			_httpStreams.Add(http2Stream.StreamId, http2Stream);
		}
	}

	private async ValueTask<Http2Stream> SendHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool mustFlush)
	{
		System.Net.ArrayBuffer headerBuffer = default(System.Net.ArrayBuffer);
		try
		{
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStart();
			}
			headerBuffer = new System.Net.ArrayBuffer(4096, usePool: true);
			WriteHeaders(request, ref headerBuffer);
			ReadOnlyMemory<byte> item = headerBuffer.ActiveMemory;
			int num = (item.Length - 1) / 16384 + 1;
			int writeBytes = item.Length + num * 9;
			Http2Stream http2Stream = new Http2Stream(request, this);
			await PerformWriteAsync(writeBytes, (this, http2Stream, item, request.Content == null, mustFlush), delegate((Http2Connection thisRef, Http2Stream http2Stream, ReadOnlyMemory<byte> headerBytes, bool endStream, bool mustFlush) s, Memory<byte> writeBuffer)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					s.thisRef.Trace(s.http2Stream.StreamId, $"Started writing. Total header bytes={s.headerBytes.Length}", "SendHeadersAsync");
				}
				s.thisRef.AddStream(s.http2Stream);
				Span<byte> destination = writeBuffer.Span;
				(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) tuple = SplitBuffer(s.headerBytes, 16384);
				ReadOnlyMemory<byte> item2 = tuple.first;
				ReadOnlyMemory<byte> item3 = tuple.rest;
				FrameFlags frameFlags = ((item3.Length == 0) ? FrameFlags.EndHeaders : FrameFlags.None);
				frameFlags |= (s.endStream ? FrameFlags.EndStream : FrameFlags.None);
				FrameHeader.WriteTo(destination, item2.Length, FrameType.Headers, frameFlags, s.http2Stream.StreamId);
				destination = destination.Slice(9);
				item2.Span.CopyTo(destination);
				destination = destination.Slice(item2.Length);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					s.thisRef.Trace(s.http2Stream.StreamId, $"Wrote HEADERS frame. Length={item2.Length}, flags={frameFlags}", "SendHeadersAsync");
				}
				while (item3.Length > 0)
				{
					(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) tuple2 = SplitBuffer(item3, 16384);
					item2 = tuple2.first;
					item3 = tuple2.rest;
					frameFlags = ((item3.Length == 0) ? FrameFlags.EndHeaders : FrameFlags.None);
					FrameHeader.WriteTo(destination, item2.Length, FrameType.Continuation, frameFlags, s.http2Stream.StreamId);
					destination = destination.Slice(9);
					item2.Span.CopyTo(destination);
					destination = destination.Slice(item2.Length);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						s.thisRef.Trace(s.http2Stream.StreamId, $"Wrote CONTINUATION frame. Length={item2.Length}, flags={frameFlags}", "SendHeadersAsync");
					}
				}
				return s.mustFlush || s.endStream;
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStop();
			}
			return http2Stream;
		}
		catch
		{
			ReleaseStream();
			throw;
		}
		finally
		{
			headerBuffer.Dispose();
		}
	}

	private async Task SendStreamDataAsync(int streamId, ReadOnlyMemory<byte> buffer, bool finalFlush, CancellationToken cancellationToken)
	{
		ReadOnlyMemory<byte> remaining = buffer;
		while (remaining.Length > 0)
		{
			int frameSize = Math.Min(remaining.Length, 16384);
			frameSize = await _connectionWindow.RequestCreditAsync(frameSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>) tuple = SplitBuffer(remaining, frameSize);
			ReadOnlyMemory<byte> item = tuple.Item1;
			remaining = tuple.Item2;
			bool item2 = false;
			if (finalFlush && remaining.Length == 0)
			{
				item2 = true;
			}
			if (!_connectionWindow.IsCreditAvailable)
			{
				item2 = true;
			}
			try
			{
				await PerformWriteAsync(9 + item.Length, (this, streamId, item, item2), delegate((Http2Connection thisRef, int streamId, ReadOnlyMemory<byte> current, bool flush) s, Memory<byte> writeBuffer)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						s.thisRef.Trace(s.streamId, $"Started writing. {"Length"}={writeBuffer.Length}", "SendStreamDataAsync");
					}
					FrameHeader.WriteTo(writeBuffer.Span, s.current.Length, FrameType.Data, FrameFlags.None, s.streamId);
					s.current.CopyTo(writeBuffer.Slice(9));
					return s.flush;
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				_connectionWindow.AdjustCredit(frameSize);
				throw;
			}
		}
	}

	private Task SendEndStreamAsync(int streamId)
	{
		return PerformWriteAsync(9, (this, streamId), delegate((Http2Connection thisRef, int streamId) s, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				s.thisRef.Trace(s.streamId, "Started writing.", "SendEndStreamAsync");
			}
			FrameHeader.WriteTo(writeBuffer.Span, 0, FrameType.Data, FrameFlags.EndStream, s.streamId);
			return true;
		});
	}

	private Task SendWindowUpdateAsync(int streamId, int amount)
	{
		return PerformWriteAsync(13, (this, streamId, amount), delegate((Http2Connection thisRef, int streamId, int amount) s, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				s.thisRef.Trace(s.streamId, $"Started writing. {"amount"}={s.amount}", "SendWindowUpdateAsync");
			}
			Span<byte> span = writeBuffer.Span;
			FrameHeader.WriteTo(span, 4, FrameType.WindowUpdate, FrameFlags.None, s.streamId);
			BinaryPrimitives.WriteInt32BigEndian(span.Slice(9), s.amount);
			return true;
		});
	}

	private void ExtendWindow(int amount)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"amount"}={amount}", "ExtendWindow");
		}
		int pendingWindowUpdate;
		lock (SyncObject)
		{
			_pendingWindowUpdate += amount;
			if (_pendingWindowUpdate < 8388608)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"{"_pendingWindowUpdate"} {_pendingWindowUpdate} < {8388608}.", "ExtendWindow");
				}
				return;
			}
			pendingWindowUpdate = _pendingWindowUpdate;
			_pendingWindowUpdate = 0;
		}
		LogExceptions(SendWindowUpdateAsync(0, pendingWindowUpdate));
	}

	public override long GetIdleTicks(long nowTicks)
	{
		lock (SyncObject)
		{
			return (_streamsInUse == 0) ? (nowTicks - _idleSinceTickCount) : 0;
		}
	}

	private void Abort(Exception abortException)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"abortException"}=={abortException}", "Abort");
		}
		List<Http2Stream> list = new List<Http2Stream>();
		lock (SyncObject)
		{
			if (_abortException != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Abort called while already aborting. {"abortException"}={abortException}", "Abort");
				}
				return;
			}
			_abortException = abortException;
			Shutdown();
			foreach (KeyValuePair<int, Http2Stream> httpStream in _httpStreams)
			{
				int key = httpStream.Key;
				list.Add(httpStream.Value);
			}
		}
		foreach (Http2Stream item in list)
		{
			item.OnReset(_abortException);
		}
	}

	private void FinalTeardown()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("", "FinalTeardown");
		}
		GC.SuppressFinalize(this);
		_stream.Dispose();
		_connectionWindow.Dispose();
		_writeChannel.Writer.Complete();
		if (HttpTelemetry.Log.IsEnabled() && Interlocked.Exchange(ref _markedByTelemetryStatus, 2) == 1)
		{
			HttpTelemetry.Log.Http20ConnectionClosed();
		}
	}

	public override void Dispose()
	{
		lock (SyncObject)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"_disposed"}={_disposed}, {"_streamsInUse"}={_streamsInUse}", "Dispose");
			}
			if (!_disposed)
			{
				SignalAvailableStreamsWaiter(result: false);
				SignalShutdownWaiter();
				_disposed = true;
				if (_streamsInUse == 0)
				{
					FinalTeardown();
				}
			}
		}
	}

	public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{request}", "SendAsync");
		}
		try
		{
			bool mustFlush = request.Content != null && request.HasHeaders && request.Headers.ExpectContinue == true;
			Http2Stream http2Stream = await SendHeadersAsync(request, cancellationToken, mustFlush).ConfigureAwait(continueOnCapturedContext: false);
			bool flag = request.Content != null && request.Content.AllowDuplex;
			CancellationToken cancellationToken2 = (flag ? CancellationToken.None : cancellationToken);
			Task requestBodyTask = http2Stream.SendRequestBodyAsync(cancellationToken2);
			Task responseHeadersTask = http2Stream.ReadResponseHeadersAsync(cancellationToken);
			bool flag2 = requestBodyTask.IsCompleted || !flag;
			bool flag3 = flag2;
			if (!flag3)
			{
				flag3 = await Task.WhenAny(requestBodyTask, responseHeadersTask).ConfigureAwait(continueOnCapturedContext: false) == requestBodyTask;
			}
			if (flag3 || requestBodyTask.IsCompleted || http2Stream.SendRequestFinished)
			{
				try
				{
					await requestBodyTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception value)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Sending request content failed: {value}", "SendAsync");
					}
					LogExceptions(responseHeadersTask);
					throw;
				}
			}
			else
			{
				LogExceptions(requestBodyTask);
			}
			await responseHeadersTask.ConfigureAwait(continueOnCapturedContext: false);
			return http2Stream.GetAndClearResponse();
		}
		catch (Exception ex)
		{
			if (ex is IOException || ex is ObjectDisposedException || ex is Http2ProtocolException || ex is InvalidOperationException)
			{
				throw new HttpRequestException(System.SR.net_http_client_execution_error, ex);
			}
			throw;
		}
	}

	private void RemoveStream(Http2Stream http2Stream)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace(http2Stream.StreamId, "", "RemoveStream");
		}
		lock (SyncObject)
		{
			if (!_httpStreams.Remove(http2Stream.StreamId))
			{
				return;
			}
		}
		ReleaseStream();
	}

	private void RefreshPingTimestamp()
	{
		_nextPingRequestTimestamp = Environment.TickCount64 + _keepAlivePingDelay;
	}

	private void ProcessPingAck(long payload)
	{
		if (payload < 0)
		{
			_rttEstimator.OnPingAckReceived(payload, this);
			return;
		}
		if (_keepAliveState != KeepAliveState.PingSent)
		{
			ThrowProtocolError();
		}
		if (Interlocked.Read(ref _keepAlivePingPayload) != payload)
		{
			ThrowProtocolError();
		}
		_keepAliveState = KeepAliveState.None;
	}

	private void VerifyKeepAlive()
	{
		if (_keepAlivePingPolicy == HttpKeepAlivePingPolicy.WithActiveRequests)
		{
			lock (SyncObject)
			{
				if (_streamsInUse == 0)
				{
					return;
				}
			}
		}
		long tickCount = Environment.TickCount64;
		switch (_keepAliveState)
		{
		case KeepAliveState.None:
			if (tickCount > _nextPingRequestTimestamp)
			{
				_keepAliveState = KeepAliveState.PingSent;
				_keepAlivePingTimeoutTimestamp = tickCount + _keepAlivePingTimeout;
				long pingContent = Interlocked.Increment(ref _keepAlivePingPayload);
				SendPingAsync(pingContent);
			}
			break;
		case KeepAliveState.PingSent:
			if (tickCount > _keepAlivePingTimeoutTimestamp)
			{
				ThrowProtocolError();
			}
			break;
		}
	}

	public sealed override string ToString()
	{
		return $"{"Http2Connection"}({_pool})";
	}

	public override void Trace(string message, [CallerMemberName] string memberName = null)
	{
		Trace(0, message, memberName);
	}

	internal void Trace(int streamId, string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(_pool?.GetHashCode() ?? 0, GetHashCode(), streamId, memberName, message);
	}

	[DoesNotReturn]
	private static void ThrowRetry(string message, Exception innerException = null)
	{
		throw new HttpRequestException(message, innerException, RequestRetryType.RetryOnConnectionFailure);
	}

	private static Exception GetRequestAbortedException(Exception innerException = null)
	{
		return new IOException(System.SR.net_http_request_aborted, innerException);
	}

	[DoesNotReturn]
	private static void ThrowRequestAborted(Exception innerException = null)
	{
		throw GetRequestAbortedException(innerException);
	}

	[DoesNotReturn]
	private static void ThrowProtocolError()
	{
		ThrowProtocolError(Http2ProtocolErrorCode.ProtocolError);
	}

	[DoesNotReturn]
	private static void ThrowProtocolError(Http2ProtocolErrorCode errorCode)
	{
		throw new Http2ConnectionException(errorCode);
	}
}
