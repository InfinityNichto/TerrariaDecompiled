using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class COMException : ExternalException
{
	public COMException()
		: base(SR.Arg_COMException)
	{
		base.HResult = -2147467259;
	}

	public COMException(string? message)
		: base(message)
	{
		base.HResult = -2147467259;
	}

	public COMException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147467259;
	}

	public COMException(string? message, int errorCode)
		: base(message)
	{
		base.HResult = errorCode;
	}

	protected COMException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
		handler.AppendFormatted(GetType());
		handler.AppendLiteral(" (0x");
		handler.AppendFormatted(base.HResult, "X8");
		handler.AppendLiteral(")");
		stringBuilder2.Append(ref handler);
		string message = Message;
		if (!string.IsNullOrEmpty(message))
		{
			stringBuilder.Append(": ").Append(message);
		}
		Exception innerException = base.InnerException;
		if (innerException != null)
		{
			stringBuilder.Append("\r\n ---> ").Append(innerException.ToString());
		}
		string stackTrace = StackTrace;
		if (stackTrace != null)
		{
			stringBuilder.AppendLine().Append(stackTrace);
		}
		return stringBuilder.ToString();
	}
}
