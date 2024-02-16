using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class VerificationException : SystemException
{
	public VerificationException()
		: base(SR.Verification_Exception)
	{
		base.HResult = -2146233075;
	}

	public VerificationException(string? message)
		: base(message)
	{
		base.HResult = -2146233075;
	}

	public VerificationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233075;
	}

	protected VerificationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
