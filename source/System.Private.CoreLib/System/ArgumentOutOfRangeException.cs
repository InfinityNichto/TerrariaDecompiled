using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArgumentOutOfRangeException : ArgumentException
{
	private readonly object _actualValue;

	public override string Message
	{
		get
		{
			string message = base.Message;
			if (_actualValue != null)
			{
				string text = SR.Format(SR.ArgumentOutOfRange_ActualValue, _actualValue);
				if (message == null)
				{
					return text;
				}
				return message + "\r\n" + text;
			}
			return message;
		}
	}

	public virtual object? ActualValue => _actualValue;

	public ArgumentOutOfRangeException()
		: base(SR.Arg_ArgumentOutOfRangeException)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? paramName)
		: base(SR.Arg_ArgumentOutOfRangeException, paramName)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? paramName, string? message)
		: base(message, paramName)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? paramName, object? actualValue, string? message)
		: base(message, paramName)
	{
		_actualValue = actualValue;
		base.HResult = -2146233086;
	}

	protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_actualValue = info.GetValue("ActualValue", typeof(object));
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("ActualValue", _actualValue, typeof(object));
	}
}
