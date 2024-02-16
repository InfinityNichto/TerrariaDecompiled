using System.Threading.Tasks;

namespace System.IO;

internal sealed class SyncTextReader : TextReader
{
	internal readonly TextReader _in;

	public static SyncTextReader GetSynchronizedTextReader(TextReader reader)
	{
		return (reader as SyncTextReader) ?? new SyncTextReader(reader);
	}

	internal SyncTextReader(TextReader t)
	{
		_in = t;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			lock (this)
			{
				_in.Dispose();
			}
		}
	}

	public override int Peek()
	{
		lock (this)
		{
			return _in.Peek();
		}
	}

	public override int Read()
	{
		lock (this)
		{
			return _in.Read();
		}
	}

	public override int Read(char[] buffer, int index, int count)
	{
		lock (this)
		{
			return _in.Read(buffer, index, count);
		}
	}

	public override int ReadBlock(char[] buffer, int index, int count)
	{
		lock (this)
		{
			return _in.ReadBlock(buffer, index, count);
		}
	}

	public override string ReadLine()
	{
		lock (this)
		{
			return _in.ReadLine();
		}
	}

	public override string ReadToEnd()
	{
		lock (this)
		{
			return _in.ReadToEnd();
		}
	}

	public override Task<string> ReadLineAsync()
	{
		return Task.FromResult(ReadLine());
	}

	public override Task<string> ReadToEndAsync()
	{
		return Task.FromResult(ReadToEnd());
	}

	public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", System.SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(ReadBlock(buffer, index, count));
	}

	public override Task<int> ReadAsync(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", System.SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(Read(buffer, index, count));
	}
}
