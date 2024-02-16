using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

public abstract class TypeDescriptionProvider
{
	private sealed class EmptyCustomTypeDescriptor : CustomTypeDescriptor
	{
	}

	private readonly TypeDescriptionProvider _parent;

	private EmptyCustomTypeDescriptor _emptyDescriptor;

	protected TypeDescriptionProvider()
	{
	}

	protected TypeDescriptionProvider(TypeDescriptionProvider parent)
	{
		_parent = parent;
	}

	public virtual object? CreateInstance(IServiceProvider? provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType, Type[]? argTypes, object[]? args)
	{
		if (_parent != null)
		{
			return _parent.CreateInstance(provider, objectType, argTypes, args);
		}
		if (objectType == null)
		{
			throw new ArgumentNullException("objectType");
		}
		return Activator.CreateInstance(objectType, args);
	}

	public virtual IDictionary? GetCache(object instance)
	{
		return _parent?.GetCache(instance);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public virtual ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
	{
		if (_parent != null)
		{
			return _parent.GetExtendedTypeDescriptor(instance);
		}
		return _emptyDescriptor ?? (_emptyDescriptor = new EmptyCustomTypeDescriptor());
	}

	protected internal virtual IExtenderProvider[] GetExtenderProviders(object instance)
	{
		if (_parent != null)
		{
			return _parent.GetExtenderProviders(instance);
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return Array.Empty<IExtenderProvider>();
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public virtual string? GetFullComponentName(object component)
	{
		if (_parent != null)
		{
			return _parent.GetFullComponentName(component);
		}
		return GetTypeDescriptor(component)?.GetComponentName();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)]
	public Type GetReflectionType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type objectType)
	{
		return GetReflectionType(objectType, null);
	}

	[RequiresUnreferencedCode("GetReflectionType is not trim compatible because the Type of object cannot be statically discovered.")]
	public Type GetReflectionType(object instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return GetReflectionType(instance.GetType(), instance);
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)]
	public virtual Type GetReflectionType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type objectType, object? instance)
	{
		if (_parent != null)
		{
			return _parent.GetReflectionType(objectType, instance);
		}
		return objectType;
	}

	public virtual Type GetRuntimeType(Type reflectionType)
	{
		if (_parent != null)
		{
			return _parent.GetRuntimeType(reflectionType);
		}
		if (reflectionType == null)
		{
			throw new ArgumentNullException("reflectionType");
		}
		if (reflectionType.GetType().Assembly == typeof(object).Assembly)
		{
			return reflectionType;
		}
		return reflectionType.UnderlyingSystemType;
	}

	public ICustomTypeDescriptor? GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType)
	{
		return GetTypeDescriptor(objectType, null);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public ICustomTypeDescriptor? GetTypeDescriptor(object instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return GetTypeDescriptor(instance.GetType(), instance);
	}

	public virtual ICustomTypeDescriptor? GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance)
	{
		if (_parent != null)
		{
			return _parent.GetTypeDescriptor(objectType, instance);
		}
		return _emptyDescriptor ?? (_emptyDescriptor = new EmptyCustomTypeDescriptor());
	}

	public virtual bool IsSupportedType(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (_parent != null)
		{
			return _parent.IsSupportedType(type);
		}
		return true;
	}
}
