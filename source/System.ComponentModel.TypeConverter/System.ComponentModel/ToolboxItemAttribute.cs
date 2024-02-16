using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public class ToolboxItemAttribute : Attribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private Type _toolboxItemType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private readonly string _toolboxItemTypeName;

	public static readonly ToolboxItemAttribute Default = new ToolboxItemAttribute("System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

	public static readonly ToolboxItemAttribute None = new ToolboxItemAttribute(defaultType: false);

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public Type? ToolboxItemType
	{
		get
		{
			if (_toolboxItemType == null && _toolboxItemTypeName != null)
			{
				try
				{
					_toolboxItemType = Type.GetType(_toolboxItemTypeName, throwOnError: true);
				}
				catch (Exception innerException)
				{
					throw new ArgumentException(System.SR.Format(System.SR.ToolboxItemAttributeFailedGetType, _toolboxItemTypeName), innerException);
				}
			}
			return _toolboxItemType;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public string ToolboxItemTypeName => _toolboxItemTypeName ?? string.Empty;

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}

	public ToolboxItemAttribute(bool defaultType)
	{
		if (defaultType)
		{
			_toolboxItemTypeName = "System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
		}
	}

	public ToolboxItemAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string toolboxItemTypeName)
	{
		_toolboxItemTypeName = toolboxItemTypeName ?? throw new ArgumentNullException("toolboxItemTypeName");
	}

	public ToolboxItemAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type toolboxItemType)
	{
		if (toolboxItemType == null)
		{
			throw new ArgumentNullException("toolboxItemType");
		}
		_toolboxItemType = toolboxItemType;
		_toolboxItemTypeName = toolboxItemType.AssemblyQualifiedName;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is ToolboxItemAttribute toolboxItemAttribute)
		{
			return toolboxItemAttribute.ToolboxItemTypeName == ToolboxItemTypeName;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (_toolboxItemTypeName != null)
		{
			return _toolboxItemTypeName.GetHashCode();
		}
		return base.GetHashCode();
	}
}
