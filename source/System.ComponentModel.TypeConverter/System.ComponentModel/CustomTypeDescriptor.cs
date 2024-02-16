using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

public abstract class CustomTypeDescriptor : ICustomTypeDescriptor
{
	private readonly ICustomTypeDescriptor _parent;

	protected CustomTypeDescriptor()
	{
	}

	protected CustomTypeDescriptor(ICustomTypeDescriptor? parent)
	{
		_parent = parent;
	}

	public virtual AttributeCollection GetAttributes()
	{
		if (_parent != null)
		{
			return _parent.GetAttributes();
		}
		return AttributeCollection.Empty;
	}

	public virtual string? GetClassName()
	{
		return _parent?.GetClassName();
	}

	public virtual string? GetComponentName()
	{
		return _parent?.GetComponentName();
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	public virtual TypeConverter GetConverter()
	{
		if (_parent != null)
		{
			return _parent.GetConverter();
		}
		return new TypeConverter();
	}

	[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
	public virtual EventDescriptor? GetDefaultEvent()
	{
		return _parent?.GetDefaultEvent();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public virtual PropertyDescriptor? GetDefaultProperty()
	{
		return _parent?.GetDefaultProperty();
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
	public virtual object? GetEditor(Type editorBaseType)
	{
		return _parent?.GetEditor(editorBaseType);
	}

	public virtual EventDescriptorCollection GetEvents()
	{
		if (_parent != null)
		{
			return _parent.GetEvents();
		}
		return EventDescriptorCollection.Empty;
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public virtual EventDescriptorCollection GetEvents(Attribute[]? attributes)
	{
		if (_parent != null)
		{
			return _parent.GetEvents(attributes);
		}
		return EventDescriptorCollection.Empty;
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public virtual PropertyDescriptorCollection GetProperties()
	{
		if (_parent != null)
		{
			return _parent.GetProperties();
		}
		return PropertyDescriptorCollection.Empty;
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public virtual PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
	{
		if (_parent != null)
		{
			return _parent.GetProperties(attributes);
		}
		return PropertyDescriptorCollection.Empty;
	}

	public virtual object? GetPropertyOwner(PropertyDescriptor? pd)
	{
		return _parent?.GetPropertyOwner(pd);
	}
}
