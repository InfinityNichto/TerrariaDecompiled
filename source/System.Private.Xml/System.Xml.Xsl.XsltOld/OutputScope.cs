using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.XsltOld;

internal sealed class OutputScope : DocumentScope
{
	private string _name;

	private string _nsUri;

	private string _prefix;

	private XmlSpace _space;

	private string _lang;

	private bool _mixed;

	private bool _toCData;

	private HtmlElementProps _htmlElementProps;

	internal string Name => _name;

	internal string Namespace => _nsUri;

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

	internal XmlSpace Space
	{
		get
		{
			return _space;
		}
		set
		{
			_space = value;
		}
	}

	internal string Lang
	{
		get
		{
			return _lang;
		}
		set
		{
			_lang = value;
		}
	}

	internal bool Mixed
	{
		get
		{
			return _mixed;
		}
		set
		{
			_mixed = value;
		}
	}

	internal bool ToCData
	{
		get
		{
			return _toCData;
		}
		set
		{
			_toCData = value;
		}
	}

	internal HtmlElementProps HtmlElementProps
	{
		get
		{
			return _htmlElementProps;
		}
		set
		{
			_htmlElementProps = value;
		}
	}

	internal OutputScope()
	{
		Init(string.Empty, string.Empty, string.Empty, XmlSpace.None, string.Empty, mixed: false);
	}

	[MemberNotNull("_name")]
	[MemberNotNull("_nsUri")]
	[MemberNotNull("_prefix")]
	[MemberNotNull("_lang")]
	internal void Init(string name, string nspace, string prefix, XmlSpace space, string lang, bool mixed)
	{
		scopes = null;
		_name = name;
		_nsUri = nspace;
		_prefix = prefix;
		_space = space;
		_lang = lang;
		_mixed = mixed;
		_toCData = false;
		_htmlElementProps = null;
	}

	internal bool FindPrefix(string urn, out string prefix)
	{
		for (NamespaceDecl next = scopes; next != null; next = next.Next)
		{
			if (Ref.Equal(next.Uri, urn) && next.Prefix != null && next.Prefix.Length > 0)
			{
				prefix = next.Prefix;
				return true;
			}
		}
		prefix = string.Empty;
		return false;
	}
}
