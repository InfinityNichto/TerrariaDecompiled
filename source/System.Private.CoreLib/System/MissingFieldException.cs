using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingFieldException : MissingMemberException, ISerializable
{
	public override string Message
	{
		get
		{
			if (ClassName == null)
			{
				return base.Message;
			}
			return SR.Format(SR.MissingField_Name, ((Signature != null) ? (MissingMemberException.FormatSignature(Signature) + " ") : "") + ClassName + "." + MemberName);
		}
	}

	public MissingFieldException()
		: base(SR.Arg_MissingFieldException)
	{
		base.HResult = -2146233071;
	}

	public MissingFieldException(string? message)
		: base(message)
	{
		base.HResult = -2146233071;
	}

	public MissingFieldException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233071;
	}

	public MissingFieldException(string? className, string? fieldName)
	{
		ClassName = className;
		MemberName = fieldName;
	}

	protected MissingFieldException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
