#define DEBUG
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics;

public static class Debug
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[InterpolatedStringHandler]
	public struct AssertInterpolatedStringHandler
	{
		private StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;

		public AssertInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
		{
			if (condition)
			{
				_stringBuilderHandler = default(StringBuilder.AppendInterpolatedStringHandler);
				shouldAppend = false;
			}
			else
			{
				_stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, new StringBuilder());
				shouldAppend = true;
			}
		}

		internal string ToStringAndClear()
		{
			StringBuilder stringBuilder = _stringBuilderHandler._stringBuilder;
			string result = ((stringBuilder != null) ? stringBuilder.ToString() : string.Empty);
			_stringBuilderHandler = default(StringBuilder.AppendInterpolatedStringHandler);
			return result;
		}

		public void AppendLiteral(string value)
		{
			_stringBuilderHandler.AppendLiteral(value);
		}

		public void AppendFormatted<T>(T value)
		{
			_stringBuilderHandler.AppendFormatted(value);
		}

		public void AppendFormatted<T>(T value, string? format)
		{
			_stringBuilderHandler.AppendFormatted(value, format);
		}

		public void AppendFormatted<T>(T value, int alignment)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment);
		}

		public void AppendFormatted<T>(T value, int alignment, string? format)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}

		public void AppendFormatted(ReadOnlySpan<char> value)
		{
			_stringBuilderHandler.AppendFormatted(value);
		}

		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}

		public void AppendFormatted(string? value)
		{
			_stringBuilderHandler.AppendFormatted(value);
		}

		public void AppendFormatted(string? value, int alignment = 0, string? format = null)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}

		public void AppendFormatted(object? value, int alignment = 0, string? format = null)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[InterpolatedStringHandler]
	public struct WriteIfInterpolatedStringHandler
	{
		private StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;

		public WriteIfInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
		{
			if (condition)
			{
				_stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, StringBuilderCache.Acquire(DefaultInterpolatedStringHandler.GetDefaultLength(literalLength, formattedCount)));
				shouldAppend = true;
			}
			else
			{
				_stringBuilderHandler = default(StringBuilder.AppendInterpolatedStringHandler);
				shouldAppend = false;
			}
		}

		internal string ToStringAndClear()
		{
			StringBuilder stringBuilder = _stringBuilderHandler._stringBuilder;
			string result = ((stringBuilder != null) ? StringBuilderCache.GetStringAndRelease(stringBuilder) : string.Empty);
			_stringBuilderHandler = default(StringBuilder.AppendInterpolatedStringHandler);
			return result;
		}

		public void AppendLiteral(string value)
		{
			_stringBuilderHandler.AppendLiteral(value);
		}

		public void AppendFormatted<T>(T value)
		{
			_stringBuilderHandler.AppendFormatted(value);
		}

		public void AppendFormatted<T>(T value, string? format)
		{
			_stringBuilderHandler.AppendFormatted(value, format);
		}

		public void AppendFormatted<T>(T value, int alignment)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment);
		}

		public void AppendFormatted<T>(T value, int alignment, string? format)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}

		public void AppendFormatted(ReadOnlySpan<char> value)
		{
			_stringBuilderHandler.AppendFormatted(value);
		}

		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}

		public void AppendFormatted(string? value)
		{
			_stringBuilderHandler.AppendFormatted(value);
		}

		public void AppendFormatted(string? value, int alignment = 0, string? format = null)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}

		public void AppendFormatted(object? value, int alignment = 0, string? format = null)
		{
			_stringBuilderHandler.AppendFormatted(value, alignment, format);
		}
	}

	private static volatile DebugProvider s_provider = new DebugProvider();

	[ThreadStatic]
	private static int t_indentLevel;

	private static volatile int s_indentSize = 4;

	public static bool AutoFlush
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	public static int IndentLevel
	{
		get
		{
			return t_indentLevel;
		}
		set
		{
			t_indentLevel = ((value >= 0) ? value : 0);
			s_provider.OnIndentLevelChanged(t_indentLevel);
		}
	}

	public static int IndentSize
	{
		get
		{
			return s_indentSize;
		}
		set
		{
			s_indentSize = ((value >= 0) ? value : 0);
			s_provider.OnIndentSizeChanged(s_indentSize);
		}
	}

	public static DebugProvider SetProvider(DebugProvider provider)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		return Interlocked.Exchange(ref s_provider, provider);
	}

	[Conditional("DEBUG")]
	public static void Close()
	{
	}

	[Conditional("DEBUG")]
	public static void Flush()
	{
	}

	[Conditional("DEBUG")]
	public static void Indent()
	{
		IndentLevel++;
	}

	[Conditional("DEBUG")]
	public static void Unindent()
	{
		IndentLevel--;
	}

	[Conditional("DEBUG")]
	public static void Print(string? message)
	{
		WriteLine(message);
	}

	[Conditional("DEBUG")]
	public static void Print(string format, params object?[] args)
	{
		WriteLine(string.Format(null, format, args));
	}

	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition)
	{
		Assert(condition, string.Empty, string.Empty);
	}

	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? message)
	{
		Assert(condition, message, string.Empty);
	}

	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler message)
	{
		Assert(condition, message.ToStringAndClear());
	}

	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, string? detailMessage)
	{
		if (!condition)
		{
			Fail(message, detailMessage);
		}
	}

	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler message, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler detailMessage)
	{
		Assert(condition, message.ToStringAndClear(), detailMessage.ToStringAndClear());
	}

	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, string detailMessageFormat, params object?[] args)
	{
		Assert(condition, message, string.Format(detailMessageFormat, args));
	}

	internal static void ContractFailure(string message, string detailMessage, string failureKindMessage)
	{
		string stackTrace;
		try
		{
			stackTrace = new StackTrace(2, fNeedFileInfo: true).ToString(StackTrace.TraceFormat.Normal);
		}
		catch
		{
			stackTrace = "";
		}
		s_provider.WriteAssert(stackTrace, message, detailMessage);
		DebugProvider.FailCore(stackTrace, message, detailMessage, failureKindMessage);
	}

	[Conditional("DEBUG")]
	[DoesNotReturn]
	public static void Fail(string? message)
	{
		Fail(message, string.Empty);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[Conditional("DEBUG")]
	[DoesNotReturn]
	public static void Fail(string? message, string? detailMessage)
	{
		s_provider.Fail(message, detailMessage);
	}

	[Conditional("DEBUG")]
	public static void WriteLine(string? message)
	{
		s_provider.WriteLine(message);
	}

	[Conditional("DEBUG")]
	public static void Write(string? message)
	{
		s_provider.Write(message);
	}

	[Conditional("DEBUG")]
	public static void WriteLine(object? value)
	{
		WriteLine(value?.ToString());
	}

	[Conditional("DEBUG")]
	public static void WriteLine(object? value, string? category)
	{
		WriteLine(value?.ToString(), category);
	}

	[Conditional("DEBUG")]
	public static void WriteLine(string format, params object?[] args)
	{
		WriteLine(string.Format(null, format, args));
	}

	[Conditional("DEBUG")]
	public static void WriteLine(string? message, string? category)
	{
		if (category == null)
		{
			WriteLine(message);
		}
		else
		{
			WriteLine(category + ": " + message);
		}
	}

	[Conditional("DEBUG")]
	public static void Write(object? value)
	{
		Write(value?.ToString());
	}

	[Conditional("DEBUG")]
	public static void Write(string? message, string? category)
	{
		if (category == null)
		{
			Write(message);
		}
		else
		{
			Write(category + ": " + message);
		}
	}

	[Conditional("DEBUG")]
	public static void Write(object? value, string? category)
	{
		Write(value?.ToString(), category);
	}

	[Conditional("DEBUG")]
	public static void WriteIf(bool condition, string? message)
	{
		if (condition)
		{
			Write(message);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message)
	{
		WriteIf(condition, message.ToStringAndClear());
	}

	[Conditional("DEBUG")]
	public static void WriteIf(bool condition, object? value)
	{
		if (condition)
		{
			Write(value);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteIf(bool condition, string? message, string? category)
	{
		if (condition)
		{
			Write(message, category);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message, string? category)
	{
		WriteIf(condition, message.ToStringAndClear(), category);
	}

	[Conditional("DEBUG")]
	public static void WriteIf(bool condition, object? value, string? category)
	{
		if (condition)
		{
			Write(value, category);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteLineIf(bool condition, object? value)
	{
		if (condition)
		{
			WriteLine(value);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteLineIf(bool condition, object? value, string? category)
	{
		if (condition)
		{
			WriteLine(value, category);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteLineIf(bool condition, string? message)
	{
		if (condition)
		{
			WriteLine(message);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteLineIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message)
	{
		WriteLineIf(condition, message.ToStringAndClear());
	}

	[Conditional("DEBUG")]
	public static void WriteLineIf(bool condition, string? message, string? category)
	{
		if (condition)
		{
			WriteLine(message, category);
		}
	}

	[Conditional("DEBUG")]
	public static void WriteLineIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message, string? category)
	{
		WriteLineIf(condition, message.ToStringAndClear(), category);
	}
}
