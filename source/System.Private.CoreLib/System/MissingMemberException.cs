using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingMemberException : MemberAccessException, ISerializable
{
	protected string? ClassName;

	protected string? MemberName;

	protected byte[]? Signature;

	public override string Message
	{
		get
		{
			if (ClassName == null)
			{
				return base.Message;
			}
			return SR.Format(SR.MissingMember_Name, ClassName + "." + MemberName + ((Signature != null) ? (" " + FormatSignature(Signature)) : string.Empty));
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern string FormatSignature(byte[] signature);

	public MissingMemberException()
		: base(SR.Arg_MissingMemberException)
	{
		base.HResult = -2146233070;
	}

	public MissingMemberException(string? message)
		: base(message)
	{
		base.HResult = -2146233070;
	}

	public MissingMemberException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233070;
	}

	public MissingMemberException(string? className, string? memberName)
	{
		ClassName = className;
		MemberName = memberName;
	}

	protected MissingMemberException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		ClassName = info.GetString("MMClassName");
		MemberName = info.GetString("MMMemberName");
		Signature = (byte[])info.GetValue("MMSignature", typeof(byte[]));
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("MMClassName", ClassName, typeof(string));
		info.AddValue("MMMemberName", MemberName, typeof(string));
		info.AddValue("MMSignature", Signature, typeof(byte[]));
	}
}
