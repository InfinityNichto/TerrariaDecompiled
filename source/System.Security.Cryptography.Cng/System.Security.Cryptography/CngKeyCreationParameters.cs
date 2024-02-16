namespace System.Security.Cryptography;

public sealed class CngKeyCreationParameters
{
	private CngProvider _provider;

	public CngExportPolicies? ExportPolicy { get; set; }

	public CngKeyCreationOptions KeyCreationOptions { get; set; }

	public CngKeyUsages? KeyUsage { get; set; }

	public CngPropertyCollection Parameters { get; private set; }

	public IntPtr ParentWindowHandle { get; set; }

	public CngProvider Provider
	{
		get
		{
			return _provider;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_provider = value;
		}
	}

	public CngUIPolicy? UIPolicy { get; set; }

	public CngKeyCreationParameters()
	{
		Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;
		Parameters = new CngPropertyCollection();
	}
}
