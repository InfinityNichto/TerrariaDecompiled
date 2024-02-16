using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime;

public class ContentType
{
	private readonly TrackingStringDictionary _parameters = new TrackingStringDictionary();

	private string _mediaType;

	private string _subType;

	private bool _isChanged;

	private string _type;

	private bool _isPersisted;

	public string? Boundary
	{
		get
		{
			return Parameters["boundary"];
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				Parameters.Remove("boundary");
			}
			else
			{
				Parameters["boundary"] = value;
			}
		}
	}

	public string? CharSet
	{
		get
		{
			return Parameters["charset"];
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				Parameters.Remove("charset");
			}
			else
			{
				Parameters["charset"] = value;
			}
		}
	}

	public string MediaType
	{
		get
		{
			return _mediaType + "/" + _subType;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length == 0)
			{
				throw new ArgumentException(System.SR.net_emptystringset, "value");
			}
			int offset = 0;
			_mediaType = MailBnfHelper.ReadToken(value, ref offset, null);
			if (_mediaType.Length == 0 || offset >= value.Length || value[offset++] != '/')
			{
				throw new FormatException(System.SR.MediaTypeInvalid);
			}
			_subType = MailBnfHelper.ReadToken(value, ref offset, null);
			if (_subType.Length == 0 || offset < value.Length)
			{
				throw new FormatException(System.SR.MediaTypeInvalid);
			}
			_isChanged = true;
			_isPersisted = false;
		}
	}

	public string Name
	{
		get
		{
			string text = Parameters["name"];
			Encoding encoding = MimeBasePart.DecodeEncoding(text);
			if (encoding != null)
			{
				text = MimeBasePart.DecodeHeaderValue(text);
			}
			return text;
		}
		[param: AllowNull]
		set
		{
			if (value == null || value == string.Empty)
			{
				Parameters.Remove("name");
			}
			else
			{
				Parameters["name"] = value;
			}
		}
	}

	public StringDictionary Parameters => _parameters;

	internal bool IsChanged
	{
		get
		{
			if (!_isChanged)
			{
				if (_parameters != null)
				{
					return _parameters.IsChanged;
				}
				return false;
			}
			return true;
		}
	}

	public ContentType()
		: this("application/octet-stream")
	{
	}

	public ContentType(string contentType)
	{
		if (contentType == null)
		{
			throw new ArgumentNullException("contentType");
		}
		if (contentType.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "contentType"), "contentType");
		}
		_isChanged = true;
		_type = contentType;
		ParseValue();
	}

	internal void PersistIfNeeded(HeaderCollection headers, bool forcePersist)
	{
		if (IsChanged || !_isPersisted || forcePersist)
		{
			headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentType), ToString());
			_isPersisted = true;
		}
	}

	public override string ToString()
	{
		if (_type == null || IsChanged)
		{
			_type = Encode(allowUnicode: false);
			_isChanged = false;
			_parameters.IsChanged = false;
			_isPersisted = false;
		}
		return _type;
	}

	internal string Encode(bool allowUnicode)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_mediaType);
		stringBuilder.Append('/');
		stringBuilder.Append(_subType);
		foreach (string key in Parameters.Keys)
		{
			stringBuilder.Append("; ");
			EncodeToBuffer(key, stringBuilder, allowUnicode);
			stringBuilder.Append('=');
			EncodeToBuffer(_parameters[key], stringBuilder, allowUnicode);
		}
		return stringBuilder.ToString();
	}

	private static void EncodeToBuffer(string value, StringBuilder builder, bool allowUnicode)
	{
		Encoding encoding = MimeBasePart.DecodeEncoding(value);
		if (encoding != null)
		{
			builder.Append('"').Append(value).Append('"');
			return;
		}
		if ((allowUnicode && !MailBnfHelper.HasCROrLF(value)) || MimeBasePart.IsAscii(value, permitCROrLF: false))
		{
			MailBnfHelper.GetTokenOrQuotedString(value, builder, allowUnicode);
			return;
		}
		encoding = Encoding.GetEncoding("utf-8");
		builder.Append('"').Append(MimeBasePart.EncodeHeaderValue(value, encoding, MimeBasePart.ShouldUseBase64Encoding(encoding))).Append('"');
	}

	public override bool Equals([NotNullWhen(true)] object? rparam)
	{
		if (rparam != null)
		{
			return string.Equals(ToString(), rparam.ToString(), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ToString().ToLowerInvariant().GetHashCode();
	}

	[MemberNotNull("_mediaType")]
	[MemberNotNull("_subType")]
	private void ParseValue()
	{
		try
		{
			int offset = 0;
			_mediaType = MailBnfHelper.ReadToken(_type, ref offset, null);
			if (_mediaType == null || _mediaType.Length == 0 || offset >= _type.Length || _type[offset++] != '/')
			{
				throw new FormatException(System.SR.ContentTypeInvalid);
			}
			_subType = MailBnfHelper.ReadToken(_type, ref offset, null);
			if (_subType == null || _subType.Length == 0)
			{
				throw new FormatException(System.SR.ContentTypeInvalid);
			}
			while (MailBnfHelper.SkipCFWS(_type, ref offset))
			{
				if (_type[offset++] != ';')
				{
					throw new FormatException(System.SR.ContentTypeInvalid);
				}
				if (!MailBnfHelper.SkipCFWS(_type, ref offset))
				{
					break;
				}
				string text = MailBnfHelper.ReadParameterAttribute(_type, ref offset, null);
				if (text == null || text.Length == 0)
				{
					throw new FormatException(System.SR.ContentTypeInvalid);
				}
				if (offset >= _type.Length || _type[offset++] != '=')
				{
					throw new FormatException(System.SR.ContentTypeInvalid);
				}
				if (!MailBnfHelper.SkipCFWS(_type, ref offset))
				{
					throw new FormatException(System.SR.ContentTypeInvalid);
				}
				string text2 = ((_type[offset] == '"') ? MailBnfHelper.ReadQuotedString(_type, ref offset, null) : MailBnfHelper.ReadToken(_type, ref offset, null));
				if (text2 == null)
				{
					throw new FormatException(System.SR.ContentTypeInvalid);
				}
				_parameters.Add(text, text2);
			}
			_parameters.IsChanged = false;
		}
		catch (FormatException ex) when (ex.Message != System.SR.ContentTypeInvalid)
		{
			throw new FormatException(System.SR.ContentTypeInvalid);
		}
	}
}
