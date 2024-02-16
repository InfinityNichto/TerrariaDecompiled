using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml;

public class XmlDeclaration : XmlLinkedNode
{
	private string _version;

	private string _encoding;

	private string _standalone;

	public string Version
	{
		get
		{
			return _version;
		}
		[MemberNotNull("_version")]
		internal set
		{
			_version = value;
		}
	}

	public string Encoding
	{
		get
		{
			return _encoding;
		}
		[MemberNotNull("_encoding")]
		[param: AllowNull]
		set
		{
			_encoding = ((value == null) ? string.Empty : value);
		}
	}

	public string Standalone
	{
		get
		{
			return _standalone;
		}
		[MemberNotNull("_standalone")]
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				_standalone = string.Empty;
				return;
			}
			if (value.Length == 0 || value == "yes" || value == "no")
			{
				_standalone = value;
				return;
			}
			throw new ArgumentException(System.SR.Format(System.SR.Xdom_standalone, value));
		}
	}

	public override string? Value
	{
		get
		{
			return InnerText;
		}
		set
		{
			InnerText = value;
		}
	}

	public override string InnerText
	{
		get
		{
			StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
			stringBuilder.Append("version=\"");
			stringBuilder.Append(Version);
			stringBuilder.Append('"');
			if (Encoding.Length > 0)
			{
				stringBuilder.Append(" encoding=\"");
				stringBuilder.Append(Encoding);
				stringBuilder.Append('"');
			}
			if (Standalone.Length > 0)
			{
				stringBuilder.Append(" standalone=\"");
				stringBuilder.Append(Standalone);
				stringBuilder.Append('"');
			}
			return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
		}
		set
		{
			string version = null;
			string encoding = null;
			string standalone = null;
			string encoding2 = Encoding;
			string standalone2 = Standalone;
			string version2 = Version;
			XmlLoader.ParseXmlDeclarationValue(value, out version, out encoding, out standalone);
			try
			{
				if (version != null && !IsValidXmlVersion(version))
				{
					throw new ArgumentException(System.SR.Xdom_Version);
				}
				Version = version;
				if (encoding != null)
				{
					Encoding = encoding;
				}
				if (standalone != null)
				{
					Standalone = standalone;
				}
			}
			catch
			{
				Encoding = encoding2;
				Standalone = standalone2;
				Version = version2;
				throw;
			}
		}
	}

	public override string Name => "xml";

	public override string LocalName => Name;

	public override XmlNodeType NodeType => XmlNodeType.XmlDeclaration;

	protected internal XmlDeclaration(string version, string? encoding, string? standalone, XmlDocument doc)
		: base(doc)
	{
		if (!IsValidXmlVersion(version))
		{
			throw new ArgumentException(System.SR.Xdom_Version);
		}
		if (standalone != null && standalone.Length > 0 && standalone != "yes" && standalone != "no")
		{
			throw new ArgumentException(System.SR.Format(System.SR.Xdom_standalone, standalone));
		}
		Encoding = encoding;
		Standalone = standalone;
		Version = version;
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateXmlDeclaration(Version, Encoding, Standalone);
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteProcessingInstruction(Name, InnerText);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}

	private bool IsValidXmlVersion(string ver)
	{
		if (ver.Length >= 3 && ver[0] == '1' && ver[1] == '.')
		{
			return XmlCharType.IsOnlyDigits(ver, 2, ver.Length - 2);
		}
		return false;
	}
}
