using System.Reflection;

namespace System.ComponentModel;

public class LicenseContext : IServiceProvider
{
	public virtual LicenseUsageMode UsageMode => LicenseUsageMode.Runtime;

	public virtual string? GetSavedLicenseKey(Type type, Assembly? resourceAssembly)
	{
		return null;
	}

	public virtual object? GetService(Type type)
	{
		return null;
	}

	public virtual void SetSavedLicenseKey(Type type, string key)
	{
	}
}
