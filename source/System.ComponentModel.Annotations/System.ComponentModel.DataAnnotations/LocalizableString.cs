using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

internal sealed class LocalizableString
{
	private readonly string _propertyName;

	private Func<string> _cachedResult;

	private string _propertyValue;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type _resourceType;

	public string Value
	{
		get
		{
			return _propertyValue;
		}
		set
		{
			if (_propertyValue != value)
			{
				ClearCache();
				_propertyValue = value;
			}
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public Type ResourceType
	{
		get
		{
			return _resourceType;
		}
		set
		{
			if (_resourceType != value)
			{
				ClearCache();
				_resourceType = value;
			}
		}
	}

	public LocalizableString(string propertyName)
	{
		_propertyName = propertyName;
	}

	private void ClearCache()
	{
		_cachedResult = null;
	}

	public string GetLocalizableValue()
	{
		if (_cachedResult == null)
		{
			if (_propertyValue == null || _resourceType == null)
			{
				_cachedResult = () => _propertyValue;
			}
			else
			{
				PropertyInfo property = _resourceType.GetRuntimeProperty(_propertyValue);
				bool flag = false;
				if (!_resourceType.IsVisible || property == null || property.PropertyType != typeof(string))
				{
					flag = true;
				}
				else
				{
					MethodInfo getMethod = property.GetMethod;
					if (getMethod == null || !getMethod.IsPublic || !getMethod.IsStatic)
					{
						flag = true;
					}
				}
				if (flag)
				{
					string exceptionMessage = System.SR.Format(System.SR.LocalizableString_LocalizationFailed, _propertyName, _resourceType.FullName, _propertyValue);
					_cachedResult = delegate
					{
						throw new InvalidOperationException(exceptionMessage);
					};
				}
				else
				{
					_cachedResult = () => (string)property.GetValue(null, null);
				}
			}
		}
		return _cachedResult();
	}
}
