using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class LicenseException : SystemException
{
	private readonly object _instance;

	public Type? LicensedType { get; }

	public LicenseException(Type? type)
		: this(type, null, System.SR.Format(System.SR.LicExceptionTypeOnly, type?.FullName))
	{
	}

	public LicenseException(Type? type, object? instance)
		: this(type, null, System.SR.Format(System.SR.LicExceptionTypeAndInstance, type?.FullName, instance?.GetType().FullName))
	{
	}

	public LicenseException(Type? type, object? instance, string? message)
		: base(message)
	{
		LicensedType = type;
		_instance = instance;
		base.HResult = -2146232063;
	}

	public LicenseException(Type? type, object? instance, string? message, Exception? innerException)
		: base(message, innerException)
	{
		LicensedType = type;
		_instance = instance;
		base.HResult = -2146232063;
	}

	protected LicenseException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("type", null);
		info.AddValue("instance", _instance);
	}
}
