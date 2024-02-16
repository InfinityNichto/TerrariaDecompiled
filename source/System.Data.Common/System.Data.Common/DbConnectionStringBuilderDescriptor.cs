using System.ComponentModel;

namespace System.Data.Common;

internal sealed class DbConnectionStringBuilderDescriptor : PropertyDescriptor
{
	internal bool RefreshOnChange { get; }

	public override Type ComponentType { get; }

	public override bool IsReadOnly { get; }

	public override Type PropertyType { get; }

	internal DbConnectionStringBuilderDescriptor(string propertyName, Type componentType, Type propertyType, bool isReadOnly, Attribute[] attributes)
		: base(propertyName, attributes)
	{
		ComponentType = componentType;
		PropertyType = propertyType;
		IsReadOnly = isReadOnly;
	}

	public override bool CanResetValue(object component)
	{
		if (component is DbConnectionStringBuilder dbConnectionStringBuilder)
		{
			return dbConnectionStringBuilder.ShouldSerialize(DisplayName);
		}
		return false;
	}

	public override object GetValue(object component)
	{
		if (component is DbConnectionStringBuilder dbConnectionStringBuilder && dbConnectionStringBuilder.TryGetValue(DisplayName, out object value))
		{
			return value;
		}
		return null;
	}

	public override void ResetValue(object component)
	{
		if (component is DbConnectionStringBuilder dbConnectionStringBuilder)
		{
			dbConnectionStringBuilder.Remove(DisplayName);
			if (RefreshOnChange)
			{
				dbConnectionStringBuilder.ClearPropertyDescriptors();
			}
		}
	}

	public override void SetValue(object component, object value)
	{
		if (component is DbConnectionStringBuilder dbConnectionStringBuilder)
		{
			if (typeof(string) == PropertyType && string.Empty.Equals(value))
			{
				value = null;
			}
			dbConnectionStringBuilder[DisplayName] = value;
			if (RefreshOnChange)
			{
				dbConnectionStringBuilder.ClearPropertyDescriptors();
			}
		}
	}

	public override bool ShouldSerializeValue(object component)
	{
		if (component is DbConnectionStringBuilder dbConnectionStringBuilder)
		{
			return dbConnectionStringBuilder.ShouldSerialize(DisplayName);
		}
		return false;
	}
}
