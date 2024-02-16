using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class StringReader : TextReader
{
	private string _s;

	private int _pos;

	private int _length;

	public StringReader(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		_s = s;
		_length = s.Length;
	}

	public override void Close()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		_s = null;
		_pos = 0;
		_length = 0;
		base.Dispose(disposing);
	}

	public override int Peek()
	{
		if (_s == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
		}
		if (_pos == _length)
		{
			return -1;
		}
		return _s[_pos];
	}

	public override int Read()
	{
		if (_s == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
		}
		if (_pos == _length)
		{
			return -1;
		}
		return _s[_pos++];
	}

	public override int Read(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (_s == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
		}
		int num = _length - _pos;
		if (num > 0)
		{
			if (num > count)
			{
				num = count;
			}
			_s.CopyTo(_pos, buffer, index, num);
			_pos += num;
		}
		return num;
	}

	public override int Read(Span<char> buffer)
	{
		if (GetType() != typeof(StringReader))
		{
			return base.Read(buffer);
		}
		if (_s == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
		}
		int num = _length - _pos;
		if (num > 0)
		{
			if (num > buffer.Length)
			{
				num = buffer.Length;
			}
			_s.AsSpan(_pos, num).CopyTo(buffer);
			_pos += num;
		}
		return num;
	}

	public override int ReadBlock(Span<char> buffer)
	{
		return Read(buffer);
	}

	public override string ReadToEnd()
	{
		if (_s == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
		}
		string result = ((_pos != 0) ? _s.Substring(_pos, _length - _pos) : _s);
		_pos = _length;
		return result;
	}

	public override string? ReadLine()
	{
		if (_s == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
		}
		int i;
		for (i = _pos; i < _length; i++)
		{
			char c = _s[i];
			if (c == '\r' || c == '\n')
			{
				string result = _s.Substring(_pos, i - _pos);
				_pos = i + 1;
				if (c == '\r' && _pos < _length && _s[_pos] == '\n')
				{
					_pos++;
				}
				return result;
			}
		}
		if (i > _pos)
		{
			string result2 = _s.Substring(_pos, i - _pos);
			_pos = i;
			return result2;
		}
		return null;
	}

	public override Task<string?> ReadLineAsync()
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
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(ReadBlock(buffer, index, count));
	}

	public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<int>(ReadBlock(buffer.Span));
		}
		return ValueTask.FromCanceled<int>(cancellationToken);
	}

	public override Task<int> ReadAsync(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(Read(buffer, index, count));
	}

	public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<int>(Read(buffer.Span));
		}
		return ValueTask.FromCanceled<int>(cancellationToken);
	}
}
