using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class TypeInitializationException : SystemException
{
	private readonly string _typeName;

	public string TypeName => _typeName ?? string.Empty;

	private TypeInitializationException()
		: base(SR.TypeInitialization_Default)
	{
		base.HResult = -2146233036;
	}

	public TypeInitializationException(string? fullTypeName, Exception? innerException)
		: this(fullTypeName, SR.Format(SR.TypeInitialization_Type, fullTypeName), innerException)
	{
	}

	internal TypeInitializationException(string message)
		: base(message)
	{
		base.HResult = -2146233036;
	}

	internal TypeInitializationException(string fullTypeName, string message, Exception innerException)
		: base(message, innerException)
	{
		_typeName = fullTypeName;
		base.HResult = -2146233036;
	}

	private TypeInitializationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_typeName = info.GetString("TypeName");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("TypeName", TypeName, typeof(string));
	}
}
