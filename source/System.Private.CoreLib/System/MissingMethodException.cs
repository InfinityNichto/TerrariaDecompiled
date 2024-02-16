using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingMethodException : MissingMemberException
{
	public override string Message
	{
		get
		{
			if (ClassName != null)
			{
				return SR.Format(SR.MissingMethod_Name, ClassName + "." + MemberName + ((Signature != null) ? (" " + MissingMemberException.FormatSignature(Signature)) : string.Empty));
			}
			return base.Message;
		}
	}

	public MissingMethodException()
		: base(SR.Arg_MissingMethodException)
	{
		base.HResult = -2146233069;
	}

	public MissingMethodException(string? message)
		: base(message)
	{
		base.HResult = -2146233069;
	}

	public MissingMethodException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233069;
	}

	public MissingMethodException(string? className, string? methodName)
	{
		ClassName = className;
		MemberName = methodName;
	}

	protected MissingMethodException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
