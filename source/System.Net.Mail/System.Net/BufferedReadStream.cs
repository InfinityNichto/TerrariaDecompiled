using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal sealed class BufferedReadStream : DelegatedStream
{
	private sealed class ReadAsyncResult : System.Net.LazyAsyncResult
	{
		private readonly BufferedReadStream _parent;

		private int _read;

		private static readonly AsyncCallback s_onRead = OnRead;

		internal ReadAsyncResult(BufferedReadStream parent, AsyncCallback callback, object state)
			: base(null, state, callback)
		{
			_parent = parent;
		}

		internal void Read(byte[] buffer, int offset, int count)
		{
			if (_parent._storedOffset < _parent._storedLength)
			{
				_read = Math.Min(count, _parent._storedLength - _parent._storedOffset);
				Buffer.BlockCopy(_parent._storedBuffer, _parent._storedOffset, buffer, offset, _read);
				_parent._storedOffset += _read;
				if (_read == count || !_parent._readMore)
				{
					InvokeCallback();
					return;
				}
				count -= _read;
				offset += _read;
			}
			IAsyncResult asyncResult = _parent.BaseStream.BeginRead(buffer, offset, count, s_onRead, this);
			if (asyncResult.CompletedSynchronously)
			{
				_read += _parent.BaseStream.EndRead(asyncResult);
				InvokeCallback();
			}
		}

		internal static int End(IAsyncResult result)
		{
			ReadAsyncResult readAsyncResult = (ReadAsyncResult)result;
			readAsyncResult.InternalWaitForCompletion();
			return readAsyncResult._read;
		}

		private static void OnRead(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ReadAsyncResult readAsyncResult = (ReadAsyncResult)result.AsyncState;
			try
			{
				readAsyncResult._read += readAsyncResult._parent.BaseStream.EndRead(result);
				readAsyncResult.InvokeCallback();
			}
			catch (Exception result2)
			{
				if (readAsyncResult.IsCompleted)
				{
					throw;
				}
				readAsyncResult.InvokeCallback(result2);
			}
		}
	}

	private byte[] _storedBuffer;

	private int _storedLength;

	private int _storedOffset;

	private readonly bool _readMore;

	public override bool CanWrite => false;

	public override bool CanSeek => false;

	internal BufferedReadStream(Stream stream)
		: this(stream, readMore: false)
	{
	}

	internal BufferedReadStream(Stream stream, bool readMore)
		: base(stream)
	{
		_readMore = readMore;
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		ReadAsyncResult readAsyncResult = new ReadAsyncResult(this, callback, state);
		readAsyncResult.Read(buffer, offset, count);
		return readAsyncResult;
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return ReadAsyncResult.End(asyncResult);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int num = 0;
		if (_storedOffset < _storedLength)
		{
			num = Math.Min(count, _storedLength - _storedOffset);
			Buffer.BlockCopy(_storedBuffer, _storedOffset, buffer, offset, num);
			_storedOffset += num;
			if (num == count || !_readMore)
			{
				return num;
			}
			offset += num;
			count -= num;
		}
		return num + base.Read(buffer, offset, count);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		int num = 0;
		if (_storedOffset >= _storedLength)
		{
			return base.ReadAsync(buffer, offset, count, cancellationToken);
		}
		num = Math.Min(count, _storedLength - _storedOffset);
		Buffer.BlockCopy(_storedBuffer, _storedOffset, buffer, offset, num);
		_storedOffset += num;
		if (num == count || !_readMore)
		{
			return Task.FromResult(num);
		}
		offset += num;
		count -= num;
		return ReadMoreAsync(num, buffer, offset, count, cancellationToken);
	}

	private async Task<int> ReadMoreAsync(int bytesAlreadyRead, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return bytesAlreadyRead + await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override int ReadByte()
	{
		if (_storedOffset < _storedLength)
		{
			return _storedBuffer[_storedOffset++];
		}
		return base.ReadByte();
	}

	internal void Push(byte[] buffer, int offset, int count)
	{
		if (count == 0)
		{
			return;
		}
		if (_storedOffset == _storedLength)
		{
			if (_storedBuffer == null || _storedBuffer.Length < count)
			{
				_storedBuffer = new byte[count];
			}
			_storedOffset = 0;
			_storedLength = count;
		}
		else if (count <= _storedOffset)
		{
			_storedOffset -= count;
		}
		else if (count <= _storedBuffer.Length - _storedLength + _storedOffset)
		{
			Buffer.BlockCopy(_storedBuffer, _storedOffset, _storedBuffer, count, _storedLength - _storedOffset);
			_storedLength += count - _storedOffset;
			_storedOffset = 0;
		}
		else
		{
			byte[] array = new byte[count + _storedLength - _storedOffset];
			Buffer.BlockCopy(_storedBuffer, _storedOffset, array, count, _storedLength - _storedOffset);
			_storedLength += count - _storedOffset;
			_storedOffset = 0;
			_storedBuffer = array;
		}
		Buffer.BlockCopy(buffer, offset, _storedBuffer, _storedOffset, count);
	}
}
