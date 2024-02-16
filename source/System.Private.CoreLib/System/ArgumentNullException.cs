using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArgumentNullException : ArgumentException
{
	public ArgumentNullException()
		: base(SR.ArgumentNull_Generic)
	{
		base.HResult = -2147467261;
	}

	public ArgumentNullException(string? paramName)
		: base(SR.ArgumentNull_Generic, paramName)
	{
		base.HResult = -2147467261;
	}

	public ArgumentNullException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467261;
	}

	public ArgumentNullException(string? paramName, string? message)
		: base(message, paramName)
	{
		base.HResult = -2147467261;
	}

	protected ArgumentNullException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (argument == null)
		{
			Throw(paramName);
		}
	}

	[DoesNotReturn]
	private static void Throw(string paramName)
	{
		throw new ArgumentNullException(paramName);
	}
}
