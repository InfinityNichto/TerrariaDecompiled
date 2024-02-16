namespace System.Xml;

public class XmlImplementation
{
	private readonly XmlNameTable _nameTable;

	internal XmlNameTable NameTable => _nameTable;

	public XmlImplementation()
		: this(new NameTable())
	{
	}

	public XmlImplementation(XmlNameTable nt)
	{
		_nameTable = nt;
	}

	public bool HasFeature(string strFeature, string strVersion)
	{
		if (string.Equals("XML", strFeature, StringComparison.OrdinalIgnoreCase))
		{
			switch (strVersion)
			{
			case null:
			case "1.0":
			case "2.0":
				return true;
			}
		}
		return false;
	}

	public virtual XmlDocument CreateDocument()
	{
		return new XmlDocument(this);
	}
}
