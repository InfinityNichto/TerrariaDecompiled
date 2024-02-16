using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class StringWriter : TextWriter
{
	private static volatile UnicodeEncoding s_encoding;

	private readonly StringBuilder _sb;

	private bool _isOpen;

	public override Encoding Encoding
	{
		get
		{
			if (s_encoding == null)
			{
				s_encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
			}
			return s_encoding;
		}
	}

	public StringWriter()
		: this(new StringBuilder(), CultureInfo.CurrentCulture)
	{
	}

	public StringWriter(IFormatProvider? formatProvider)
		: this(new StringBuilder(), formatProvider)
	{
	}

	public StringWriter(StringBuilder sb)
		: this(sb, CultureInfo.CurrentCulture)
	{
	}

	public StringWriter(StringBuilder sb, IFormatProvider? formatProvider)
		: base(formatProvider)
	{
		if (sb == null)
		{
			throw new ArgumentNullException("sb", SR.ArgumentNull_Buffer);
		}
		_sb = sb;
		_isOpen = true;
	}

	public override void Close()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		_isOpen = false;
		base.Dispose(disposing);
	}

	public virtual StringBuilder GetStringBuilder()
	{
		return _sb;
	}

	public override void Write(char value)
	{
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(value);
	}

	public override void Write(char[] buffer, int index, int count)
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
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(buffer, index, count);
	}

	public override void Write(ReadOnlySpan<char> buffer)
	{
		if (GetType() != typeof(StringWriter))
		{
			base.Write(buffer);
			return;
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(buffer);
	}

	public override void Write(string? value)
	{
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		if (value != null)
		{
			_sb.Append(value);
		}
	}

	public override void Write(StringBuilder? value)
	{
		if (GetType() != typeof(StringWriter))
		{
			base.Write(value);
			return;
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(value);
	}

	public override void WriteLine(ReadOnlySpan<char> buffer)
	{
		if (GetType() != typeof(StringWriter))
		{
			base.WriteLine(buffer);
			return;
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(buffer);
		WriteLine();
	}

	public override void WriteLine(StringBuilder? value)
	{
		if (GetType() != typeof(StringWriter))
		{
			base.WriteLine(value);
			return;
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(value);
		WriteLine();
	}

	public override Task WriteAsync(char value)
	{
		Write(value);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(string? value)
	{
		Write(value);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(char[] buffer, int index, int count)
	{
		Write(buffer, index, count);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		Write(buffer.Span);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StringWriter))
		{
			return base.WriteAsync(value, cancellationToken);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(char value)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(string? value)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StringWriter))
		{
			return base.WriteLineAsync(value, cancellationToken);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
		}
		_sb.Append(value);
		WriteLine();
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(char[] buffer, int index, int count)
	{
		WriteLine(buffer, index, count);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		WriteLine(buffer.Span);
		return Task.CompletedTask;
	}

	public override Task FlushAsync()
	{
		return Task.CompletedTask;
	}

	public override string ToString()
	{
		return _sb.ToString();
	}
}
