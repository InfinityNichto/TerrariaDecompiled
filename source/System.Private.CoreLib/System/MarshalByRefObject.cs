using System.Runtime.InteropServices;

namespace System;

[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public abstract class MarshalByRefObject
{
	[Obsolete("This Remoting API is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0010", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public object GetLifetimeService()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_Remoting);
	}

	[Obsolete("This Remoting API is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0010", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual object InitializeLifetimeService()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_Remoting);
	}

	protected MarshalByRefObject MemberwiseClone(bool cloneIdentity)
	{
		return (MarshalByRefObject)MemberwiseClone();
	}
}
