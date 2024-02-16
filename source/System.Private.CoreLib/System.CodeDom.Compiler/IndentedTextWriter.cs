using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.CodeDom.Compiler;

public class IndentedTextWriter : TextWriter
{
	private readonly TextWriter _writer;

	private readonly string _tabString;

	private int _indentLevel;

	private bool _tabsPending;

	public const string DefaultTabString = "    ";

	public override Encoding Encoding => _writer.Encoding;

	public override string NewLine
	{
		get
		{
			return _writer.NewLine;
		}
		[param: AllowNull]
		set
		{
			_writer.NewLine = value;
		}
	}

	public int Indent
	{
		get
		{
			return _indentLevel;
		}
		set
		{
			_indentLevel = Math.Max(value, 0);
		}
	}

	public TextWriter InnerWriter => _writer;

	public IndentedTextWriter(TextWriter writer)
		: this(writer, "    ")
	{
	}

	public IndentedTextWriter(TextWriter writer, string tabString)
		: base(CultureInfo.InvariantCulture)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		_writer = writer;
		_tabString = tabString;
	}

	public override void Close()
	{
		_writer.Close();
	}

	public override ValueTask DisposeAsync()
	{
		return _writer.DisposeAsync();
	}

	public override void Flush()
	{
		_writer.Flush();
	}

	public override Task FlushAsync()
	{
		return _writer.FlushAsync();
	}

	protected virtual void OutputTabs()
	{
		if (_tabsPending)
		{
			for (int i = 0; i < _indentLevel; i++)
			{
				_writer.Write(_tabString);
			}
			_tabsPending = false;
		}
	}

	protected virtual async Task OutputTabsAsync()
	{
		if (_tabsPending)
		{
			for (int i = 0; i < _indentLevel; i++)
			{
				await _writer.WriteAsync(_tabString).ConfigureAwait(continueOnCapturedContext: false);
			}
			_tabsPending = false;
		}
	}

	public override void Write(string? s)
	{
		OutputTabs();
		_writer.Write(s);
	}

	public override void Write(bool value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(char value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(char[]? buffer)
	{
		OutputTabs();
		_writer.Write(buffer);
	}

	public override void Write(char[] buffer, int index, int count)
	{
		OutputTabs();
		_writer.Write(buffer, index, count);
	}

	public override void Write(double value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(float value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(int value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(long value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(object? value)
	{
		OutputTabs();
		_writer.Write(value);
	}

	public override void Write(string format, object? arg0)
	{
		OutputTabs();
		_writer.Write(format, arg0);
	}

	public override void Write(string format, object? arg0, object? arg1)
	{
		OutputTabs();
		_writer.Write(format, arg0, arg1);
	}

	public override void Write(string format, params object?[] arg)
	{
		OutputTabs();
		_writer.Write(format, arg);
	}

	public override async Task WriteAsync(char value)
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteAsync(value).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task WriteAsync(char[] buffer, int index, int count)
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task WriteAsync(string? value)
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteAsync(value).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default(CancellationToken))
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteAsync(value, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public void WriteLineNoTabs(string? s)
	{
		_writer.WriteLine(s);
	}

	public Task WriteLineNoTabsAsync(string? s)
	{
		return _writer.WriteLineAsync(s);
	}

	public override void WriteLine(string? s)
	{
		OutputTabs();
		_writer.WriteLine(s);
		_tabsPending = true;
	}

	public override void WriteLine()
	{
		OutputTabs();
		_writer.WriteLine();
		_tabsPending = true;
	}

	public override void WriteLine(bool value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(char value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(char[]? buffer)
	{
		OutputTabs();
		_writer.WriteLine(buffer);
		_tabsPending = true;
	}

	public override void WriteLine(char[] buffer, int index, int count)
	{
		OutputTabs();
		_writer.WriteLine(buffer, index, count);
		_tabsPending = true;
	}

	public override void WriteLine(double value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(float value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(int value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(long value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(object? value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override void WriteLine(string format, object? arg0)
	{
		OutputTabs();
		_writer.WriteLine(format, arg0);
		_tabsPending = true;
	}

	public override void WriteLine(string format, object? arg0, object? arg1)
	{
		OutputTabs();
		_writer.WriteLine(format, arg0, arg1);
		_tabsPending = true;
	}

	public override void WriteLine(string format, params object?[] arg)
	{
		OutputTabs();
		_writer.WriteLine(format, arg);
		_tabsPending = true;
	}

	[CLSCompliant(false)]
	public override void WriteLine(uint value)
	{
		OutputTabs();
		_writer.WriteLine(value);
		_tabsPending = true;
	}

	public override async Task WriteLineAsync()
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteLineAsync().ConfigureAwait(continueOnCapturedContext: false);
		_tabsPending = true;
	}

	public override async Task WriteLineAsync(char value)
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteLineAsync(value).ConfigureAwait(continueOnCapturedContext: false);
		_tabsPending = true;
	}

	public override async Task WriteLineAsync(char[] buffer, int index, int count)
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteLineAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_tabsPending = true;
	}

	public override async Task WriteLineAsync(string? value)
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteLineAsync(value).ConfigureAwait(continueOnCapturedContext: false);
		_tabsPending = true;
	}

	public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteLineAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_tabsPending = true;
	}

	public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default(CancellationToken))
	{
		await OutputTabsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteLineAsync(value, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_tabsPending = true;
	}
}
