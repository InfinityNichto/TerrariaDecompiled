using System.Text;

namespace System.Xml.Linq;

public class XDeclaration
{
	private string _version;

	private string _encoding;

	private string _standalone;

	public string? Encoding
	{
		get
		{
			return _encoding;
		}
		set
		{
			_encoding = value;
		}
	}

	public string? Standalone
	{
		get
		{
			return _standalone;
		}
		set
		{
			_standalone = value;
		}
	}

	public string? Version
	{
		get
		{
			return _version;
		}
		set
		{
			_version = value;
		}
	}

	public XDeclaration(string? version, string? encoding, string? standalone)
	{
		_version = version;
		_encoding = encoding;
		_standalone = standalone;
	}

	public XDeclaration(XDeclaration other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		_version = other._version;
		_encoding = other._encoding;
		_standalone = other._standalone;
	}

	internal XDeclaration(XmlReader r)
	{
		_version = r.GetAttribute("version");
		_encoding = r.GetAttribute("encoding");
		_standalone = r.GetAttribute("standalone");
		r.Read();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		stringBuilder.Append("<?xml");
		if (_version != null)
		{
			stringBuilder.Append(" version=\"");
			stringBuilder.Append(_version);
			stringBuilder.Append('"');
		}
		if (_encoding != null)
		{
			stringBuilder.Append(" encoding=\"");
			stringBuilder.Append(_encoding);
			stringBuilder.Append('"');
		}
		if (_standalone != null)
		{
			stringBuilder.Append(" standalone=\"");
			stringBuilder.Append(_standalone);
			stringBuilder.Append('"');
		}
		stringBuilder.Append("?>");
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}
}
