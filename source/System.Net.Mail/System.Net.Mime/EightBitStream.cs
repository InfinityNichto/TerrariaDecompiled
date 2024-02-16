using System.IO;
using System.Text;

namespace System.Net.Mime;

internal sealed class EightBitStream : DelegatedStream, IEncodableStream
{
	private WriteStateInfoBase _writeState;

	private readonly bool _shouldEncodeLeadingDots;

	private WriteStateInfoBase WriteState => _writeState ?? (_writeState = new WriteStateInfoBase());

	internal EightBitStream(Stream stream)
		: base(stream)
	{
	}

	internal EightBitStream(Stream stream, bool shouldEncodeLeadingDots)
		: this(stream)
	{
		_shouldEncodeLeadingDots = shouldEncodeLeadingDots;
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (_shouldEncodeLeadingDots)
		{
			EncodeLines(buffer, offset, count);
			return base.BeginWrite(WriteState.Buffer, 0, WriteState.Length, callback, state);
		}
		return base.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		base.EndWrite(asyncResult);
		WriteState.BufferFlushed();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (_shouldEncodeLeadingDots)
		{
			EncodeLines(buffer, offset, count);
			base.Write(WriteState.Buffer, 0, WriteState.Length);
			WriteState.BufferFlushed();
		}
		else
		{
			base.Write(buffer, offset, count);
		}
	}

	private void EncodeLines(byte[] buffer, int offset, int count)
	{
		for (int i = offset; i < offset + count && i < buffer.Length; i++)
		{
			if (buffer[i] == 13 && i + 1 < offset + count && buffer[i + 1] == 10)
			{
				WriteState.AppendCRLF(includeSpace: false);
				i++;
			}
			else if (WriteState.CurrentLineLength == 0 && buffer[i] == 46)
			{
				WriteState.Append(46);
				WriteState.Append(buffer[i]);
			}
			else
			{
				WriteState.Append(buffer[i]);
			}
		}
	}

	public int DecodeBytes(byte[] buffer, int offset, int count)
	{
		throw new NotImplementedException();
	}

	public int EncodeString(string value, Encoding encoding)
	{
		throw new NotImplementedException();
	}

	public string GetEncodedString()
	{
		throw new NotImplementedException();
	}
}
