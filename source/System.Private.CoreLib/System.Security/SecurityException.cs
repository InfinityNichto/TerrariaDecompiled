using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SecurityException : SystemException
{
	public object? Demanded { get; set; }

	public object? DenySetInstance { get; set; }

	public AssemblyName? FailedAssemblyInfo { get; set; }

	public string? GrantedSet { get; set; }

	public MethodInfo? Method { get; set; }

	public string? PermissionState { get; set; }

	public Type? PermissionType { get; set; }

	public object? PermitOnlySetInstance { get; set; }

	public string? RefusedSet { get; set; }

	public string? Url { get; set; }

	public SecurityException()
		: base(SR.Arg_SecurityException)
	{
		base.HResult = -2146233078;
	}

	public SecurityException(string? message)
		: base(message)
	{
		base.HResult = -2146233078;
	}

	public SecurityException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233078;
	}

	public SecurityException(string? message, Type? type)
		: base(message)
	{
		base.HResult = -2146233078;
		PermissionType = type;
	}

	public SecurityException(string? message, Type? type, string? state)
		: base(message)
	{
		base.HResult = -2146233078;
		PermissionType = type;
		PermissionState = state;
	}

	protected SecurityException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		Demanded = (string)info.GetValueNoThrow("Demanded", typeof(string));
		GrantedSet = (string)info.GetValueNoThrow("GrantedSet", typeof(string));
		RefusedSet = (string)info.GetValueNoThrow("RefusedSet", typeof(string));
		DenySetInstance = (string)info.GetValueNoThrow("Denied", typeof(string));
		PermitOnlySetInstance = (string)info.GetValueNoThrow("PermitOnly", typeof(string));
		Url = (string)info.GetValueNoThrow("Url", typeof(string));
	}

	public override string ToString()
	{
		return base.ToString();
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("Demanded", Demanded, typeof(string));
		info.AddValue("GrantedSet", GrantedSet, typeof(string));
		info.AddValue("RefusedSet", RefusedSet, typeof(string));
		info.AddValue("Denied", DenySetInstance, typeof(string));
		info.AddValue("PermitOnly", PermitOnlySetInstance, typeof(string));
		info.AddValue("Url", Url, typeof(string));
	}
}
