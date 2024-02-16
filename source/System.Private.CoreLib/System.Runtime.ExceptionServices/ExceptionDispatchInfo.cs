using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.ExceptionServices;

public sealed class ExceptionDispatchInfo
{
	private readonly Exception _exception;

	private readonly Exception.DispatchState _dispatchState;

	public Exception SourceException => _exception;

	private ExceptionDispatchInfo(Exception exception)
	{
		_exception = exception;
		_dispatchState = exception.CaptureDispatchState();
	}

	public static ExceptionDispatchInfo Capture(Exception source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new ExceptionDispatchInfo(source);
	}

	[DoesNotReturn]
	[StackTraceHidden]
	public void Throw()
	{
		_exception.RestoreDispatchState(in _dispatchState);
		throw _exception;
	}

	[DoesNotReturn]
	[StackTraceHidden]
	public static void Throw(Exception source)
	{
		Capture(source).Throw();
	}

	[StackTraceHidden]
	public static Exception SetCurrentStackTrace(Exception source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		source.SetCurrentStackTrace();
		return source;
	}

	public static Exception SetRemoteStackTrace(Exception source, string stackTrace)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (stackTrace == null)
		{
			throw new ArgumentNullException("stackTrace");
		}
		source.SetRemoteStackTrace(stackTrace);
		return source;
	}
}
