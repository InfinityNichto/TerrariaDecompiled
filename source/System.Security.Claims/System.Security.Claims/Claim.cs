using System.Collections.Generic;
using System.IO;

namespace System.Security.Claims;

public class Claim
{
	private enum SerializationMask
	{
		None = 0,
		NameClaimType = 1,
		RoleClaimType = 2,
		StringType = 4,
		Issuer = 8,
		OriginalIssuerEqualsIssuer = 0x10,
		OriginalIssuer = 0x20,
		HasProperties = 0x40,
		UserData = 0x80
	}

	private readonly byte[] _userSerializationData;

	private readonly string _issuer;

	private readonly string _originalIssuer;

	private Dictionary<string, string> _properties;

	private readonly ClaimsIdentity _subject;

	private readonly string _type;

	private readonly string _value;

	private readonly string _valueType;

	protected virtual byte[]? CustomSerializationData => _userSerializationData;

	public string Issuer => _issuer;

	public string OriginalIssuer => _originalIssuer;

	public IDictionary<string, string> Properties
	{
		get
		{
			if (_properties == null)
			{
				_properties = new Dictionary<string, string>();
			}
			return _properties;
		}
	}

	public ClaimsIdentity? Subject => _subject;

	public string Type => _type;

	public string Value => _value;

	public string ValueType => _valueType;

	public Claim(BinaryReader reader)
		: this(reader, null)
	{
	}

