using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpConnection : HttpConnectionBase, IDisposable
{
	private sealed class ChunkedEncodingReadStream : HttpContentReadStream
	{
		private enum ParsingState : byte
		{
			ExpectChunkHeader,
			ExpectChunkData,
			ExpectChunkTerminator,
			ConsumeTrailers,
			Done
		}

		private ulong _chunkBytesRemaining;

		private ParsingState _state;

		private readonly HttpResponseMessage _response;

		public override bool NeedsDrain => base.CanReadFromConnection;

		public ChunkedEncodingReadStream(HttpConnection connection, HttpResponseMessage response)
			: base(connection)
		{
			_response = response;
		}

		public override int Read(Span<byte> buffer)
		{
			if (_connection == null || buffer.Length == 0)
			{
				return 0;
			}
			int num = ReadChunksFromConnectionBuffer(buffer, default(CancellationTokenRegistration));
			if (num > 0)
			{
				return num;
			}
			int num2;
			do
			{
				if (_connection == null)
				{
					return 0;
				}
				if (_state == ParsingState.ExpectChunkData && buffer.Length >= _connection.ReadBufferSize && _chunkBytesRemaining >= (ulong)_connection.ReadBufferSize)
				{
					num = _connection.Read(buffer.Slice(0, (int)Math.Min((ulong)buffer.Length, _chunkBytesRemaining)));
					if (num == 0)
					{
						throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _chunkBytesRemaining));
					}
					_chunkBytesRemaining -= (ulong)num;
					if (_chunkBytesRemaining == 0L)
					{
						_state = ParsingState.ExpectChunkTerminator;
					}
					return num;
				}
				_connection.Fill();
				num2 = ReadChunksFromConnectionBuffer(buffer, default(CancellationTokenRegistration));
			}
			while (num2 <= 0);
			return num2;
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return ValueTask.FromCanceled<int>(cancellationToken);
			}
			if (_connection == null || buffer.Length == 0)
			{
				return new ValueTask<int>(0);
			}
			int num = ReadChunksFromConnectionBuffer(buffer.Span, default(CancellationTokenRegistration));
			if (num > 0)
			{
				return new ValueTask<int>(num);
			}
			if (_connection == null)
			{
				return new ValueTask<int>(0);
			}
			return ReadAsyncCore(buffer, cancellationToken);
		}

		private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
			try
			{
				int num2;
				do
				{
					if (_connection == null)
					{
						return 0;
					}
					if (_state == ParsingState.ExpectChunkData && buffer.Length >= _connection.ReadBufferSize && _chunkBytesRemaining >= (ulong)_connection.ReadBufferSize)
					{
						int num = await _connection.ReadAsync(buffer.Slice(0, (int)Math.Min((ulong)buffer.Length, _chunkBytesRemaining))).ConfigureAwait(continueOnCapturedContext: false);
						if (num == 0)
						{
							throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _chunkBytesRemaining));
						}
						_chunkBytesRemaining -= (ulong)num;
						if (_chunkBytesRemaining == 0L)
						{
							_state = ParsingState.ExpectChunkTerminator;
						}
						return num;
					}
					await _connection.FillAsync(async: true).ConfigureAwait(continueOnCapturedContext: false);
					num2 = ReadChunksFromConnectionBuffer(buffer.Span, ctr);
				}
				while (num2 <= 0);
				return num2;
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (!cancellationToken.IsCancellationRequested)
			{
				if (_connection != null)
				{
					return CopyToAsyncCore(destination, cancellationToken);
				}
				return Task.CompletedTask;
			}
			return Task.FromCanceled(cancellationToken);
		}

		private async Task CopyToAsyncCore(Stream destination, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
			try
			{
				while (true)
				{
					ReadOnlyMemory<byte> buffer = ReadChunkFromConnectionBuffer(int.MaxValue, ctr);
					if (buffer.Length != 0)
					{
						await destination.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						continue;
					}
					if (_connection == null)
					{
						break;
					}
					await _connection.FillAsync(async: true).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
		}

		private int ReadChunksFromConnectionBuffer(Span<byte> buffer, CancellationTokenRegistration cancellationRegistration)
		{
			int num = 0;
			while (buffer.Length > 0)
			{
				ReadOnlyMemory<byte> readOnlyMemory = ReadChunkFromConnectionBuffer(buffer.Length, cancellationRegistration);
				if (readOnlyMemory.Length == 0)
				{
					break;
				}
				num += readOnlyMemory.Length;
				readOnlyMemory.Span.CopyTo(buffer);
				buffer = buffer.Slice(readOnlyMemory.Length);
			}
			return num;
		}

		private ReadOnlyMemory<byte> ReadChunkFromConnectionBuffer(int maxBytesToRead, CancellationTokenRegistration cancellationRegistration)
		{
			try
			{
				ReadOnlySpan<byte> line;
				switch (_state)
				{
				case ParsingState.ExpectChunkHeader:
				{
					_connection._allowedReadLineBytes = 16384;
					if (!_connection.TryReadNextLine(out line))
					{
						return default(ReadOnlyMemory<byte>);
					}
					if (!Utf8Parser.TryParse(line, out ulong value, out int bytesConsumed, 'X'))
					{
						throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_chunk_header_invalid, BitConverter.ToString(line.ToArray())));
					}
					_chunkBytesRemaining = value;
					if (bytesConsumed != line.Length)
					{
						ValidateChunkExtension(line.Slice(bytesConsumed));
					}
					if (value != 0)
					{
						_state = ParsingState.ExpectChunkData;
						goto case ParsingState.ExpectChunkData;
					}
					_state = ParsingState.ConsumeTrailers;
					goto case ParsingState.ConsumeTrailers;
				}
				case ParsingState.ExpectChunkData:
				{
					ReadOnlyMemory<byte> remainingBuffer = _connection.RemainingBuffer;
					if (remainingBuffer.Length == 0)
					{
						return default(ReadOnlyMemory<byte>);
					}
					int num = Math.Min(maxBytesToRead, (int)Math.Min((ulong)remainingBuffer.Length, _chunkBytesRemaining));
					_connection.ConsumeFromRemainingBuffer(num);
					_chunkBytesRemaining -= (ulong)num;
					if (_chunkBytesRemaining == 0L)
					{
						_state = ParsingState.ExpectChunkTerminator;
					}
					return remainingBuffer.Slice(0, num);
				}
				case ParsingState.ExpectChunkTerminator:
					_connection._allowedReadLineBytes = 16384;
					if (!_connection.TryReadNextLine(out line))
					{
						return default(ReadOnlyMemory<byte>);
					}
					if (line.Length != 0)
					{
						throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_chunk_terminator_invalid, Encoding.ASCII.GetString(line)));
					}
					_state = ParsingState.ExpectChunkHeader;
					goto case ParsingState.ExpectChunkHeader;
				case ParsingState.ConsumeTrailers:
					while (true)
					{
						_connection._allowedReadLineBytes = 16384;
						if (!_connection.TryReadNextLine(out line))
						{
							break;
						}
						if (line.IsEmpty)
						{
							cancellationRegistration.Dispose();
							CancellationHelper.ThrowIfCancellationRequested(cancellationRegistration.Token);
							_state = ParsingState.Done;
							_connection.CompleteResponse();
							_connection = null;
							break;
						}
						if (!base.IsDisposed)
						{
							ParseHeaderNameValue(_connection, line, _response, isFromTrailer: true);
						}
					}
					return default(ReadOnlyMemory<byte>);
				default:
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, $"Unexpected state: {_state}", "ReadChunkFromConnectionBuffer");
					}
					return default(ReadOnlyMemory<byte>);
				}
			}
			catch (Exception)
			{
				_connection.Dispose();
				_connection = null;
				throw;
			}
		}

		private static void ValidateChunkExtension(ReadOnlySpan<byte> lineAfterChunkSize)
		{
			for (int i = 0; i < lineAfterChunkSize.Length; i++)
			{
				switch (lineAfterChunkSize[i])
				{
				case 9:
				case 32:
					continue;
				case 59:
					return;
				}
				throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_chunk_extension_invalid, BitConverter.ToString(lineAfterChunkSize.ToArray())));
			}
		}

		public override async ValueTask<bool> DrainAsync(int maxDrainBytes)
		{
			CancellationTokenSource cts = null;
			CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
			try
			{
				int drainedBytes = 0;
				while (true)
				{
					drainedBytes += _connection.RemainingBuffer.Length;
					while (ReadChunkFromConnectionBuffer(int.MaxValue, ctr).Length != 0)
					{
					}
					if (_connection == null)
					{
						return true;
					}
					if (drainedBytes >= maxDrainBytes)
					{
						return false;
					}
					if (cts == null)
					{
						TimeSpan maxResponseDrainTime = _connection._pool.Settings._maxResponseDrainTime;
						if (maxResponseDrainTime == TimeSpan.Zero)
						{
							break;
						}
						if (maxResponseDrainTime != Timeout.InfiniteTimeSpan)
						{
							cts = new CancellationTokenSource((int)maxResponseDrainTime.TotalMilliseconds);
							ctr = cts.Token.Register(delegate(object s)
							{
								((HttpConnection)s).Dispose();
							}, _connection);
						}
					}
					await _connection.FillAsync(async: true).ConfigureAwait(continueOnCapturedContext: false);
				}
				return false;
			}
			finally
			{
				ctr.Dispose();
				cts?.Dispose();
			}
		}
	}

	private sealed class ChunkedEncodingWriteStream : HttpContentWriteStream
	{
		private static readonly byte[] s_finalChunkBytes = new byte[5] { 48, 13, 10, 13, 10 };

		public ChunkedEncodingWriteStream(HttpConnection connection)
			: base(connection)
		{
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			base.BytesWritten += buffer.Length;
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			if (buffer.Length == 0)
			{
				connectionOrThrow.Flush();
				return;
			}
			connectionOrThrow.WriteHexInt32Async(buffer.Length, async: false).GetAwaiter().GetResult();
			connectionOrThrow.WriteTwoBytesAsync(13, 10, async: false).GetAwaiter().GetResult();
			connectionOrThrow.Write(buffer);
			connectionOrThrow.WriteTwoBytesAsync(13, 10, async: false).GetAwaiter().GetResult();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored)
		{
			base.BytesWritten += buffer.Length;
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			return (buffer.Length == 0) ? connectionOrThrow.FlushAsync(async: true) : WriteChunkAsync(connectionOrThrow, buffer);
			static async ValueTask WriteChunkAsync(HttpConnection connection, ReadOnlyMemory<byte> buffer)
			{
				await connection.WriteHexInt32Async(buffer.Length, async: true).ConfigureAwait(continueOnCapturedContext: false);
				await connection.WriteTwoBytesAsync(13, 10, async: true).ConfigureAwait(continueOnCapturedContext: false);
				await connection.WriteAsync(buffer, async: true).ConfigureAwait(continueOnCapturedContext: false);
				await connection.WriteTwoBytesAsync(13, 10, async: true).ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		public override Task FinishAsync(bool async)
		{
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			_connection = null;
			return connectionOrThrow.WriteBytesAsync(s_finalChunkBytes, async);
		}
	}

	private sealed class ConnectionCloseReadStream : HttpContentReadStream
	{
		public ConnectionCloseReadStream(HttpConnection connection)
			: base(connection)
		{
		}

		public override int Read(Span<byte> buffer)
		{
			HttpConnection connection = _connection;
			if (connection == null || buffer.Length == 0)
			{
				return 0;
			}
			int num = connection.Read(buffer);
			if (num == 0)
			{
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			HttpConnection connection = _connection;
			if (connection == null || buffer.Length == 0)
			{
				return 0;
			}
			ValueTask<int> valueTask = connection.ReadAsync(buffer);
			int num;
			if (valueTask.IsCompletedSuccessfully)
			{
				num = valueTask.Result;
			}
			else
			{
				CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
				try
				{
					num = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
				{
					throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
				}
				finally
				{
					ctr.Dispose();
				}
			}
			if (num == 0)
			{
				CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return Task.CompletedTask;
			}
			Task task = connection.CopyToUntilEofAsync(destination, async: true, bufferSize, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				Finish(connection);
				return Task.CompletedTask;
			}
			return CompleteCopyToAsync(task, connection, cancellationToken);
		}

		private async Task CompleteCopyToAsync(Task copyTask, HttpConnection connection, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			Finish(connection);
		}

		private void Finish(HttpConnection connection)
		{
			_connection = null;
			connection.Dispose();
		}
	}

	private sealed class ContentLengthReadStream : HttpContentReadStream
	{
		private ulong _contentBytesRemaining;

		public override bool NeedsDrain => base.CanReadFromConnection;

		public ContentLengthReadStream(HttpConnection connection, ulong contentLength)
			: base(connection)
		{
			_contentBytesRemaining = contentLength;
		}

		public override int Read(Span<byte> buffer)
		{
			if (_connection == null || buffer.Length == 0)
			{
				return 0;
			}
			if ((ulong)buffer.Length > _contentBytesRemaining)
			{
				buffer = buffer.Slice(0, (int)_contentBytesRemaining);
			}
			int num = _connection.Read(buffer);
			if (num <= 0)
			{
				throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _contentBytesRemaining));
			}
			_contentBytesRemaining -= (ulong)num;
			if (_contentBytesRemaining == 0L)
			{
				_connection.CompleteResponse();
				_connection = null;
			}
			return num;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			if (_connection == null || buffer.Length == 0)
			{
				return 0;
			}
			if ((ulong)buffer.Length > _contentBytesRemaining)
			{
				buffer = buffer.Slice(0, (int)_contentBytesRemaining);
			}
			ValueTask<int> valueTask = _connection.ReadAsync(buffer);
			int num;
			if (valueTask.IsCompletedSuccessfully)
			{
				num = valueTask.Result;
			}
			else
			{
				CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
				try
				{
					num = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
				{
					throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
				}
				finally
				{
					ctr.Dispose();
				}
			}
			if (num <= 0)
			{
				CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
				throw new IOException(System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _contentBytesRemaining));
			}
			_contentBytesRemaining -= (ulong)num;
			if (_contentBytesRemaining == 0L)
			{
				_connection.CompleteResponse();
				_connection = null;
			}
			return num;
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			if (_connection == null)
			{
				return Task.CompletedTask;
			}
			Task task = _connection.CopyToContentLengthAsync(destination, async: true, _contentBytesRemaining, bufferSize, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				Finish();
				return Task.CompletedTask;
			}
			return CompleteCopyToAsync(task, cancellationToken);
		}

		private async Task CompleteCopyToAsync(Task copyTask, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
			Finish();
		}

		private void Finish()
		{
			_contentBytesRemaining = 0uL;
			_connection.CompleteResponse();
			_connection = null;
		}

		private ReadOnlyMemory<byte> ReadFromConnectionBuffer(int maxBytesToRead)
		{
			ReadOnlyMemory<byte> remainingBuffer = _connection.RemainingBuffer;
			if (remainingBuffer.Length == 0)
			{
				return default(ReadOnlyMemory<byte>);
			}
			int num = Math.Min(maxBytesToRead, (int)Math.Min((ulong)remainingBuffer.Length, _contentBytesRemaining));
			_connection.ConsumeFromRemainingBuffer(num);
			_contentBytesRemaining -= (ulong)num;
			return remainingBuffer.Slice(0, num);
		}

		public override async ValueTask<bool> DrainAsync(int maxDrainBytes)
		{
			ReadFromConnectionBuffer(int.MaxValue);
			if (_contentBytesRemaining == 0L)
			{
				Finish();
				return true;
			}
			if (_contentBytesRemaining > (ulong)maxDrainBytes)
			{
				return false;
			}
			CancellationTokenSource cts = null;
			CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
			TimeSpan maxResponseDrainTime = _connection._pool.Settings._maxResponseDrainTime;
			if (maxResponseDrainTime == TimeSpan.Zero)
			{
				return false;
			}
			if (maxResponseDrainTime != Timeout.InfiniteTimeSpan)
			{
				cts = new CancellationTokenSource((int)maxResponseDrainTime.TotalMilliseconds);
				ctr = cts.Token.Register(delegate(object s)
				{
					((HttpConnection)s).Dispose();
				}, _connection);
			}
			try
			{
				do
				{
					await _connection.FillAsync(async: true).ConfigureAwait(continueOnCapturedContext: false);
					ReadFromConnectionBuffer(int.MaxValue);
				}
				while (_contentBytesRemaining != 0L);
				ctr.Dispose();
				CancellationHelper.ThrowIfCancellationRequested(ctr.Token);
				Finish();
				return true;
			}
			finally
			{
				ctr.Dispose();
				cts?.Dispose();
			}
		}
	}

	private sealed class ContentLengthWriteStream : HttpContentWriteStream
	{
		public ContentLengthWriteStream(HttpConnection connection)
			: base(connection)
		{
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			base.BytesWritten += buffer.Length;
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			connectionOrThrow.Write(buffer);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored)
		{
			base.BytesWritten += buffer.Length;
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			return connectionOrThrow.WriteAsync(buffer, async: true);
		}

		public override Task FinishAsync(bool async)
		{
			_connection = null;
			return Task.CompletedTask;
		}
	}

	internal abstract class HttpContentReadStream : HttpContentStream
	{
		private int _disposed;

		public sealed override bool CanRead => _disposed == 0;

		public sealed override bool CanWrite => false;

		public virtual bool NeedsDrain => false;

		protected bool IsDisposed => _disposed == 1;

		protected bool CanReadFromConnection
		{
			get
			{
				HttpConnection connection = _connection;
				if (connection != null)
				{
					return connection._disposed != 1;
				}
				return false;
			}
		}

		public HttpContentReadStream(HttpConnection connection)
			: base(connection)
		{
		}

		public sealed override void Write(ReadOnlySpan<byte> buffer)
		{
			throw new NotSupportedException(System.SR.net_http_content_readonly_stream);
		}

		public sealed override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public virtual ValueTask<bool> DrainAsync(int maxDrainBytes)
		{
			return new ValueTask<bool>(result: false);
		}

		protected override void Dispose(bool disposing)
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				if (disposing && NeedsDrain)
				{
					DrainOnDisposeAsync();
				}
				else
				{
					base.Dispose(disposing);
				}
			}
		}

		private async Task DrainOnDisposeAsync()
		{
			HttpConnection connection = _connection;
			try
			{
				bool flag = await DrainAsync(connection._pool.Settings._maxResponseDrainSize).ConfigureAwait(continueOnCapturedContext: false);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace(flag ? "Connection drain succeeded" : $"Connection drain failed when MaxResponseDrainSize={connection._pool.Settings._maxResponseDrainSize} bytes or MaxResponseDrainTime=={connection._pool.Settings._maxResponseDrainTime} exceeded", "DrainOnDisposeAsync");
				}
			}
			catch (Exception value)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"Connection drain failed due to exception: {value}", "DrainOnDisposeAsync");
				}
			}
			base.Dispose(disposing: true);
		}
	}

	private abstract class HttpContentWriteStream : HttpContentStream
	{
		public long BytesWritten { get; protected set; }

		public sealed override bool CanRead => false;

		public sealed override bool CanWrite => _connection != null;

		public HttpContentWriteStream(HttpConnection connection)
			: base(connection)
		{
		}

		public sealed override void Flush()
		{
			_connection?.Flush();
		}

		public sealed override Task FlushAsync(CancellationToken ignored)
		{
			return _connection?.FlushAsync(async: true).AsTask();
		}

		public sealed override int Read(Span<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public sealed override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public sealed override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public abstract Task FinishAsync(bool async);
	}

	private sealed class RawConnectionStream : HttpContentStream
	{
		public sealed override bool CanRead => _connection != null;

		public sealed override bool CanWrite => _connection != null;

		public RawConnectionStream(HttpConnection connection)
			: base(connection)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, null, ".ctor");
			}
		}

		public override int Read(Span<byte> buffer)
		{
			HttpConnection connection = _connection;
			if (connection == null || buffer.Length == 0)
			{
				return 0;
			}
			int num = connection.ReadBuffered(buffer);
			if (num == 0)
			{
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			HttpConnection connection = _connection;
			if (connection == null || buffer.Length == 0)
			{
				return 0;
			}
			ValueTask<int> valueTask = connection.ReadBufferedAsync(buffer);
			int num;
			if (valueTask.IsCompletedSuccessfully)
			{
				num = valueTask.Result;
			}
			else
			{
				CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
				try
				{
					num = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
				{
					throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
				}
				finally
				{
					ctr.Dispose();
				}
			}
			if (num == 0)
			{
				CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return Task.CompletedTask;
			}
			Task task = connection.CopyToUntilEofAsync(destination, async: true, bufferSize, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				Finish(connection);
				return Task.CompletedTask;
			}
			return CompleteCopyToAsync(task, connection, cancellationToken);
		}

		private async Task CompleteCopyToAsync(Task copyTask, HttpConnection connection, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			Finish(connection);
		}

		private void Finish(HttpConnection connection)
		{
			connection.Dispose();
			_connection = null;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			HttpConnection connection = _connection;
			if (connection == null)
			{
				throw new IOException(System.SR.ObjectDisposed_StreamClosed);
			}
			if (buffer.Length != 0)
			{
				connection.WriteWithoutBuffering(buffer);
			}
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return ValueTask.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(System.SR.ObjectDisposed_StreamClosed)));
			}
			if (buffer.Length == 0)
			{
				return default(ValueTask);
			}
			ValueTask valueTask = connection.WriteWithoutBufferingAsync(buffer, async: true);
			if (!valueTask.IsCompleted)
			{
				return new ValueTask(WaitWithConnectionCancellationAsync(valueTask, connection, cancellationToken));
			}
			return valueTask;
		}

		public override void Flush()
		{
			_connection?.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return Task.CompletedTask;
			}
			ValueTask task = connection.FlushAsync(async: true);
			if (!task.IsCompleted)
			{
				return WaitWithConnectionCancellationAsync(task, connection, cancellationToken);
			}
			return task.AsTask();
		}

		private static async Task WaitWithConnectionCancellationAsync(ValueTask task, HttpConnection connection, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
			try
			{
				await task.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
		}
	}

	private static readonly byte[] s_contentLength0NewlineAsciiBytes = Encoding.ASCII.GetBytes("Content-Length: 0\r\n");

	private static readonly byte[] s_spaceHttp10NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.0\r\n");

	private static readonly byte[] s_spaceHttp11NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.1\r\n");

	private static readonly byte[] s_httpSchemeAndDelimiter = Encoding.ASCII.GetBytes(Uri.UriSchemeHttp + Uri.SchemeDelimiter);

	private static readonly byte[] s_http1DotBytes = Encoding.ASCII.GetBytes("HTTP/1.");

	private static readonly ulong s_http10Bytes = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("HTTP/1.0"));

	private static readonly ulong s_http11Bytes = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("HTTP/1.1"));

	private readonly HttpConnectionPool _pool;

	private readonly Socket _socket;

	private readonly Stream _stream;

	private readonly TransportContext _transportContext;

	private readonly WeakReference<HttpConnection> _weakThisRef;

	private HttpRequestMessage _currentRequest;

	private readonly byte[] _writeBuffer;

	private int _writeOffset;

	private int _allowedReadLineBytes;

	private string[] _headerValues = Array.Empty<string>();

	private ValueTask<int>? _readAheadTask;

	private int _readAheadTaskLock;

	private byte[] _readBuffer;

	private int _readOffset;

	private int _readLength;

	private long _idleSinceTickCount;

	private bool _inUse;

	private bool _detachedFromPool;

	private bool _canRetry;

	private bool _startedSendingRequestBody;

	private bool _connectionClose;

	private int _disposed;

	public TransportContext TransportContext => _transportContext;

	public HttpConnectionKind Kind => _pool.Kind;

	private int ReadBufferSize => _readBuffer.Length;

	private ReadOnlyMemory<byte> RemainingBuffer => new ReadOnlyMemory<byte>(_readBuffer, _readOffset, _readLength - _readOffset);

	public HttpConnection(HttpConnectionPool pool, Socket socket, Stream stream, TransportContext transportContext)
	{
		_pool = pool;
		_stream = stream;
		_socket = socket;
		_transportContext = transportContext;
		_writeBuffer = new byte[4096];
		_readBuffer = new byte[4096];
		_weakThisRef = new WeakReference<HttpConnection>(this);
		_idleSinceTickCount = Environment.TickCount64;
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.Http11ConnectionEstablished();
			_disposed = 2;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			TraceConnection(_stream);
		}
	}

	~HttpConnection()
	{
		Dispose(disposing: false);
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
	}

	private void Dispose(bool disposing)
	{
		int num = Interlocked.Exchange(ref _disposed, 1);
		if (num == 1)
		{
			return;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Connection closing.", "Dispose");
		}
		if (HttpTelemetry.Log.IsEnabled() && num == 2)
		{
			HttpTelemetry.Log.Http11ConnectionClosed();
		}
		if (!_detachedFromPool)
		{
			_pool.InvalidateHttp11Connection(this, disposing);
		}
		if (disposing)
		{
			GC.SuppressFinalize(this);
			_stream.Dispose();
			ValueTask<int>? valueTask = ConsumeReadAheadTask();
			if (valueTask.HasValue)
			{
				HttpConnectionBase.IgnoreExceptions(valueTask.GetValueOrDefault());
			}
		}
	}

	public bool PrepareForReuse(bool async)
	{
		ValueTask<int>? readAheadTask = _readAheadTask;
		if (readAheadTask.HasValue)
		{
			return !_readAheadTask.Value.IsCompleted;
		}
		if (!async && _socket != null)
		{
			try
			{
				return !_socket.Poll(0, SelectMode.SelectRead);
			}
			catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
			{
				return false;
			}
		}
		try
		{
			_readAheadTask = _stream.ReadAsync(new Memory<byte>(_readBuffer));
			return !_readAheadTask.Value.IsCompleted;
		}
		catch (Exception value)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Error performing read ahead: {value}", "PrepareForReuse");
			}
			return false;
		}
	}

	public override bool CheckUsabilityOnScavenge()
	{
		ValueTask<int>? readAheadTask = _readAheadTask;
		if (!readAheadTask.HasValue)
		{
			_readAheadTask = ReadAheadWithZeroByteReadAsync();
		}
		return !_readAheadTask.Value.IsCompleted;
		async ValueTask<int> ReadAheadWithZeroByteReadAsync()
		{
			await _stream.ReadAsync(Memory<byte>.Empty).ConfigureAwait(continueOnCapturedContext: false);
			return await _stream.ReadAsync(new Memory<byte>(_readBuffer)).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private ValueTask<int>? ConsumeReadAheadTask()
	{
		if (Interlocked.CompareExchange(ref _readAheadTaskLock, 1, 0) == 0)
		{
			ValueTask<int>? readAheadTask = _readAheadTask;
			_readAheadTask = null;
			Volatile.Write(ref _readAheadTaskLock, 0);
			return readAheadTask;
		}
		return null;
	}

	public override long GetIdleTicks(long nowTicks)
	{
		return nowTicks - _idleSinceTickCount;
	}

	private void ConsumeFromRemainingBuffer(int bytesToConsume)
	{
		_readOffset += bytesToConsume;
	}

	private async ValueTask WriteHeadersAsync(HttpHeaders headers, string cookiesFromContainer, bool async)
	{
		if (headers.HeaderStore != null)
		{
			foreach (KeyValuePair<HeaderDescriptor, object> header in headers.HeaderStore)
			{
				if (header.Key.KnownHeader == null)
				{
					await WriteAsciiStringAsync(header.Key.Name, async).ConfigureAwait(continueOnCapturedContext: false);
					await WriteTwoBytesAsync(58, 32, async).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await WriteBytesAsync(header.Key.KnownHeader.AsciiBytesWithColonSpace, async).ConfigureAwait(continueOnCapturedContext: false);
				}
				int headerValuesCount = HttpHeaders.GetStoreValuesIntoStringArray(header.Key, header.Value, ref _headerValues);
				if (headerValuesCount > 0)
				{
					Encoding valueEncoding = _pool.Settings._requestHeaderEncodingSelector?.Invoke(header.Key.Name, _currentRequest);
					await WriteStringAsync(_headerValues[0], async, valueEncoding).ConfigureAwait(continueOnCapturedContext: false);
					if (cookiesFromContainer != null && header.Key.KnownHeader == KnownHeaders.Cookie)
					{
						await WriteTwoBytesAsync(59, 32, async).ConfigureAwait(continueOnCapturedContext: false);
						await WriteStringAsync(cookiesFromContainer, async, valueEncoding).ConfigureAwait(continueOnCapturedContext: false);
						cookiesFromContainer = null;
					}
					if (headerValuesCount > 1)
					{
						HttpHeaderParser parser = header.Key.Parser;
						string separator = ", ";
						if (parser != null && parser.SupportsMultipleValues)
						{
							separator = parser.Separator;
						}
						for (int i = 1; i < headerValuesCount; i++)
						{
							await WriteAsciiStringAsync(separator, async).ConfigureAwait(continueOnCapturedContext: false);
							await WriteStringAsync(_headerValues[i], async, valueEncoding).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
				}
				await WriteTwoBytesAsync(13, 10, async).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		if (cookiesFromContainer != null)
		{
			await WriteAsciiStringAsync("Cookie", async).ConfigureAwait(continueOnCapturedContext: false);
			await WriteTwoBytesAsync(58, 32, async).ConfigureAwait(continueOnCapturedContext: false);
			Encoding encoding = _pool.Settings._requestHeaderEncodingSelector?.Invoke("Cookie", _currentRequest);
			await WriteStringAsync(cookiesFromContainer, async, encoding).ConfigureAwait(continueOnCapturedContext: false);
			await WriteTwoBytesAsync(13, 10, async).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async ValueTask WriteHostHeaderAsync(Uri uri, bool async)
	{
		await WriteBytesAsync(KnownHeaders.Host.AsciiBytesWithColonSpace, async).ConfigureAwait(continueOnCapturedContext: false);
		if (_pool.HostHeaderValueBytes != null)
		{
			await WriteBytesAsync(_pool.HostHeaderValueBytes, async).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			if (uri.HostNameType != UriHostNameType.IPv6)
			{
				await WriteAsciiStringAsync(uri.IdnHost, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await WriteByteAsync(91, async).ConfigureAwait(continueOnCapturedContext: false);
				await WriteAsciiStringAsync(uri.IdnHost, async).ConfigureAwait(continueOnCapturedContext: false);
				await WriteByteAsync(93, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (!uri.IsDefaultPort)
			{
				await WriteByteAsync(58, async).ConfigureAwait(continueOnCapturedContext: false);
				await WriteDecimalInt32Async(uri.Port, async).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		await WriteTwoBytesAsync(13, 10, async).ConfigureAwait(continueOnCapturedContext: false);
	}

	private Task WriteDecimalInt32Async(int value, bool async)
	{
		if (Utf8Formatter.TryFormat(value, new Span<byte>(_writeBuffer, _writeOffset, _writeBuffer.Length - _writeOffset), out var bytesWritten))
		{
			_writeOffset += bytesWritten;
			return Task.CompletedTask;
		}
		return WriteAsciiStringAsync(value.ToString(), async);
	}

	private Task WriteHexInt32Async(int value, bool async)
	{
		if (Utf8Formatter.TryFormat(value, new Span<byte>(_writeBuffer, _writeOffset, _writeBuffer.Length - _writeOffset), out var bytesWritten, 'X'))
		{
			_writeOffset += bytesWritten;
			return Task.CompletedTask;
		}
		return WriteAsciiStringAsync(value.ToString("X", CultureInfo.InvariantCulture), async);
	}

	public async Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> allowExpect100ToContinue = null;
		Task sendRequestContentTask = null;
		_currentRequest = request;
		HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);
		_canRetry = false;
		_startedSendingRequestBody = false;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Sending request: {request}", "SendAsyncCore");
		}
		CancellationTokenRegistration cancellationRegistration = RegisterCancellation(cancellationToken);
		Unsafe.SkipInit(out HttpResponseMessage result);
		try
		{
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStart();
			}
			await WriteStringAsync(normalizedMethod.Method, async).ConfigureAwait(continueOnCapturedContext: false);
			await WriteByteAsync(32, async).ConfigureAwait(continueOnCapturedContext: false);
			if ((object)normalizedMethod == HttpMethod.Connect)
			{
				if (!request.HasHeaders || request.Headers.Host == null)
				{
					throw new HttpRequestException(System.SR.net_http_request_no_host);
				}
				await WriteAsciiStringAsync(request.Headers.Host, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				if (Kind == HttpConnectionKind.Proxy)
				{
					await WriteBytesAsync(s_httpSchemeAndDelimiter, async).ConfigureAwait(continueOnCapturedContext: false);
					if (request.RequestUri.HostNameType != UriHostNameType.IPv6)
					{
						await WriteAsciiStringAsync(request.RequestUri.IdnHost, async).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						await WriteByteAsync(91, async).ConfigureAwait(continueOnCapturedContext: false);
						await WriteAsciiStringAsync(request.RequestUri.IdnHost, async).ConfigureAwait(continueOnCapturedContext: false);
						await WriteByteAsync(93, async).ConfigureAwait(continueOnCapturedContext: false);
					}
					if (!request.RequestUri.IsDefaultPort)
					{
						await WriteByteAsync(58, async).ConfigureAwait(continueOnCapturedContext: false);
						await WriteDecimalInt32Async(request.RequestUri.Port, async).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				await WriteStringAsync(request.RequestUri.PathAndQuery, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			bool flag = request.Version.Minor == 0 && request.Version.Major == 1;
			await WriteBytesAsync(flag ? s_spaceHttp10NewlineAsciiBytes : s_spaceHttp11NewlineAsciiBytes, async).ConfigureAwait(continueOnCapturedContext: false);
			string cookiesFromContainer = null;
			if (_pool.Settings._useCookies)
			{
				cookiesFromContainer = _pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
				if (cookiesFromContainer == "")
				{
					cookiesFromContainer = null;
				}
			}
			if (!request.HasHeaders || request.Headers.Host == null)
			{
				await WriteHostHeaderAsync(request.RequestUri, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (request.HasHeaders || cookiesFromContainer != null)
			{
				await WriteHeadersAsync(request.Headers, cookiesFromContainer, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (request.Content != null)
			{
				await WriteHeadersAsync(request.Content.Headers, null, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else if (normalizedMethod.MustHaveRequestBody)
			{
				await WriteBytesAsync(s_contentLength0NewlineAsciiBytes, async).ConfigureAwait(continueOnCapturedContext: false);
			}
			await WriteTwoBytesAsync(13, 10, async).ConfigureAwait(continueOnCapturedContext: false);
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStop();
			}
			if (request.Content == null)
			{
				await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				bool flag2 = request.HasHeaders && request.Headers.ExpectContinue == true;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Request content is not null, start processing it. hasExpectContinueHeader = {flag2}", "SendAsyncCore");
				}
				if (!flag2)
				{
					await SendRequestContentAsync(request, CreateRequestContentStream(request), async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
					allowExpect100ToContinue = new TaskCompletionSource<bool>();
					Timer expect100Timer = new Timer(delegate(object s)
					{
						((TaskCompletionSource<bool>)s).TrySetResult(result: true);
					}, allowExpect100ToContinue, _pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
					sendRequestContentTask = SendRequestContentWithExpect100ContinueAsync(request, allowExpect100ToContinue.Task, CreateRequestContentStream(request), expect100Timer, async, cancellationToken);
				}
			}
			_allowedReadLineBytes = (int)Math.Min(2147483647L, (long)_pool.Settings._maxResponseHeadersLength * 1024L);
			ValueTask<int>? valueTask = ConsumeReadAheadTask();
			if (!valueTask.HasValue)
			{
				await InitialFillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				ValueTask<int> valueOrDefault = valueTask.GetValueOrDefault();
				int num;
				if (valueOrDefault.IsCompleted)
				{
					num = valueOrDefault.Result;
				}
				else
				{
					if (!async)
					{
						Trace("Pre-emptive read completed asynchronously for a synchronous request.", "SendAsyncCore");
					}
					num = await valueOrDefault.ConfigureAwait(continueOnCapturedContext: false);
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Received {num} bytes.", "SendAsyncCore");
				}
				_readOffset = 0;
				_readLength = num;
			}
			if (_readLength == 0)
			{
				if (!_startedSendingRequestBody)
				{
					_canRetry = true;
				}
				throw new IOException(System.SR.net_http_invalid_response_premature_eof);
			}
			HttpResponseMessage response = new HttpResponseMessage
			{
				RequestMessage = request,
				Content = new HttpConnectionResponseContent()
			};
			ParseStatusLine((await ReadNextResponseHeaderLineAsync(async).ConfigureAwait(continueOnCapturedContext: false)).Span, response);
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.ResponseHeadersStart();
			}
			while ((uint)(response.StatusCode - 100) <= 99u)
			{
				if (allowExpect100ToContinue != null && response.StatusCode == HttpStatusCode.Continue)
				{
					allowExpect100ToContinue.TrySetResult(result: true);
					allowExpect100ToContinue = null;
				}
				else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
				{
					break;
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Current {response.StatusCode} response is an interim response or not expected, need to read for a final response.", "SendAsyncCore");
				}
				while (!IsLineEmpty(await ReadNextResponseHeaderLineAsync(async).ConfigureAwait(continueOnCapturedContext: false)))
				{
				}
				ParseStatusLine((await ReadNextResponseHeaderLineAsync(async).ConfigureAwait(continueOnCapturedContext: false)).Span, response);
			}
			while (true)
			{
				ReadOnlyMemory<byte> line = await ReadNextResponseHeaderLineAsync(async, foldedHeadersAllowed: true).ConfigureAwait(continueOnCapturedContext: false);
				if (IsLineEmpty(line))
				{
					break;
				}
				ParseHeaderNameValue(this, line.Span, response, isFromTrailer: false);
			}
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.ResponseHeadersStop();
			}
			if (allowExpect100ToContinue != null)
			{
				if (response.StatusCode >= HttpStatusCode.MultipleChoices && request.Content != null && (!request.Content.Headers.ContentLength.HasValue || request.Content.Headers.ContentLength.GetValueOrDefault() > 1024) && !AuthenticationHelper.IsSessionAuthenticationChallenge(response))
				{
					allowExpect100ToContinue.TrySetResult(result: false);
					if (!allowExpect100ToContinue.Task.Result)
					{
						_connectionClose = true;
					}
				}
				else
				{
					allowExpect100ToContinue.TrySetResult(result: true);
				}
			}
			if (response.Headers.ConnectionClose.GetValueOrDefault())
			{
				_connectionClose = true;
			}
			if (sendRequestContentTask != null)
			{
				Task task = sendRequestContentTask;
				sendRequestContentTask = null;
				await task.ConfigureAwait(continueOnCapturedContext: false);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Request is fully sent.", "SendAsyncCore");
			}
			cancellationRegistration.Dispose();
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			Stream stream;
			if ((object)normalizedMethod == HttpMethod.Head || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotModified)
			{
				stream = EmptyReadStream.Instance;
				CompleteResponse();
			}
			else if ((object)normalizedMethod == HttpMethod.Connect && response.StatusCode == HttpStatusCode.OK)
			{
				stream = new RawConnectionStream(this);
				_connectionClose = true;
				_pool.InvalidateHttp11Connection(this);
				_detachedFromPool = true;
			}
			else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
			{
				stream = new RawConnectionStream(this);
				_connectionClose = true;
				_pool.InvalidateHttp11Connection(this);
				_detachedFromPool = true;
			}
			else if (!response.Content.Headers.ContentLength.HasValue)
			{
				stream = ((response.Headers.TransferEncodingChunked != true) ? ((HttpContentReadStream)new ConnectionCloseReadStream(this)) : ((HttpContentReadStream)new ChunkedEncodingReadStream(this, response)));
			}
			else
			{
				long valueOrDefault2 = response.Content.Headers.ContentLength.GetValueOrDefault();
				if (valueOrDefault2 <= 0)
				{
					stream = EmptyReadStream.Instance;
					CompleteResponse();
				}
				else
				{
					stream = new ContentLengthReadStream(this, (ulong)valueOrDefault2);
				}
			}
			((HttpConnectionResponseContent)response.Content).SetStream(stream);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Received response: {response}", "SendAsyncCore");
			}
			if (_pool.Settings._useCookies)
			{
				CookieHelper.ProcessReceivedCookies(response, _pool.Settings._cookieContainer);
			}
			result = response;
			return result;
		}
		catch (Exception ex)
		{
			Exception error = ex;
			cancellationRegistration.Dispose();
			allowExpect100ToContinue?.TrySetResult(result: false);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Error sending request: {error}", "SendAsyncCore");
			}
			if (sendRequestContentTask != null && !sendRequestContentTask.IsCompletedSuccessfully)
			{
				if (Volatile.Read(ref _disposed) == 1)
				{
					Exception mappedException;
					try
					{
						await sendRequestContentTask.ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (Exception exception) when (MapSendException(exception, cancellationToken, out mappedException))
					{
						throw mappedException;
					}
				}
				LogExceptions(sendRequestContentTask);
			}
			Dispose();
			if (MapSendException(error, cancellationToken, out var mappedException2))
			{
				throw mappedException2;
			}
			if (!(ex is Exception source))
			{
				throw ex;
			}
			ExceptionDispatchInfo.Capture(source).Throw();
		}
		return result;
	}

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		return SendAsyncCore(request, async, cancellationToken);
	}

	private bool MapSendException(Exception exception, CancellationToken cancellationToken, out Exception mappedException)
	{
		if (CancellationHelper.ShouldWrapInOperationCanceledException(exception, cancellationToken))
		{
			mappedException = CancellationHelper.CreateOperationCanceledException(exception, cancellationToken);
			return true;
		}
		if (exception is InvalidOperationException)
		{
			mappedException = new HttpRequestException(System.SR.net_http_client_execution_error, exception);
			return true;
		}
		if (exception is IOException inner)
		{
			mappedException = new HttpRequestException(System.SR.net_http_client_execution_error, inner, _canRetry ? RequestRetryType.RetryOnConnectionFailure : RequestRetryType.NoRetry);
			return true;
		}
		mappedException = exception;
		return false;
	}

	private HttpContentWriteStream CreateRequestContentStream(HttpRequestMessage request)
	{
		return (request.HasHeaders && request.Headers.TransferEncodingChunked == true) ? ((HttpContentWriteStream)new ChunkedEncodingWriteStream(this)) : ((HttpContentWriteStream)new ContentLengthWriteStream(this));
	}

	private CancellationTokenRegistration RegisterCancellation(CancellationToken cancellationToken)
	{
		return cancellationToken.Register(delegate(object s)
		{
			WeakReference<HttpConnection> weakReference = (WeakReference<HttpConnection>)s;
			if (weakReference.TryGetTarget(out var target))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					target.Trace("Cancellation requested. Disposing of the connection.", "RegisterCancellation");
				}
				target.Dispose();
			}
		}, _weakThisRef);
	}

	private static bool IsLineEmpty(ReadOnlyMemory<byte> line)
	{
		return line.Length == 0;
	}

	private async ValueTask SendRequestContentAsync(HttpRequestMessage request, HttpContentWriteStream stream, bool async, CancellationToken cancellationToken)
	{
		_startedSendingRequestBody = true;
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestContentStart();
		}
		if (async)
		{
			await request.Content.CopyToAsync(stream, _transportContext, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			request.Content.CopyTo(stream, _transportContext, cancellationToken);
		}
		await stream.FinishAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestContentStop(stream.BytesWritten);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Finished sending request content.", "SendRequestContentAsync");
		}
	}

	private async Task SendRequestContentWithExpect100ContinueAsync(HttpRequestMessage request, Task<bool> allowExpect100ToContinueTask, HttpContentWriteStream stream, Timer expect100Timer, bool async, CancellationToken cancellationToken)
	{
		bool flag = await allowExpect100ToContinueTask.ConfigureAwait(continueOnCapturedContext: false);
		expect100Timer.Dispose();
		if (flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Sending request content for Expect: 100-continue.", "SendRequestContentWithExpect100ContinueAsync");
			}
			try
			{
				await SendRequestContentAsync(request, stream, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Canceling request content for Expect: 100-continue.", "SendRequestContentWithExpect100ContinueAsync");
		}
	}

	private static void ParseStatusLine(ReadOnlySpan<byte> line, HttpResponseMessage response)
	{
		if (line.Length < 12 || line[8] != 32)
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
		}
		ulong num = BitConverter.ToUInt64(line);
		if (num == s_http11Bytes)
		{
			response.SetVersionWithoutValidation(HttpVersion.Version11);
		}
		else if (num == s_http10Bytes)
		{
			response.SetVersionWithoutValidation(HttpVersion.Version10);
		}
		else
		{
			byte b = line[7];
			if (!HttpConnectionBase.IsDigit(b) || !line.Slice(0, 7).SequenceEqual(s_http1DotBytes))
			{
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
			}
			response.SetVersionWithoutValidation(new Version(1, b - 48));
		}
		byte b2 = line[9];
		byte b3 = line[10];
		byte b4 = line[11];
		if (!HttpConnectionBase.IsDigit(b2) || !HttpConnectionBase.IsDigit(b3) || !HttpConnectionBase.IsDigit(b4))
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(line.Slice(9, 3))));
		}
		response.SetStatusCodeWithoutValidation((HttpStatusCode)(100 * (b2 - 48) + 10 * (b3 - 48) + (b4 - 48)));
		if (line.Length == 12)
		{
			response.SetReasonPhraseWithoutValidation(string.Empty);
			return;
		}
		if (line[12] == 32)
		{
			ReadOnlySpan<byte> readOnlySpan = line.Slice(13);
			string text = HttpStatusDescription.Get(response.StatusCode);
			if (text != null && EqualsOrdinal(text, readOnlySpan))
			{
				response.SetReasonPhraseWithoutValidation(text);
				return;
			}
			try
			{
				response.ReasonPhrase = HttpRuleParser.DefaultHttpEncoding.GetString(readOnlySpan);
				return;
			}
			catch (FormatException inner)
			{
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_status_reason, Encoding.ASCII.GetString(readOnlySpan.ToArray())), inner);
			}
		}
		throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
	}

	private static void ParseHeaderNameValue(HttpConnection connection, ReadOnlySpan<byte> line, HttpResponseMessage response, bool isFromTrailer)
	{
		int i = 0;
		while (line[i] != 58 && line[i] != 32)
		{
			i++;
			if (i == line.Length)
			{
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
			}
		}
		if (i == 0)
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_name, ""));
		}
		if (!HeaderDescriptor.TryGet(line.Slice(0, i), out var descriptor))
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(line.Slice(0, i))));
		}
		if (isFromTrailer && descriptor.KnownHeader != null && (descriptor.KnownHeader.HeaderType & HttpHeaderType.NonTrailing) == HttpHeaderType.NonTrailing)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Stripping forbidden " + descriptor.Name + " from trailer headers.", "ParseHeaderNameValue");
			}
			return;
		}
		while (line[i] == 32)
		{
			i++;
			if (i == line.Length)
			{
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
			}
		}
		if (line[i++] != 58)
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
		}
		for (; i < line.Length && (line[i] == 32 || line[i] == 9); i++)
		{
		}
		Encoding valueEncoding = connection._pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, response.RequestMessage);
		ReadOnlySpan<byte> readOnlySpan = line.Slice(i);
		if (isFromTrailer)
		{
			string headerValue = descriptor.GetHeaderValue(readOnlySpan, valueEncoding);
			response.TrailingHeaders.TryAddWithoutValidation(((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, headerValue);
		}
		else if ((descriptor.HeaderType & HttpHeaderType.Content) == HttpHeaderType.Content)
		{
			string headerValue2 = descriptor.GetHeaderValue(readOnlySpan, valueEncoding);
			response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue2);
		}
		else
		{
			string responseHeaderValueWithCaching = connection.GetResponseHeaderValueWithCaching(descriptor, readOnlySpan, valueEncoding);
			response.Headers.TryAddWithoutValidation(((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, responseHeaderValueWithCaching);
		}
	}

	private void WriteToBuffer(ReadOnlySpan<byte> source)
	{
		source.CopyTo(new Span<byte>(_writeBuffer, _writeOffset, source.Length));
		_writeOffset += source.Length;
	}

	private void WriteToBuffer(ReadOnlyMemory<byte> source)
	{
		source.Span.CopyTo(new Span<byte>(_writeBuffer, _writeOffset, source.Length));
		_writeOffset += source.Length;
	}

	private void Write(ReadOnlySpan<byte> source)
	{
		int num = _writeBuffer.Length - _writeOffset;
		if (source.Length <= num)
		{
			WriteToBuffer(source);
			return;
		}
		if (_writeOffset != 0)
		{
			WriteToBuffer(source.Slice(0, num));
			source = source.Slice(num);
			Flush();
		}
		if (source.Length >= _writeBuffer.Length)
		{
			WriteToStream(source);
		}
		else
		{
			WriteToBuffer(source);
		}
	}

	private async ValueTask WriteAsync(ReadOnlyMemory<byte> source, bool async)
	{
		int num = _writeBuffer.Length - _writeOffset;
		if (source.Length <= num)
		{
			WriteToBuffer(source);
			return;
		}
		if (_writeOffset != 0)
		{
			WriteToBuffer(source.Slice(0, num));
			source = source.Slice(num);
			await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (source.Length >= _writeBuffer.Length)
		{
			await WriteToStreamAsync(source, async).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			WriteToBuffer(source);
		}
	}

	private void WriteWithoutBuffering(ReadOnlySpan<byte> source)
	{
		if (_writeOffset != 0)
		{
			int num = _writeBuffer.Length - _writeOffset;
			if (source.Length <= num)
			{
				WriteToBuffer(source);
				Flush();
				return;
			}
			Flush();
		}
		WriteToStream(source);
	}

	private ValueTask WriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, bool async)
	{
		if (_writeOffset == 0)
		{
			return WriteToStreamAsync(source, async);
		}
		int num = _writeBuffer.Length - _writeOffset;
		if (source.Length <= num)
		{
			WriteToBuffer(source);
			return FlushAsync(async);
		}
		return FlushThenWriteWithoutBufferingAsync(source, async);
	}

	private async ValueTask FlushThenWriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, bool async)
	{
		await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		await WriteToStreamAsync(source, async).ConfigureAwait(continueOnCapturedContext: false);
	}

	private Task WriteByteAsync(byte b, bool async)
	{
		if (_writeOffset < _writeBuffer.Length)
		{
			_writeBuffer[_writeOffset++] = b;
			return Task.CompletedTask;
		}
		return WriteByteSlowAsync(b, async);
	}

	private async Task WriteByteSlowAsync(byte b, bool async)
	{
		await WriteToStreamAsync(_writeBuffer, async).ConfigureAwait(continueOnCapturedContext: false);
		_writeBuffer[0] = b;
		_writeOffset = 1;
	}

	private Task WriteTwoBytesAsync(byte b1, byte b2, bool async)
	{
		if (_writeOffset <= _writeBuffer.Length - 2)
		{
			byte[] writeBuffer = _writeBuffer;
			writeBuffer[_writeOffset++] = b1;
			writeBuffer[_writeOffset++] = b2;
			return Task.CompletedTask;
		}
		return WriteTwoBytesSlowAsync(b1, b2, async);
	}

	private async Task WriteTwoBytesSlowAsync(byte b1, byte b2, bool async)
	{
		await WriteByteAsync(b1, async).ConfigureAwait(continueOnCapturedContext: false);
		await WriteByteAsync(b2, async).ConfigureAwait(continueOnCapturedContext: false);
	}

	private Task WriteBytesAsync(byte[] bytes, bool async)
	{
		if (_writeOffset <= _writeBuffer.Length - bytes.Length)
		{
			Buffer.BlockCopy(bytes, 0, _writeBuffer, _writeOffset, bytes.Length);
			_writeOffset += bytes.Length;
			return Task.CompletedTask;
		}
		return WriteBytesSlowAsync(bytes, bytes.Length, async);
	}

	private async Task WriteBytesSlowAsync(byte[] bytes, int length, bool async)
	{
		int offset = 0;
		while (true)
		{
			int val = length - offset;
			int num = Math.Min(val, _writeBuffer.Length - _writeOffset);
			Buffer.BlockCopy(bytes, offset, _writeBuffer, _writeOffset, num);
			_writeOffset += num;
			offset += num;
			if (offset != length)
			{
				if (_writeOffset == _writeBuffer.Length)
				{
					await WriteToStreamAsync(_writeBuffer, async).ConfigureAwait(continueOnCapturedContext: false);
					_writeOffset = 0;
				}
				continue;
			}
			break;
		}
	}

	private Task WriteStringAsync(string s, bool async)
	{
		int writeOffset = _writeOffset;
		if (s.Length <= _writeBuffer.Length - writeOffset)
		{
			byte[] writeBuffer = _writeBuffer;
			foreach (char c in s)
			{
				if ((c & 0xFF80u) != 0)
				{
					throw new HttpRequestException(System.SR.net_http_request_invalid_char_encoding);
				}
				writeBuffer[writeOffset++] = (byte)c;
			}
			_writeOffset = writeOffset;
			return Task.CompletedTask;
		}
		return WriteStringAsyncSlow(s, async);
	}

	private Task WriteStringAsync(string s, bool async, Encoding encoding)
	{
		if (encoding == null)
		{
			return WriteStringAsync(s, async);
		}
		if (encoding.GetMaxByteCount(s.Length) <= _writeBuffer.Length - _writeOffset)
		{
			_writeOffset += encoding.GetBytes(s, _writeBuffer.AsSpan(_writeOffset));
			return Task.CompletedTask;
		}
		return WriteStringWithEncodingAsyncSlow(s, async, encoding);
	}

	private async Task WriteStringWithEncodingAsyncSlow(string s, bool async, Encoding encoding)
	{
		int minimumLength = ((s.Length <= 512) ? encoding.GetMaxByteCount(s.Length) : encoding.GetByteCount(s));
		byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(minimumLength);
		try
		{
			int bytes = encoding.GetBytes(s, rentedBuffer);
			await WriteBytesSlowAsync(rentedBuffer, bytes, async).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rentedBuffer);
		}
	}

	private Task WriteAsciiStringAsync(string s, bool async)
	{
		int writeOffset = _writeOffset;
		if (s.Length <= _writeBuffer.Length - writeOffset)
		{
			byte[] writeBuffer = _writeBuffer;
			foreach (char c in s)
			{
				writeBuffer[writeOffset++] = (byte)c;
			}
			_writeOffset = writeOffset;
			return Task.CompletedTask;
		}
		return WriteStringAsyncSlow(s, async);
	}

	private async Task WriteStringAsyncSlow(string s, bool async)
	{
		foreach (char c in s)
		{
			if ((c & 0xFF80u) != 0)
			{
				throw new HttpRequestException(System.SR.net_http_request_invalid_char_encoding);
			}
			await WriteByteAsync((byte)c, async).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void Flush()
	{
		if (_writeOffset > 0)
		{
			WriteToStream(new ReadOnlySpan<byte>(_writeBuffer, 0, _writeOffset));
			_writeOffset = 0;
		}
	}

	private ValueTask FlushAsync(bool async)
	{
		if (_writeOffset > 0)
		{
			ValueTask result = WriteToStreamAsync(new ReadOnlyMemory<byte>(_writeBuffer, 0, _writeOffset), async);
			_writeOffset = 0;
			return result;
		}
		return default(ValueTask);
	}

	private void WriteToStream(ReadOnlySpan<byte> source)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Writing {source.Length} bytes.", "WriteToStream");
		}
		_stream.Write(source);
	}

	private ValueTask WriteToStreamAsync(ReadOnlyMemory<byte> source, bool async)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Writing {source.Length} bytes.", "WriteToStreamAsync");
		}
		if (async)
		{
			return _stream.WriteAsync(source);
		}
		_stream.Write(source.Span);
		return default(ValueTask);
	}

	private bool TryReadNextLine(out ReadOnlySpan<byte> line)
	{
		ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(_readBuffer, _readOffset, _readLength - _readOffset);
		int num = span.IndexOf<byte>(10);
		if (num < 0)
		{
			if (_allowedReadLineBytes < span.Length)
			{
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_response_headers_exceeded_length, (long)_pool.Settings._maxResponseHeadersLength * 1024L));
			}
			line = default(ReadOnlySpan<byte>);
			return false;
		}
		int num2 = num + 1;
		_readOffset += num2;
		_allowedReadLineBytes -= num2;
		ThrowIfExceededAllowedReadLineBytes();
		line = span.Slice(0, (num > 0 && span[num - 1] == 13) ? (num - 1) : num);
		return true;
	}

	private async ValueTask<ReadOnlyMemory<byte>> ReadNextResponseHeaderLineAsync(bool async, bool foldedHeadersAllowed = false)
	{
		int previouslyScannedBytes = 0;
		int num;
		int num2;
		int readOffset;
		int num3;
		while (true)
		{
			num = _readOffset + previouslyScannedBytes;
			num2 = Array.IndexOf(_readBuffer, (byte)10, num, _readLength - num);
			if (num2 >= 0)
			{
				readOffset = _readOffset;
				num3 = num2 - readOffset;
				if (num2 > 0 && _readBuffer[num2 - 1] == 13)
				{
					num3--;
				}
				if (!foldedHeadersAllowed || num3 <= 0)
				{
					break;
				}
				if (num2 + 1 == _readLength)
				{
					int num4 = ((_readBuffer[num2 - 1] == 13) ? (num2 - 2) : (num2 - 1));
					previouslyScannedBytes = num4 - _readOffset;
					_allowedReadLineBytes -= num4 - num;
					ThrowIfExceededAllowedReadLineBytes();
					await FillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				char c = (char)_readBuffer[num2 + 1];
				if (c != ' ' && c != '\t')
				{
					break;
				}
				if (Array.IndexOf(_readBuffer, (byte)58, _readOffset, num2 - _readOffset) == -1)
				{
					throw new HttpRequestException(System.SR.net_http_invalid_response_header_folder);
				}
				_readBuffer[num2] = 32;
				if (_readBuffer[num2 - 1] == 13)
				{
					_readBuffer[num2 - 1] = 32;
				}
				previouslyScannedBytes = num2 + 1 - _readOffset;
				_allowedReadLineBytes -= num2 + 1 - num;
				ThrowIfExceededAllowedReadLineBytes();
			}
			else
			{
				previouslyScannedBytes = _readLength - _readOffset;
				_allowedReadLineBytes -= _readLength - num;
				ThrowIfExceededAllowedReadLineBytes();
				await FillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		_allowedReadLineBytes -= num2 + 1 - num;
		ThrowIfExceededAllowedReadLineBytes();
		_readOffset = num2 + 1;
		return new ReadOnlyMemory<byte>(_readBuffer, readOffset, num3);
	}

	private void ThrowIfExceededAllowedReadLineBytes()
	{
		if (_allowedReadLineBytes < 0)
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_response_headers_exceeded_length, (long)_pool.Settings._maxResponseHeadersLength * 1024L));
		}
	}

	private void Fill()
	{
		FillAsync(async: false).GetAwaiter().GetResult();
	}

	private async ValueTask InitialFillAsync(bool async)
	{
		_readOffset = 0;
		int readLength = ((!async) ? _stream.Read(_readBuffer) : (await _stream.ReadAsync(_readBuffer).ConfigureAwait(continueOnCapturedContext: false)));
		_readLength = readLength;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {_readLength} bytes.", "InitialFillAsync");
		}
	}

	private async ValueTask FillAsync(bool async)
	{
		int num = _readLength - _readOffset;
		if (num == 0)
		{
			_readOffset = (_readLength = 0);
		}
		else if (_readOffset > 0)
		{
			Buffer.BlockCopy(_readBuffer, _readOffset, _readBuffer, 0, num);
			_readOffset = 0;
			_readLength = num;
		}
		else if (num == _readBuffer.Length)
		{
			byte[] array = new byte[_readBuffer.Length * 2];
			Buffer.BlockCopy(_readBuffer, 0, array, 0, num);
			_readBuffer = array;
			_readOffset = 0;
			_readLength = num;
		}
		int num2 = ((!async) ? _stream.Read(_readBuffer, _readLength, _readBuffer.Length - _readLength) : (await _stream.ReadAsync(new Memory<byte>(_readBuffer, _readLength, _readBuffer.Length - _readLength)).ConfigureAwait(continueOnCapturedContext: false)));
		int num3 = num2;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num3} bytes.", "FillAsync");
		}
		if (num3 == 0)
		{
			throw new IOException(System.SR.net_http_invalid_response_premature_eof);
		}
		_readLength += num3;
	}

	private void ReadFromBuffer(Span<byte> buffer)
	{
		new Span<byte>(_readBuffer, _readOffset, buffer.Length).CopyTo(buffer);
		_readOffset += buffer.Length;
	}

	private int Read(Span<byte> destination)
	{
		int num = _readLength - _readOffset;
		if (num > 0)
		{
			if (destination.Length <= num)
			{
				ReadFromBuffer(destination);
				return destination.Length;
			}
			ReadFromBuffer(destination.Slice(0, num));
			return num;
		}
		int num2 = _stream.Read(destination);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num2} bytes.", "Read");
		}
		return num2;
	}

	private async ValueTask<int> ReadAsync(Memory<byte> destination)
	{
		int num = _readLength - _readOffset;
		if (num > 0)
		{
			if (destination.Length <= num)
			{
				ReadFromBuffer(destination.Span);
				return destination.Length;
			}
			ReadFromBuffer(destination.Span.Slice(0, num));
			return num;
		}
		int num2 = await _stream.ReadAsync(destination).ConfigureAwait(continueOnCapturedContext: false);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num2} bytes.", "ReadAsync");
		}
		return num2;
	}

	private int ReadBuffered(Span<byte> destination)
	{
		int num = _readLength - _readOffset;
		if (num > 0)
		{
			if (destination.Length <= num)
			{
				ReadFromBuffer(destination);
				return destination.Length;
			}
			ReadFromBuffer(destination.Slice(0, num));
			return num;
		}
		_readOffset = (_readLength = 0);
		int num2 = _stream.Read(_readBuffer, 0, _readBuffer.Length);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num2} bytes.", "ReadBuffered");
		}
		_readLength = num2;
		int num3 = Math.Min(num2, destination.Length);
		_readBuffer.AsSpan(0, num3).CopyTo(destination);
		_readOffset = num3;
		return num3;
	}

	private ValueTask<int> ReadBufferedAsync(Memory<byte> destination)
	{
		if (destination.Length < _readBuffer.Length)
		{
			return ReadBufferedAsyncCore(destination);
		}
		return ReadAsync(destination);
	}

	private async ValueTask<int> ReadBufferedAsyncCore(Memory<byte> destination)
	{
		int num = _readLength - _readOffset;
		if (num > 0)
		{
			if (destination.Length <= num)
			{
				ReadFromBuffer(destination.Span);
				return destination.Length;
			}
			ReadFromBuffer(destination.Span.Slice(0, num));
			return num;
		}
		_readOffset = (_readLength = 0);
		int num2 = await _stream.ReadAsync(_readBuffer.AsMemory()).ConfigureAwait(continueOnCapturedContext: false);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num2} bytes.", "ReadBufferedAsyncCore");
		}
		_readLength = num2;
		int num3 = Math.Min(num2, destination.Length);
		_readBuffer.AsSpan(0, num3).CopyTo(destination.Span);
		_readOffset = num3;
		return num3;
	}

	private ValueTask CopyFromBufferAsync(Stream destination, bool async, int count, CancellationToken cancellationToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Copying {count} bytes to stream.", "CopyFromBufferAsync");
		}
		int readOffset = _readOffset;
		_readOffset += count;
		if (async)
		{
			return destination.WriteAsync(new ReadOnlyMemory<byte>(_readBuffer, readOffset, count), cancellationToken);
		}
		destination.Write(_readBuffer, readOffset, count);
		return default(ValueTask);
	}

	private Task CopyToUntilEofAsync(Stream destination, bool async, int bufferSize, CancellationToken cancellationToken)
	{
		int num = _readLength - _readOffset;
		if (num > 0)
		{
			return CopyToUntilEofWithExistingBufferedDataAsync(destination, async, bufferSize, cancellationToken);
		}
		if (async)
		{
			return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
		}
		_stream.CopyTo(destination, bufferSize);
		return Task.CompletedTask;
	}

	private async Task CopyToUntilEofWithExistingBufferedDataAsync(Stream destination, bool async, int bufferSize, CancellationToken cancellationToken)
	{
		int count = _readLength - _readOffset;
		await CopyFromBufferAsync(destination, async, count, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_readLength = (_readOffset = 0);
		if (async)
		{
			await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			_stream.CopyTo(destination, bufferSize);
		}
	}

	private async Task CopyToContentLengthAsync(Stream destination, bool async, ulong length, int bufferSize, CancellationToken cancellationToken)
	{
		int remaining = _readLength - _readOffset;
		if (remaining > 0)
		{
			if ((ulong)remaining > length)
			{
				remaining = (int)length;
			}
			await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			length -= (ulong)remaining;
			if (length == 0L)
			{
				return;
			}
		}
		byte[] origReadBuffer = null;
		try
		{
			while (true)
			{
				await FillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
				remaining = (((ulong)_readLength < length) ? _readLength : ((int)length));
				await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				length -= (ulong)remaining;
				if (length == 0L)
				{
					break;
				}
				if (origReadBuffer != null)
				{
					continue;
				}
				byte[] readBuffer = _readBuffer;
				if (remaining == readBuffer.Length)
				{
					int num = (int)Math.Min((ulong)bufferSize, length);
					if (num > readBuffer.Length)
					{
						origReadBuffer = readBuffer;
						_readBuffer = ArrayPool<byte>.Shared.Rent(num);
					}
				}
			}
		}
		finally
		{
			if (origReadBuffer != null)
			{
				byte[] readBuffer2 = _readBuffer;
				_readBuffer = origReadBuffer;
				ArrayPool<byte>.Shared.Return(readBuffer2);
				_readLength = ((_readOffset < _readLength) ? 1 : 0);
				_readOffset = 0;
			}
		}
	}

	internal void Acquire()
	{
		_inUse = true;
	}

	internal void Release()
	{
		_inUse = false;
		if (_currentRequest == null)
		{
			ReturnConnectionToPool();
		}
	}

	internal void DetachFromPool()
	{
		_detachedFromPool = true;
	}

	private void CompleteResponse()
	{
		_currentRequest = null;
		if (_readLength != _readOffset)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Unexpected data on connection after response read.", "CompleteResponse");
			}
			_readOffset = (_readLength = 0);
			_connectionClose = true;
		}
		if (!_inUse)
		{
			ReturnConnectionToPool();
		}
	}

	public async ValueTask DrainResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (_connectionClose)
		{
			throw new HttpRequestException(System.SR.net_http_authconnectionfailure);
		}
		Stream stream = response.Content.ReadAsStream(cancellationToken);
		if (stream is HttpContentReadStream { NeedsDrain: not false } httpContentReadStream && (!(await httpContentReadStream.DrainAsync(_pool.Settings._maxResponseDrainSize).ConfigureAwait(continueOnCapturedContext: false)) || _connectionClose))
		{
			throw new HttpRequestException(System.SR.net_http_authconnectionfailure);
		}
		response.Dispose();
	}

	private void ReturnConnectionToPool()
	{
		if (_connectionClose)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Connection will not be reused.", "ReturnConnectionToPool");
			}
			Dispose();
		}
		else
		{
			_idleSinceTickCount = Environment.TickCount64;
			_pool.ReturnHttp11Connection(this);
		}
	}

	private static bool EqualsOrdinal(string left, ReadOnlySpan<byte> right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}

	public sealed override string ToString()
	{
		return $"{"HttpConnection"}({_pool})";
	}

	public sealed override void Trace(string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(_pool?.GetHashCode() ?? 0, GetHashCode(), _currentRequest?.GetHashCode() ?? 0, memberName, message);
	}
}
