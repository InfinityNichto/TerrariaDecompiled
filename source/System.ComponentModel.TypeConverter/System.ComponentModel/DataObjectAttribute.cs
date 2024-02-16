using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DataObjectAttribute : Attribute
{
	public static readonly DataObjectAttribute DataObject = new DataObjectAttribute(isDataObject: true);

	public static readonly DataObjectAttribute NonDataObject = new DataObjectAttribute(isDataObject: false);

	public static readonly DataObjectAttribute Default = NonDataObject;

	public bool IsDataObject { get; }

	public DataObjectAttribute()
		: this(isDataObject: true)
	{
	}

	public DataObjectAttribute(bool isDataObject)
	{
		IsDataObject = isDataObject;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is DataObjectAttribute dataObjectAttribute)
		{
			return dataObjectAttribute.IsDataObject == IsDataObject;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return IsDataObject.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}
}
