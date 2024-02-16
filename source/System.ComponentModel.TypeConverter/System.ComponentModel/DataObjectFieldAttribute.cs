using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DataObjectFieldAttribute : Attribute
{
	public bool IsIdentity { get; }

	public bool IsNullable { get; }

	public int Length { get; }

	public bool PrimaryKey { get; }

	public DataObjectFieldAttribute(bool primaryKey)
		: this(primaryKey, isIdentity: false, isNullable: false, -1)
	{
	}

	public DataObjectFieldAttribute(bool primaryKey, bool isIdentity)
		: this(primaryKey, isIdentity, isNullable: false, -1)
	{
	}

	public DataObjectFieldAttribute(bool primaryKey, bool isIdentity, bool isNullable)
		: this(primaryKey, isIdentity, isNullable, -1)
	{
	}

	public DataObjectFieldAttribute(bool primaryKey, bool isIdentity, bool isNullable, int length)
	{
		PrimaryKey = primaryKey;
		IsIdentity = isIdentity;
		IsNullable = isNullable;
		Length = length;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is DataObjectFieldAttribute dataObjectFieldAttribute && dataObjectFieldAttribute.IsIdentity == IsIdentity && dataObjectFieldAttribute.IsNullable == IsNullable && dataObjectFieldAttribute.Length == Length)
		{
			return dataObjectFieldAttribute.PrimaryKey == PrimaryKey;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
