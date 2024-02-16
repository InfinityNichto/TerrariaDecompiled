using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Resources;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingSatelliteAssemblyException : SystemException
{
	private readonly string _cultureName;

	public string? CultureName => _cultureName;

	public MissingSatelliteAssemblyException()
		: base(SR.MissingSatelliteAssembly_Default)
	{
		base.HResult = -2146233034;
	}

	public MissingSatelliteAssemblyException(string? message)
		: base(message)
	{
		base.HResult = -2146233034;
	}

	public MissingSatelliteAssemblyException(string? message, string? cultureName)
		: base(message)
	{
		base.HResult = -2146233034;
		_cultureName = cultureName;
	}

	public MissingSatelliteAssemblyException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233034;
	}

	protected MissingSatelliteAssemblyException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
