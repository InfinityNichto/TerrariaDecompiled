namespace System.Xml.Xsl;

public sealed class XsltSettings
{
	private bool _enableDocumentFunction;

	private bool _enableScript;

	private bool _checkOnly;

	private bool _includeDebugInformation;

	private int _warningLevel = -1;

	private bool _treatWarningsAsErrors;

	public static XsltSettings Default => new XsltSettings(enableDocumentFunction: false, enableScript: false);

	public static XsltSettings TrustedXslt => new XsltSettings(enableDocumentFunction: true, enableScript: true);

	public bool EnableDocumentFunction
	{
		get
		{
			return _enableDocumentFunction;
		}
		set
		{
			_enableDocumentFunction = value;
		}
	}

	public bool EnableScript
	{
		get
		{
			return _enableScript;
		}
		set
		{
			_enableScript = value;
		}
	}

	internal bool CheckOnly => _checkOnly;

	internal bool IncludeDebugInformation => _includeDebugInformation;

	internal int WarningLevel => _warningLevel;

	internal bool TreatWarningsAsErrors => _treatWarningsAsErrors;

	public XsltSettings()
	{
	}

	public XsltSettings(bool enableDocumentFunction, bool enableScript)
	{
		_enableDocumentFunction = enableDocumentFunction;
		_enableScript = enableScript;
	}
}
