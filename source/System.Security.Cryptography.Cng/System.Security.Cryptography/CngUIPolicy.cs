namespace System.Security.Cryptography;

public sealed class CngUIPolicy
{
	public CngUIProtectionLevels ProtectionLevel { get; private set; }

	public string? FriendlyName { get; private set; }

	public string? Description { get; private set; }

	public string? UseContext { get; private set; }

	public string? CreationTitle { get; private set; }

	public CngUIPolicy(CngUIProtectionLevels protectionLevel)
		: this(protectionLevel, null)
	{
	}

	public CngUIPolicy(CngUIProtectionLevels protectionLevel, string? friendlyName)
		: this(protectionLevel, friendlyName, null)
	{
	}

	public CngUIPolicy(CngUIProtectionLevels protectionLevel, string? friendlyName, string? description)
		: this(protectionLevel, friendlyName, description, null)
	{
	}

	public CngUIPolicy(CngUIProtectionLevels protectionLevel, string? friendlyName, string? description, string? useContext)
		: this(protectionLevel, friendlyName, description, useContext, null)
	{
	}

	public CngUIPolicy(CngUIProtectionLevels protectionLevel, string? friendlyName, string? description, string? useContext, string? creationTitle)
	{
		ProtectionLevel = protectionLevel;
		FriendlyName = friendlyName;
		Description = description;
		UseContext = useContext;
		CreationTitle = creationTitle;
	}
}
