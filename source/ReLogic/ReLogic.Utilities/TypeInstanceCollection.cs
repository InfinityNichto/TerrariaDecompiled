using System;
using System.Collections.Generic;

namespace ReLogic.Utilities;

internal class TypeInstanceCollection<BaseType> : IDisposable where BaseType : class
{
	private Dictionary<Type, BaseType> _services = new Dictionary<Type, BaseType>();

	private bool _disposedValue;

	public void Register<T>(T instance) where T : BaseType
	{
		_services.Add(typeof(T), (BaseType)(object)instance);
	}

	public T Get<T>() where T : BaseType
	{
		if (_services.TryGetValue(typeof(T), out var value))
		{
			return (T)value;
		}
		return default(T);
	}

	public void Remove<T>() where T : BaseType
	{
		_services.Remove(typeof(T));
	}

	public bool Has<T>() where T : BaseType
	{
		return _services.ContainsKey(typeof(T));
	}

	public bool Has(Type type)
	{
		return _services.ContainsKey(type);
	}

	public void IfHas<T>(Action<T> callback) where T : BaseType
	{
		if (_services.TryGetValue(typeof(T), out var value))
		{
			callback((T)value);
		}
	}

	public U IfHas<T, U>(Func<T, U> callback) where T : BaseType
	{
		if (_services.TryGetValue(typeof(T), out var value))
		{
			return callback((T)value);
		}
		return default(U);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}
		if (disposing)
		{
			foreach (BaseType value in _services.Values)
			{
				if (value is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			_services.Clear();
		}
		_disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
