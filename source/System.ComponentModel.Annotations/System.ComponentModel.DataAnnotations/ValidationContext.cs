using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations;

public sealed class ValidationContext : IServiceProvider
{
	private readonly Dictionary<object, object> _items;

	private string _displayName;

	private Func<Type, object> _serviceProvider;

	public object ObjectInstance { get; }

	public Type ObjectType => ObjectInstance.GetType();

	public string DisplayName
	{
		get
		{
			if (string.IsNullOrEmpty(_displayName))
			{
				_displayName = GetDisplayName();
				if (string.IsNullOrEmpty(_displayName))
				{
					_displayName = ObjectType.Name;
				}
			}
			return _displayName;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}
			_displayName = value;
		}
	}

	public string? MemberName { get; set; }

	public IDictionary<object, object?> Items => _items;

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public ValidationContext(object instance)
		: this(instance, null, null)
	{
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public ValidationContext(object instance, IDictionary<object, object?>? items)
		: this(instance, null, items)
	{
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public ValidationContext(object instance, IServiceProvider? serviceProvider, IDictionary<object, object?>? items)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (serviceProvider != null)
		{
			IServiceProvider localServiceProvider = serviceProvider;
			InitializeServiceProvider((Type serviceType) => localServiceProvider.GetService(serviceType));
		}
		_items = ((items != null) ? new Dictionary<object, object>(items) : new Dictionary<object, object>());
		ObjectInstance = instance;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctors are marked with RequiresUnreferencedCode.")]
	private string GetDisplayName()
	{
		string text = null;
		ValidationAttributeStore instance = ValidationAttributeStore.Instance;
		DisplayAttribute displayAttribute = null;
		if (string.IsNullOrEmpty(MemberName))
		{
			displayAttribute = instance.GetTypeDisplayAttribute(this);
		}
		else if (instance.IsPropertyContext(this))
		{
			displayAttribute = instance.GetPropertyDisplayAttribute(this);
		}
		if (displayAttribute != null)
		{
			text = displayAttribute.GetName();
		}
		return text ?? MemberName;
	}

	public void InitializeServiceProvider(Func<Type, object?> serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public object? GetService(Type serviceType)
	{
		return _serviceProvider?.Invoke(serviceType);
	}
}
