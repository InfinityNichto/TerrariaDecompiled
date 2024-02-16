using System.IO;

namespace System.Diagnostics;

public class DefaultTraceListener : TraceListener
{
	private bool _assertUIEnabled;

	private bool _settingsInitialized;

	private string _logFileName;

	public bool AssertUiEnabled
	{
		get
		{
			if (!_settingsInitialized)
			{
				InitializeSettings();
			}
			return _assertUIEnabled;
		}
		set
		{
			if (!_settingsInitialized)
			{
				InitializeSettings();
			}
			_assertUIEnabled = value;
		}
	}

	public string? LogFileName
	{
		get
		{
			if (!_settingsInitialized)
			{
				InitializeSettings();
			}
			return _logFileName;
		}
		set
		{
			if (!_settingsInitialized)
			{
				InitializeSettings();
			}
			_logFileName = value;
		}
	}

	public DefaultTraceListener()
		: base("Default")
	{
	}

	public override void Fail(string? message)
	{
		Fail(message, null);
	}

	public override void Fail(string? message, string? detailMessage)
	{
		string stackTrace;
		try
		{
			stackTrace = new StackTrace(fNeedFileInfo: true).ToString();
		}
		catch
		{
			stackTrace = "";
		}
		WriteAssert(stackTrace, message, detailMessage);
		if (AssertUiEnabled)
		{
			DebugProvider.FailCore(stackTrace, message, detailMessage, "Assertion Failed");
		}
	}

	private void InitializeSettings()
	{
		_assertUIEnabled = DiagnosticsConfiguration.AssertUIEnabled;
		_logFileName = DiagnosticsConfiguration.LogFileName;
		_settingsInitialized = true;
	}

	private void WriteAssert(string stackTrace, string message, string detailMessage)
	{
		WriteLine(System.SR.DebugAssertBanner + Environment.NewLine + System.SR.DebugAssertShortMessage + Environment.NewLine + message + Environment.NewLine + System.SR.DebugAssertLongMessage + Environment.NewLine + detailMessage + Environment.NewLine + stackTrace);
	}

	public override void Write(string? message)
	{
		Write(message, useLogFile: true);
	}

	public override void WriteLine(string? message)
	{
		WriteLine(message, useLogFile: true);
	}

	private void WriteLine(string message, bool useLogFile)
	{
		if (base.NeedIndent)
		{
			WriteIndent();
		}
		Write(message + Environment.NewLine, useLogFile);
		base.NeedIndent = true;
	}

	private void Write(string message, bool useLogFile)
	{
		if (message == null)
		{
			message = string.Empty;
		}
		if (base.NeedIndent && message.Length != 0)
		{
			WriteIndent();
		}
		DebugProvider.WriteCore(message);
		if (useLogFile && !string.IsNullOrEmpty(LogFileName))
		{
			WriteToLogFile(message);
		}
	}

	private void WriteToLogFile(string message)
	{
		try
		{
			File.AppendAllText(LogFileName, message);
		}
		catch (Exception p)
		{
			WriteLine(System.SR.Format(System.SR.ExceptionOccurred, LogFileName, p), useLogFile: false);
		}
	}
}
