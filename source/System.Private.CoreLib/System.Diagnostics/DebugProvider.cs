using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public class DebugProvider
{
	private sealed class DebugAssertException : Exception
	{
		internal DebugAssertException(string message, string detailMessage, string stackTrace)
			: base(Terminate(message) + Terminate(detailMessage) + stackTrace)
		{
		}

		private static string Terminate(string s)
		{
			if (s == null)
			{
				return s;
			}
			s = s.Trim();
			if (s.Length > 0)
			{
				s += "\r\n";
			}
			return s;
		}
	}

	private static readonly object s_lock = new object();

	private bool _needIndent = true;

	private string _indentString;

	internal static Action<string, string, string, string> s_FailCore;

	internal static Action<string> s_WriteCore;

	private static readonly object s_ForLock = new object();

	[DoesNotReturn]
	public virtual void Fail(string? message, string? detailMessage)
	{
		string stackTrace;
		try
		{
			stackTrace = new StackTrace(0, fNeedFileInfo: true).ToString(StackTrace.TraceFormat.Normal);
		}
		catch
		{
			stackTrace = "";
		}
		WriteAssert(stackTrace, message, detailMessage);
		FailCore(stackTrace, message, detailMessage, "Assertion failed.");
	}

	internal void WriteAssert(string stackTrace, string message, string detailMessage)
	{
		WriteLine(SR.DebugAssertBanner + "\r\n" + SR.DebugAssertShortMessage + "\r\n" + message + "\r\n" + SR.DebugAssertLongMessage + "\r\n" + detailMessage + "\r\n" + stackTrace);
	}

	public virtual void Write(string? message)
	{
		lock (s_lock)
		{
			if (message == null)
			{
				WriteCore(string.Empty);
				return;
			}
			if (_needIndent)
			{
				message = GetIndentString() + message;
				_needIndent = false;
			}
			WriteCore(message);
			if (message.EndsWith("\r\n", StringComparison.Ordinal))
			{
				_needIndent = true;
			}
		}
	}

	public virtual void WriteLine(string? message)
	{
		Write(message + "\r\n");
	}

	public virtual void OnIndentLevelChanged(int indentLevel)
	{
	}

	public virtual void OnIndentSizeChanged(int indentSize)
	{
	}

	private string GetIndentString()
	{
		int num = Debug.IndentSize * Debug.IndentLevel;
		string indentString = _indentString;
		if (indentString != null && indentString.Length == num)
		{
			return _indentString;
		}
		return _indentString = new string(' ', num);
	}

	public static void FailCore(string stackTrace, string? message, string? detailMessage, string errorSource)
	{
		if (s_FailCore != null)
		{
			s_FailCore(stackTrace, message, detailMessage, errorSource);
			return;
		}
		if (Debugger.IsAttached)
		{
			Debugger.Break();
			return;
		}
		DebugAssertException ex = new DebugAssertException(message, detailMessage, stackTrace);
		Environment.FailFast(ex.Message, ex, errorSource);
	}

	public static void WriteCore(string message)
	{
		if (s_WriteCore != null)
		{
			s_WriteCore(message);
			return;
		}
		lock (s_ForLock)
		{
			if (message.Length <= 4091)
			{
				WriteToDebugger(message);
				return;
			}
			int i;
			for (i = 0; i < message.Length - 4091; i += 4091)
			{
				WriteToDebugger(message.Substring(i, 4091));
			}
			WriteToDebugger(message.Substring(i));
		}
	}

	private static void WriteToDebugger(string message)
	{
		if (Debugger.IsLogging())
		{
			Debugger.Log(0, null, message);
		}
		else
		{
			Interop.Kernel32.OutputDebugString(message ?? string.Empty);
		}
	}
}
