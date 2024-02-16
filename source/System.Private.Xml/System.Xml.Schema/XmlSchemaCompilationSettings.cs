namespace System.Xml.Schema;

public sealed class XmlSchemaCompilationSettings
{
	private bool _enableUpaCheck;

	public bool EnableUpaCheck
	{
		get
		{
			return _enableUpaCheck;
		}
		set
		{
			_enableUpaCheck = value;
		}
	}

	public XmlSchemaCompilationSettings()
	{
		_enableUpaCheck = true;
	}
}
