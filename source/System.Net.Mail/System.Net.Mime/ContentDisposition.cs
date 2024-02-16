using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime;

public class ContentDisposition
{
	private TrackingValidationObjectDictionary _parameters;

	private string _disposition;

	private string _dispositionType;

	private bool _isChanged;

	private bool _isPersisted;

	private static readonly TrackingValidationObjectDictionary.ValidateAndParseValue s_dateParser = (object v) => new SmtpDateTime(v.ToString());

	private static readonly TrackingValidationObjectDictionary.ValidateAndParseValue s_longParser = delegate(object value)
	{
		if (!long.TryParse(value.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var result))
		{
			throw new FormatException(System.SR.ContentDispositionInvalid);
		}
		return result;
	};

	private static readonly Dictionary<string, TrackingValidationObjectDictionary.ValidateAndParseValue> s_validators = new Dictionary<string, TrackingValidationObjectDictionary.ValidateAndParseValue>
	{
		{ "creation-date", s_dateParser },
		{ "modification-date", s_dateParser },
		{ "read-date", s_dateParser },
		{ "size", s_longParser }
	};

	public string DispositionType
	{
		get
		{
			return _dispositionType;
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
			_isChanged = true;
			_dispositionType = value;
		}
	}

	public StringDictionary Parameters => _parameters ?? (_parameters = new TrackingValidationObjectDictionary(s_validators));

	public string? FileName
	{
		get
		{
			return Parameters["filename"];
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				Parameters.Remove("filename");
			}
			else
			{
				Parameters["filename"] = value;
			}
		}
	}

	public DateTime CreationDate
	{
		get
		{
			return GetDateParameter("creation-date");
		}
		set
		{
			SmtpDateTime value2 = new SmtpDateTime(value);
			((TrackingValidationObjectDictionary)Parameters).InternalSet("creation-date", value2);
		}
	}

	public DateTime ModificationDate
	{
		get
		{
			return GetDateParameter("modification-date");
		}
		set
		{
			SmtpDateTime value2 = new SmtpDateTime(value);
			((TrackingValidationObjectDictionary)Parameters).InternalSet("modification-date", value2);
		}
	}

	public bool Inline
	{
		get
		{
			return _dispositionType == "inline";
		}
		set
		{
			_isChanged = true;
			_dispositionType = (value ? "inline" : "attachment");
		}
	}

	public DateTime ReadDate
	{
		get
		{
			return GetDateParameter("read-date");
		}
		set
		{
			SmtpDateTime value2 = new SmtpDateTime(value);
			((TrackingValidationObjectDictionary)Parameters).InternalSet("read-date", value2);
		}
	}

	public long Size
	{
		get
		{
			object obj = ((TrackingValidationObjectDictionary)Parameters).InternalGet("size");
			if (obj != null)
			{
				return (long)obj;
			}
			return -1L;
		}
		set
		{
			((TrackingValidationObjectDictionary)Parameters).InternalSet("size", value);
		}
	}

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

	public ContentDisposition()
	{
		_isChanged = true;
		_disposition = (_dispositionType = "attachment");
	}

	public ContentDisposition(string disposition)
	{
		if (disposition == null)
		{
			throw new ArgumentNullException("disposition");
		}
		_isChanged = true;
		_disposition = disposition;
		ParseValue();
	}

	internal DateTime GetDateParameter(string parameterName)
	{
		if (((TrackingValidationObjectDictionary)Parameters).InternalGet(parameterName) is SmtpDateTime smtpDateTime)
		{
			return smtpDateTime.Date;
		}
		return DateTime.MinValue;
	}

	internal void PersistIfNeeded(HeaderCollection headers, bool forcePersist)
	{
		if (IsChanged || !_isPersisted || forcePersist)
		{
			headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition), ToString());
			_isPersisted = true;
		}
	}

	public override string ToString()
	{
		if (_disposition == null || _isChanged || (_parameters != null && _parameters.IsChanged))
		{
			_disposition = Encode(allowUnicode: false);
			_isChanged = false;
			_parameters.IsChanged = false;
			_isPersisted = false;
		}
		return _disposition;
	}

	internal string Encode(bool allowUnicode)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_dispositionType);
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

	[MemberNotNull("_dispositionType")]
	private void ParseValue()
	{
		int offset = 0;
		try
		{
			_dispositionType = MailBnfHelper.ReadToken(_disposition, ref offset, null);
			if (string.IsNullOrEmpty(_dispositionType))
			{
				throw new FormatException(System.SR.MailHeaderFieldMalformedHeader);
			}
			if (_parameters == null)
			{
				_parameters = new TrackingValidationObjectDictionary(s_validators);
			}
			else
			{
				_parameters.Clear();
			}
			while (MailBnfHelper.SkipCFWS(_disposition, ref offset))
			{
				if (_disposition[offset++] != ';')
				{
					throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, _disposition[offset - 1]));
				}
				if (MailBnfHelper.SkipCFWS(_disposition, ref offset))
				{
					string text = MailBnfHelper.ReadParameterAttribute(_disposition, ref offset, null);
					if (_disposition[offset++] != '=')
					{
						throw new FormatException(System.SR.MailHeaderFieldMalformedHeader);
					}
					if (!MailBnfHelper.SkipCFWS(_disposition, ref offset))
					{
						throw new FormatException(System.SR.ContentDispositionInvalid);
					}
					string value = ((_disposition[offset] == '"') ? MailBnfHelper.ReadQuotedString(_disposition, ref offset, null) : MailBnfHelper.ReadToken(_disposition, ref offset, null));
					if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value))
					{
						throw new FormatException(System.SR.ContentDispositionInvalid);
					}
					Parameters.Add(text, value);
					continue;
				}
				break;
			}
		}
		catch (FormatException innerException)
		{
			throw new FormatException(System.SR.ContentDispositionInvalid, innerException);
		}
		_parameters.IsChanged = false;
	}
}
