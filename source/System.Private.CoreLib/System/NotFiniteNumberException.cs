using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class NotFiniteNumberException : ArithmeticException
{
	private readonly double _offendingNumber;

	public double OffendingNumber => _offendingNumber;

	public NotFiniteNumberException()
		: base(SR.Arg_NotFiniteNumberException)
	{
		_offendingNumber = 0.0;
		base.HResult = -2146233048;
	}

	public NotFiniteNumberException(double offendingNumber)
	{
		_offendingNumber = offendingNumber;
		base.HResult = -2146233048;
	}

	public NotFiniteNumberException(string? message)
		: base(message)
	{
		_offendingNumber = 0.0;
		base.HResult = -2146233048;
	}

	public NotFiniteNumberException(string? message, double offendingNumber)
		: base(message)
	{
		_offendingNumber = offendingNumber;
		base.HResult = -2146233048;
	}

	public NotFiniteNumberException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233048;
	}

	public NotFiniteNumberException(string? message, double offendingNumber, Exception? innerException)
		: base(message, innerException)
	{
		_offendingNumber = offendingNumber;
		base.HResult = -2146233048;
	}

	protected NotFiniteNumberException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_offendingNumber = info.GetDouble("OffendingNumber");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("OffendingNumber", _offendingNumber, typeof(double));
	}
}
