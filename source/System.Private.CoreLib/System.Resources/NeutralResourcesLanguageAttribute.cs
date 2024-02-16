namespace System.Resources;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class NeutralResourcesLanguageAttribute : Attribute
{
	public string CultureName { get; }

	public UltimateResourceFallbackLocation Location { get; }

	public NeutralResourcesLanguageAttribute(string cultureName)
	{
		if (cultureName == null)
		{
			throw new ArgumentNullException("cultureName");
		}
		CultureName = cultureName;
		Location = UltimateResourceFallbackLocation.MainAssembly;
	}

	public NeutralResourcesLanguageAttribute(string cultureName, UltimateResourceFallbackLocation location)
	{
		if (cultureName == null)
		{
			throw new ArgumentNullException("cultureName");
		}
		if (!Enum.IsDefined(typeof(UltimateResourceFallbackLocation), location))
		{
			throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_FallbackLoc, location));
		}
		CultureName = cultureName;
		Location = location;
	}
}