	public Claim(BinaryReader reader, ClaimsIdentity? subject)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		_subject = subject;
		SerializationMask serializationMask = (SerializationMask)reader.ReadInt32();
		int num = 1;
		int num2 = reader.ReadInt32();
		_value = reader.ReadString();
		if ((serializationMask & SerializationMask.NameClaimType) == SerializationMask.NameClaimType)
		{
			_type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
		}
		else if ((serializationMask & SerializationMask.RoleClaimType) == SerializationMask.RoleClaimType)
		{
			_type = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
		}
		else
		{
			_type = reader.ReadString();
			num++;
		}
		if ((serializationMask & SerializationMask.StringType) == SerializationMask.StringType)
		{
			_valueType = reader.ReadString();
			num++;
		}
		else
		{
			_valueType = "http://www.w3.org/2001/XMLSchema#string";
		}
		if ((serializationMask & SerializationMask.Issuer) == SerializationMask.Issuer)
		{
			_issuer = reader.ReadString();
			num++;
		}
		else
		{
			_issuer = "LOCAL AUTHORITY";
		}
		if ((serializationMask & SerializationMask.OriginalIssuerEqualsIssuer) == SerializationMask.OriginalIssuerEqualsIssuer)
		{
			_originalIssuer = _issuer;
		}
		else if ((serializationMask & SerializationMask.OriginalIssuer) == SerializationMask.OriginalIssuer)
		{
			_originalIssuer = reader.ReadString();
			num++;
		}
		else
		{
			_originalIssuer = "LOCAL AUTHORITY";
		}
		if ((serializationMask & SerializationMask.HasProperties) == SerializationMask.HasProperties)
		{
			int num3 = reader.ReadInt32();
			num++;
			for (int i = 0; i < num3; i++)
			{
				Properties.Add(reader.ReadString(), reader.ReadString());
			}
		}
		if ((serializationMask & SerializationMask.UserData) == SerializationMask.UserData)
		{
			int count = reader.ReadInt32();
			_userSerializationData = reader.ReadBytes(count);
			num++;
		}
		for (int j = num; j < num2; j++)
		{
			reader.ReadString();
		}
	}

	public Claim(string type, string value)
		: this(type, value, "http://www.w3.org/2001/XMLSchema#string", "LOCAL AUTHORITY", "LOCAL AUTHORITY", null)
	{
	}

	public Claim(string type, string value, string? valueType)
		: this(type, value, valueType, "LOCAL AUTHORITY", "LOCAL AUTHORITY", null)
	{
	}

	public Claim(string type, string value, string? valueType, string? issuer)
		: this(type, value, valueType, issuer, issuer, null)
	{
	}

	public Claim(string type, string value, string? valueType, string? issuer, string? originalIssuer)
		: this(type, value, valueType, issuer, originalIssuer, null)
	{
	}

	public Claim(string type, string value, string? valueType, string? issuer, string? originalIssuer, ClaimsIdentity? subject)
		: this(type, value, valueType, issuer, originalIssuer, subject, null, null)
	{
	}

	internal Claim(string type, string value, string valueType, string issuer, string originalIssuer, ClaimsIdentity subject, string propertyKey, string propertyValue)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		_type = type;
		_value = value;
		_valueType = (string.IsNullOrEmpty(valueType) ? "http://www.w3.org/2001/XMLSchema#string" : valueType);
		_issuer = (string.IsNullOrEmpty(issuer) ? "LOCAL AUTHORITY" : issuer);
		_originalIssuer = (string.IsNullOrEmpty(originalIssuer) ? _issuer : originalIssuer);
		_subject = subject;
		if (propertyKey != null)
		{
			_properties = new Dictionary<string, string>();
			_properties[propertyKey] = propertyValue;
		}
	}

	protected Claim(Claim other)
		: this(other, other?._subject)
	{
	}

	protected Claim(Claim other, ClaimsIdentity? subject)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		_issuer = other._issuer;
		_originalIssuer = other._originalIssuer;
		_subject = subject;
		_type = other._type;
		_value = other._value;
		_valueType = other._valueType;
		if (other._properties != null)
		{
			_properties = new Dictionary<string, string>(other._properties);
		}
		if (other._userSerializationData != null)
		{
			_userSerializationData = other._userSerializationData.Clone() as byte[];
		}
	}

	public virtual Claim Clone()
	{
		return Clone(null);
	}

	public virtual Claim Clone(ClaimsIdentity? identity)
	{
		return new Claim(this, identity);
	}

	public virtual void WriteTo(BinaryWriter writer)
	{
		WriteTo(writer, null);
	}

	protected virtual void WriteTo(BinaryWriter writer, byte[]? userData)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		int num = 1;
		SerializationMask serializationMask = SerializationMask.None;
		if (string.Equals(_type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"))
		{
			serializationMask |= SerializationMask.NameClaimType;
		}
		else if (string.Equals(_type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
		{
			serializationMask |= SerializationMask.RoleClaimType;
		}
		else
		{
			num++;
		}
		if (!string.Equals(_valueType, "http://www.w3.org/2001/XMLSchema#string", StringComparison.Ordinal))
		{
			num++;
			serializationMask |= SerializationMask.StringType;
		}
		if (!string.Equals(_issuer, "LOCAL AUTHORITY", StringComparison.Ordinal))
		{
			num++;
			serializationMask |= SerializationMask.Issuer;
		}
		if (string.Equals(_originalIssuer, _issuer, StringComparison.Ordinal))
		{
			serializationMask |= SerializationMask.OriginalIssuerEqualsIssuer;
		}
		else if (!string.Equals(_originalIssuer, "LOCAL AUTHORITY"))
		{
			num++;
			serializationMask |= SerializationMask.OriginalIssuer;
		}
		if (_properties != null && _properties.Count > 0)
		{
			num++;
			serializationMask |= SerializationMask.HasProperties;
		}
		if (userData != null && userData.Length != 0)
		{
			num++;
			serializationMask |= SerializationMask.UserData;
		}
		writer.Write((int)serializationMask);
		writer.Write(num);
		writer.Write(_value);
		if ((serializationMask & SerializationMask.NameClaimType) != SerializationMask.NameClaimType && (serializationMask & SerializationMask.RoleClaimType) != SerializationMask.RoleClaimType)
		{
			writer.Write(_type);
		}
		if ((serializationMask & SerializationMask.StringType) == SerializationMask.StringType)
		{
			writer.Write(_valueType);
		}
		if ((serializationMask & SerializationMask.Issuer) == SerializationMask.Issuer)
		{
			writer.Write(_issuer);
		}
		if ((serializationMask & SerializationMask.OriginalIssuer) == SerializationMask.OriginalIssuer)
		{
			writer.Write(_originalIssuer);
		}
		if ((serializationMask & SerializationMask.HasProperties) == SerializationMask.HasProperties)
		{
			writer.Write(_properties.Count);
			foreach (KeyValuePair<string, string> property in _properties)
			{
				writer.Write(property.Key);
				writer.Write(property.Value);
			}
		}
		if ((serializationMask & SerializationMask.UserData) == SerializationMask.UserData)
		{
			writer.Write(userData.Length);
			writer.Write(userData);
		}
		writer.Flush();
	}

	public override string ToString()
	{
		return _type + ": " + _value;
	}
}
