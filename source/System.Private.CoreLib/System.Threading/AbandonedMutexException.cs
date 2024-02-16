using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class AbandonedMutexException : SystemException
{
	private int _mutexIndex = -1;

	private Mutex _mutex;

	public Mutex? Mutex => _mutex;

	public int MutexIndex => _mutexIndex;

	public AbandonedMutexException()
		: base(SR.Threading_AbandonedMutexException)
	{
		base.HResult = -2146233043;
	}

	public AbandonedMutexException(string? message)
		: base(message)
	{
		base.HResult = -2146233043;
	}

	public AbandonedMutexException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233043;
	}

	public AbandonedMutexException(int location, WaitHandle? handle)
		: base(SR.Threading_AbandonedMutexException)
	{
		base.HResult = -2146233043;
		SetupException(location, handle);
	}

	public AbandonedMutexException(string? message, int location, WaitHandle? handle)
		: base(message)
	{
		base.HResult = -2146233043;
		SetupException(location, handle);
	}

	public AbandonedMutexException(string? message, Exception? inner, int location, WaitHandle? handle)
		: base(message, inner)
	{
		base.HResult = -2146233043;
		SetupException(location, handle);
	}

	protected AbandonedMutexException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	private void SetupException(int location, WaitHandle handle)
	{
		_mutexIndex = location;
		_mutex = handle as Mutex;
	}
}
