namespace System.Net.Mail;

internal readonly struct ParseAddressInfo
{
	public string DisplayName { get; }

	public string User { get; }

	public string Host { get; }

	public ParseAddressInfo(string displayName, string userName, string domain)
	{
		DisplayName = displayName;
		User = userName;
		Host = domain;
	}
}
