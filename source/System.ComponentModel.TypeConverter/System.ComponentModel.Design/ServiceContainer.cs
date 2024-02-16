using System.Collections.Generic;

namespace System.ComponentModel.Design;

public class ServiceContainer : IServiceContainer, IServiceProvider, IDisposable
{
	private sealed class ServiceCollection<T> : Dictionary<Type, T>
	{
		private sealed class EmbeddedTypeAwareTypeComparer : IEqualityComparer<Type>
		{
			public bool Equals(Type x, Type y)
			{
				return x.IsEquivalentTo(y);
			}

			public int GetHashCode(Type obj)
			{
				return obj.FullName.GetHashCode();
			}
		}

		private static readonly EmbeddedTypeAwareTypeComparer s_serviceTypeComparer = new EmbeddedTypeAwareTypeComparer();

		public ServiceCollection()
			: base((IEqualityComparer<Type>?)s_serviceTypeComparer)
		{
		}
	}

	private ServiceCollection<object> _services;

	private readonly IServiceProvider _parentProvider;

	private static readonly Type[] s_defaultServices = new Type[2]
	{
		typeof(IServiceContainer),
		typeof(ServiceContainer)
	};

	private IServiceContainer? Container => _parentProvider?.GetService(typeof(IServiceContainer)) as IServiceContainer;

	protected virtual Type[] DefaultServices => s_defaultServices;

	private ServiceCollection<object?> Services => _services ?? (_services = new ServiceCollection<object>());

	public ServiceContainer()
	{
	}

	public ServiceContainer(IServiceProvider? parentProvider)
	{
		_parentProvider = parentProvider;
	}

	public void AddService(Type serviceType, object serviceInstance)
	{
		AddService(serviceType, serviceInstance, promote: false);
	}

	public virtual void AddService(Type serviceType, object serviceInstance, bool promote)
	{
		if (promote)
		{
			IServiceContainer container = Container;
			if (container != null)
			{
				container.AddService(serviceType, serviceInstance, promote);
				return;
			}
		}
		if (serviceType == null)
		{
			throw new ArgumentNullException("serviceType");
		}
		if (serviceInstance == null)
		{
			throw new ArgumentNullException("serviceInstance");
		}
		if (!(serviceInstance is ServiceCreatorCallback) && !serviceInstance.GetType().IsCOMObject && !serviceType.IsInstanceOfType(serviceInstance))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ErrorInvalidServiceInstance, serviceType.FullName));
		}
		if (Services.ContainsKey(serviceType))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ErrorServiceExists, serviceType.FullName), "serviceType");
		}
		Services[serviceType] = serviceInstance;
	}

	public void AddService(Type serviceType, ServiceCreatorCallback callback)
	{
		AddService(serviceType, callback, promote: false);
	}

	public virtual void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
	{
		if (promote)
		{
			IServiceContainer container = Container;
			if (container != null)
			{
				container.AddService(serviceType, callback, promote);
				return;
			}
		}
		if (serviceType == null)
		{
			throw new ArgumentNullException("serviceType");
		}
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		if (Services.ContainsKey(serviceType))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ErrorServiceExists, serviceType.FullName), "serviceType");
		}
		Services[serviceType] = callback;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}
		ServiceCollection<object> services = _services;
		_services = null;
		if (services == null)
		{
			return;
		}
		foreach (object value in services.Values)
		{
			if (value is IDisposable)
			{
				((IDisposable)value).Dispose();
			}
		}
	}

	public virtual object? GetService(Type serviceType)
	{
		object value = null;
		Type[] defaultServices = DefaultServices;
		for (int i = 0; i < defaultServices.Length; i++)
		{
			if (serviceType != null && serviceType.IsEquivalentTo(defaultServices[i]))
			{
				value = this;
				break;
			}
		}
		if (value == null && serviceType != null)
		{
			Services.TryGetValue(serviceType, out value);
		}
		if (value is ServiceCreatorCallback)
		{
			value = ((ServiceCreatorCallback)value)(this, serviceType);
			if (value != null && !value.GetType().IsCOMObject && !serviceType.IsInstanceOfType(value))
			{
				value = null;
			}
			Services[serviceType] = value;
		}
		if (value == null && _parentProvider != null)
		{
			value = _parentProvider.GetService(serviceType);
		}
		return value;
	}

	public void RemoveService(Type serviceType)
	{
		RemoveService(serviceType, promote: false);
	}

	public virtual void RemoveService(Type serviceType, bool promote)
	{
		if (promote)
		{
			IServiceContainer container = Container;
			if (container != null)
			{
				container.RemoveService(serviceType, promote);
				return;
			}
		}
		if (serviceType == null)
		{
			throw new ArgumentNullException("serviceType");
		}
		Services.Remove(serviceType);
	}
}
