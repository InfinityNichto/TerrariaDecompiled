using System.ComponentModel;
using System.Reflection;

namespace System.Xml.Serialization;

public class SoapAttributes
{
	private bool _soapIgnore;

	private SoapTypeAttribute _soapType;

	private SoapElementAttribute _soapElement;

	private SoapAttributeAttribute _soapAttribute;

	private SoapEnumAttribute _soapEnum;

	private object _soapDefaultValue;

	internal SoapAttributeFlags SoapFlags
	{
		get
		{
			SoapAttributeFlags soapAttributeFlags = (SoapAttributeFlags)0;
			if (_soapElement != null)
			{
				soapAttributeFlags |= SoapAttributeFlags.Element;
			}
			if (_soapAttribute != null)
			{
				soapAttributeFlags |= SoapAttributeFlags.Attribute;
			}
			if (_soapEnum != null)
			{
				soapAttributeFlags |= SoapAttributeFlags.Enum;
			}
			if (_soapType != null)
			{
				soapAttributeFlags |= SoapAttributeFlags.Type;
			}
			return soapAttributeFlags;
		}
	}

	public SoapTypeAttribute? SoapType
	{
		get
		{
			return _soapType;
		}
		set
		{
			_soapType = value;
		}
	}

	public SoapEnumAttribute? SoapEnum
	{
		get
		{
			return _soapEnum;
		}
		set
		{
			_soapEnum = value;
		}
	}

	public bool SoapIgnore
	{
		get
		{
			return _soapIgnore;
		}
		set
		{
			_soapIgnore = value;
		}
	}

	public SoapElementAttribute? SoapElement
	{
		get
		{
			return _soapElement;
		}
		set
		{
			_soapElement = value;
		}
	}

	public SoapAttributeAttribute? SoapAttribute
	{
		get
		{
			return _soapAttribute;
		}
		set
		{
			_soapAttribute = value;
		}
	}

	public object? SoapDefaultValue
	{
		get
		{
			return _soapDefaultValue;
		}
		set
		{
			_soapDefaultValue = value;
		}
	}

	public SoapAttributes()
	{
	}

	public SoapAttributes(ICustomAttributeProvider provider)
	{
		object[] customAttributes = provider.GetCustomAttributes(inherit: false);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			if (customAttributes[i] is SoapIgnoreAttribute || customAttributes[i] is ObsoleteAttribute)
			{
				_soapIgnore = true;
				break;
			}
			if (customAttributes[i] is SoapElementAttribute)
			{
				_soapElement = (SoapElementAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is SoapAttributeAttribute)
			{
				_soapAttribute = (SoapAttributeAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is SoapTypeAttribute)
			{
				_soapType = (SoapTypeAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is SoapEnumAttribute)
			{
				_soapEnum = (SoapEnumAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is DefaultValueAttribute)
			{
				_soapDefaultValue = ((DefaultValueAttribute)customAttributes[i]).Value;
			}
		}
		if (_soapIgnore)
		{
			_soapElement = null;
			_soapAttribute = null;
			_soapType = null;
			_soapEnum = null;
			_soapDefaultValue = null;
		}
	}

	internal SoapAttributeFlags GetSoapFlags()
	{
		return SoapFlags;
	}
}
