using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ExternalException : SystemException
{
	public virtual int ErrorCode => base.HResult;

	public ExternalException()
		: base(SR.Arg_ExternalException)
	{
		base.HResult = -2147467259;
	}

	public ExternalException(string? message)
		: base(message)
	{
		base.HResult = -2147467259;
	}

	public ExternalException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147467259;
	}

	public ExternalException(string? message, int errorCode)
		: base(message)
	{
		base.HResult = errorCode;
	}

	protected ExternalException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override string ToString()
	{
		string message = Message;
		string text = $"{GetType()} (0x{base.HResult:X8})";
		if (!string.IsNullOrEmpty(message))
		{
			text = text + ": " + message;
		}
		Exception innerException = base.InnerException;
		if (innerException != null)
		{
			text = text + "\r\n ---> " + innerException.ToString();
		}
		if (StackTrace != null)
		{
			text = text + "\r\n" + StackTrace;
		}
		return text;
	}
}
