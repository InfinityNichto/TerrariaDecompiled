using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;

namespace System.Net;

internal sealed class StreamFramer
{
	private readonly FrameHeader _writeHeader = new FrameHeader();

	private readonly FrameHeader _curReadHeader = new FrameHeader();

	private readonly byte[] _readHeaderBuffer = new byte[5];

	private readonly byte[] _writeHeaderBuffer = new byte[5];

	private bool _eof;

	public FrameHeader ReadHeader => _curReadHeader;

	public FrameHeader WriteHeader => _writeHeader;

	public async ValueTask<byte[]> ReadMessageAsync<TAdapter>(TAdapter adapter) where TAdapter : IReadWriteAdapter
	{
		if (_eof)
		{
			return null;
		}
		byte[] buffer = _readHeaderBuffer;
		int num;
		for (int offset = 0; offset < buffer.Length; offset += num)
		{
			num = await adapter.ReadAsync(buffer.AsMemory(offset)).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				if (offset == 0)
				{
					_eof = true;
					return null;
				}
				throw new IOException(System.SR.Format(System.SR.net_io_readfailure, System.SR.net_io_connectionclosed));
			}
		}
		_curReadHeader.CopyFrom(buffer, 0);
		if (_curReadHeader.PayloadSize > 65535)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_frame_size, 65535, _curReadHeader.PayloadSize.ToString(NumberFormatInfo.InvariantInfo)));
		}
		buffer = new byte[_curReadHeader.PayloadSize];
		for (int offset = 0; offset < buffer.Length; offset += num)
		{
			num = await adapter.ReadAsync(buffer.AsMemory(offset)).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				throw new IOException(System.SR.Format(System.SR.net_io_readfailure, System.SR.net_io_connectionclosed));
			}
		}
		return buffer;
	}

	public async Task WriteMessageAsync<TAdapter>(TAdapter adapter, byte[] message) where TAdapter : IReadWriteAdapter
	{
		if (message == null)
		{
			throw new ArgumentNullException("message");
		}
		_writeHeader.PayloadSize = message.Length;
		_writeHeader.CopyTo(_writeHeaderBuffer, 0);
		await adapter.WriteAsync(_writeHeaderBuffer, 0, _writeHeaderBuffer.Length).ConfigureAwait(continueOnCapturedContext: false);
		if (message.Length != 0)
		{
			await adapter.WriteAsync(message, 0, message.Length).ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
