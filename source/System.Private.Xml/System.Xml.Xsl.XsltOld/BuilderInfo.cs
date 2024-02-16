using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml.Xsl.XsltOld;

internal sealed class BuilderInfo
{
	private string _name;

	private string _localName;

	private string _namespaceURI;

	private string _prefix;

	private XmlNodeType _nodeType;

	private int _depth;

	private bool _isEmptyTag;

	internal string[] TextInfo = new string[4];

	internal int TextInfoCount;

	internal bool search;

	internal HtmlElementProps htmlProps;

	internal HtmlAttributeProps htmlAttrProps;

	internal string Name
	{
		get
		{
			if (_name == null)
			{
				string prefix = Prefix;
				string localName = LocalName;
				if (prefix != null && 0 < prefix.Length)
				{
					if (localName.Length > 0)
					{
						_name = prefix + ":" + localName;
					}
					else
					{
						_name = prefix;
					}
				}
				else
				{
					_name = localName;
				}
			}
			return _name;
		}
	}

	internal string LocalName
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

	internal string NamespaceURI
	{
		get
		{
			return _namespaceURI;
		}
		set
		{
			_namespaceURI = value;
		}
	}

	internal string Prefix
	{
		get
		{
			return _prefix;
		}
		set
		{
			_prefix = value;
		}
	}

	internal string Value
	{
		get
		{
			switch (TextInfoCount)
			{
			case 0:
				return string.Empty;
			case 1:
				return TextInfo[0];
			default:
			{
				int num = 0;
				for (int i = 0; i < TextInfoCount; i++)
				{
					string text = TextInfo[i];
					if (text != null)
					{
						num += text.Length;
					}
				}
				StringBuilder stringBuilder = new StringBuilder(num);
				for (int j = 0; j < TextInfoCount; j++)
				{
					string text2 = TextInfo[j];
					if (text2 != null)
					{
						stringBuilder.Append(text2);
					}
				}
				return stringBuilder.ToString();
			}
			}
		}
		set
		{
			TextInfoCount = 0;
			ValueAppend(value, disableEscaping: false);
		}
	}

	internal XmlNodeType NodeType
	{
		get
		{
			return _nodeType;
		}
		set
		{
			_nodeType = value;
		}
	}

	internal int Depth
	{
		get
		{
			return _depth;
		}
		set
		{
			_depth = value;
		}
	}

	internal bool IsEmptyTag
	{
		get
		{
			return _isEmptyTag;
		}
		set
		{
			_isEmptyTag = value;
		}
	}

	internal BuilderInfo()
	{
		Initialize(string.Empty, string.Empty, string.Empty);
	}

	[MemberNotNull("_prefix")]
	[MemberNotNull("_localName")]
	[MemberNotNull("_namespaceURI")]
	internal void Initialize(string prefix, string name, string nspace)
	{
		_prefix = prefix;
		_localName = name;
		_namespaceURI = nspace;
		_name = null;
		htmlProps = null;
		htmlAttrProps = null;
		TextInfoCount = 0;
	}

	[MemberNotNull("_prefix")]
	[MemberNotNull("_localName")]
	[MemberNotNull("_namespaceURI")]
	internal void Initialize(BuilderInfo src)
	{
		_prefix = src.Prefix;
		_localName = src.LocalName;
		_namespaceURI = src.NamespaceURI;
		_name = null;
		_depth = src.Depth;
		_nodeType = src.NodeType;
		htmlProps = src.htmlProps;
		htmlAttrProps = src.htmlAttrProps;
		TextInfoCount = 0;
		EnsureTextInfoSize(src.TextInfoCount);
		src.TextInfo.CopyTo(TextInfo, 0);
		TextInfoCount = src.TextInfoCount;
	}

	private void EnsureTextInfoSize(int newSize)
	{
		if (TextInfo.Length < newSize)
		{
			string[] array = new string[newSize * 2];
			Array.Copy(TextInfo, array, TextInfoCount);
			TextInfo = array;
		}
	}

	internal BuilderInfo Clone()
	{
		BuilderInfo builderInfo = new BuilderInfo();
		builderInfo.Initialize(this);
		return builderInfo;
	}

	internal void ValueAppend(string s, bool disableEscaping)
	{
		if (s != null && s.Length != 0)
		{
			EnsureTextInfoSize(TextInfoCount + ((!disableEscaping) ? 1 : 2));
			if (disableEscaping)
			{
				TextInfo[TextInfoCount++] = null;
			}
			TextInfo[TextInfoCount++] = s;
		}
	}
}
