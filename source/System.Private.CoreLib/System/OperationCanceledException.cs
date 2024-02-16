using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class OperationCanceledException : SystemException
{
	[NonSerialized]
	private CancellationToken _cancellationToken;

	public CancellationToken CancellationToken
	{
		get
		{
			return _cancellationToken;
		}
		private set
		{
			_cancellationToken = value;
		}
	}

	public OperationCanceledException()
		: base(SR.OperationCanceled)
	{
		base.HResult = -2146233029;
	}

	public OperationCanceledException(string? message)
		: base(message)
	{
		base.HResult = -2146233029;
	}

	public OperationCanceledException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233029;
	}

	public OperationCanceledException(CancellationToken token)
		: this()
	{
		CancellationToken = token;
	}

	public OperationCanceledException(string? message, CancellationToken token)
		: this(message)
	{
		CancellationToken = token;
	}

	public OperationCanceledException(string? message, Exception? innerException, CancellationToken token)
		: this(message, innerException)
	{
		CancellationToken = token;
	}

	protected OperationCanceledException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
