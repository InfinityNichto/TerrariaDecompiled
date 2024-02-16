using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.Qil;

internal sealed class QilName : QilLiteral
{
	private string _local;

	private string _uri;

	private string _prefix;

	public string LocalName
	{
		get
		{
			return _local;
		}
		[MemberNotNull("_local")]
		set
		{
			_local = value;
		}
	}

	public string NamespaceUri
	{
		get
		{
			return _uri;
		}
		[MemberNotNull("_uri")]
		set
		{
			_uri = value;
		}
	}

	public string Prefix
	{
		get
		{
			return _prefix;
		}
		[MemberNotNull("_prefix")]
		set
		{
			_prefix = value;
		}
	}

	public string QualifiedName
	{
		get
		{
			if (_prefix.Length == 0)
			{
				return _local;
			}
			return _prefix + ":" + _local;
		}
	}

	public QilName(QilNodeType nodeType, string local, string uri, string prefix)
		: base(nodeType, null)
	{
		LocalName = local;
		NamespaceUri = uri;
		Prefix = prefix;
		base.Value = this;
	}

	public override int GetHashCode()
	{
		return _local.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		QilName qilName = other as QilName;
		if (qilName == null)
		{
			return false;
		}
		if (_local == qilName._local)
		{
			return _uri == qilName._uri;
		}
		return false;
	}

	public static bool operator ==(QilName a, QilName b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a._local == b._local)
		{
			return a._uri == b._uri;
		}
		return false;
	}

	public static bool operator !=(QilName a, QilName b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		if (_prefix.Length == 0)
		{
			if (_uri.Length == 0)
			{
				return _local;
			}
			return "{" + _uri + "}" + _local;
		}
		return "{" + _uri + "}" + _prefix + ":" + _local;
	}
}
