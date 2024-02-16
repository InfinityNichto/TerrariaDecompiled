using Internal.Cryptography;

namespace System.Security.Cryptography;

public sealed class Oid
{
	private string _value;

	private string _friendlyName;

	private bool _hasInitializedFriendlyName;

	private readonly OidGroup _group;

	public string? Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (_value != null && !_value.Equals(value, StringComparison.Ordinal))
			{
				throw new PlatformNotSupportedException(System.SR.Cryptography_Oid_SetOnceValue);
			}
			_value = value;
		}
	}

	public string? FriendlyName
	{
		get
		{
			if (!_hasInitializedFriendlyName && _value != null)
			{
				_friendlyName = OidLookup.ToFriendlyName(_value, _group, fallBackToAllGroups: true);
				_hasInitializedFriendlyName = true;
			}
			return _friendlyName;
		}
		set
		{
			if (_hasInitializedFriendlyName)
			{
				if ((_friendlyName != null && !_friendlyName.Equals(value, StringComparison.Ordinal)) || (_friendlyName == null && value != null))
				{
					throw new PlatformNotSupportedException(System.SR.Cryptography_Oid_SetOnceFriendlyName);
				}
				return;
			}
			if (value != null)
			{
				string text = OidLookup.ToOid(value, _group, fallBackToAllGroups: true);
				if (text != null)
				{
					if (_value == null)
					{
						_value = text;
					}
					else if (!_value.Equals(text, StringComparison.Ordinal))
					{
						throw new PlatformNotSupportedException(System.SR.Cryptography_Oid_SetOnceValue);
					}
				}
			}
			_friendlyName = value;
			_hasInitializedFriendlyName = true;
		}
	}

	public Oid()
	{
	}

	public Oid(string oid)
	{
		string text = OidLookup.ToOid(oid, OidGroup.All, fallBackToAllGroups: false);
		if (text == null)
		{
			text = oid;
		}
		Value = text;
		_group = OidGroup.All;
	}

	public Oid(string? value, string? friendlyName)
	{
		_value = value;
		_friendlyName = friendlyName;
		_hasInitializedFriendlyName = friendlyName != null;
	}

	public Oid(Oid oid)
	{
		if (oid == null)
		{
			throw new ArgumentNullException("oid");
		}
		_value = oid._value;
		_friendlyName = oid._friendlyName;
		_group = oid._group;
		_hasInitializedFriendlyName = oid._hasInitializedFriendlyName;
	}

	public static Oid FromFriendlyName(string friendlyName, OidGroup group)
	{
		if (friendlyName == null)
		{
			throw new ArgumentNullException("friendlyName");
		}
		string text = OidLookup.ToOid(friendlyName, group, fallBackToAllGroups: false);
		if (text == null)
		{
			throw new CryptographicException(System.SR.Cryptography_Oid_InvalidName);
		}
		return new Oid(text, friendlyName, group);
	}

	public static Oid FromOidValue(string oidValue, OidGroup group)
	{
		if (oidValue == null)
		{
			throw new ArgumentNullException("oidValue");
		}
		string text = OidLookup.ToFriendlyName(oidValue, group, fallBackToAllGroups: false);
		if (text == null)
		{
			throw new CryptographicException(System.SR.Cryptography_Oid_InvalidValue);
		}
		return new Oid(oidValue, text, group);
	}

	private Oid(string value, string friendlyName, OidGroup group)
	{
		_value = value;
		_friendlyName = friendlyName;
		_group = group;
		_hasInitializedFriendlyName = true;
	}
}
