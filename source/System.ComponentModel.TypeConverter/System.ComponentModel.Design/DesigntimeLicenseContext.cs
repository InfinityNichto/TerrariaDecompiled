using System.Collections;
using System.Reflection;

namespace System.ComponentModel.Design;

public class DesigntimeLicenseContext : LicenseContext
{
	internal Hashtable _savedLicenseKeys = new Hashtable();

	public override LicenseUsageMode UsageMode => LicenseUsageMode.Designtime;

	public override string? GetSavedLicenseKey(Type type, Assembly? resourceAssembly)
	{
		return null;
	}

	public override void SetSavedLicenseKey(Type type, string key)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		_savedLicenseKeys[type.AssemblyQualifiedName] = key;
	}
}
