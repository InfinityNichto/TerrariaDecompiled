using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Qil;

internal class WhitespaceRule
{
	private string _localName;

	private string _namespaceName;

	private bool _preserveSpace;

	public string LocalName
	{
		get
		{
			return _localName;
		}
		set
		{
			_localName = value;
		}
	}

	public string NamespaceName
	{
		get
		{
			return _namespaceName;
		}
		set
		{
			_namespaceName = value;
		}
	}

	public bool PreserveSpace => _preserveSpace;

	protected WhitespaceRule()
	{
	}

	public WhitespaceRule(string localName, string namespaceName, bool preserveSpace)
	{
		Init(localName, namespaceName, preserveSpace);
	}

	protected void Init(string localName, string namespaceName, bool preserveSpace)
	{
		_localName = localName;
		_namespaceName = namespaceName;
		_preserveSpace = preserveSpace;
	}

	public void GetObjectData(XmlQueryDataWriter writer)
	{
		writer.WriteStringQ(_localName);
		writer.WriteStringQ(_namespaceName);
		writer.Write(_preserveSpace);
	}

	public WhitespaceRule(XmlQueryDataReader reader)
	{
		_localName = reader.ReadStringQ();
		_namespaceName = reader.ReadStringQ();
		_preserveSpace = reader.ReadBoolean();
	}
}
