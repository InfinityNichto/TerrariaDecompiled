using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DataObjectMethodAttribute : Attribute
{
	public bool IsDefault { get; }

	public DataObjectMethodType MethodType { get; }

	public DataObjectMethodAttribute(DataObjectMethodType methodType)
		: this(methodType, isDefault: false)
	{
	}

	public DataObjectMethodAttribute(DataObjectMethodType methodType, bool isDefault)
	{
		MethodType = methodType;
		IsDefault = isDefault;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is DataObjectMethodAttribute dataObjectMethodAttribute && dataObjectMethodAttribute.MethodType == MethodType)
		{
			return dataObjectMethodAttribute.IsDefault == IsDefault;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((int)MethodType).GetHashCode() ^ IsDefault.GetHashCode();
	}

	public override bool Match([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is DataObjectMethodAttribute dataObjectMethodAttribute)
		{
			return dataObjectMethodAttribute.MethodType == MethodType;
		}
		return false;
	}
}
