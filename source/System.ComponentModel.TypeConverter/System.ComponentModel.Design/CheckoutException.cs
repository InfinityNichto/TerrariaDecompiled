using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.ComponentModel.Design;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CheckoutException : ExternalException
{
	public static readonly CheckoutException Canceled = new CheckoutException(System.SR.CHECKOUTCanceled, -2147467260);

	public CheckoutException()
	{
	}

	public CheckoutException(string? message)
		: base(message)
	{
	}

	public CheckoutException(string? message, int errorCode)
		: base(message, errorCode)
	{
	}

	protected CheckoutException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public CheckoutException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
