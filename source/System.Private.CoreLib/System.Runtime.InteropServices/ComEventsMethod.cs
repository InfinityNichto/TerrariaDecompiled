using System.Collections.Generic;
using System.Reflection;

namespace System.Runtime.InteropServices;

internal sealed class ComEventsMethod
{
	public class DelegateWrapper
	{
		private bool _once;

		private int _expectedParamsCount;

		private Type[] _cachedTargetTypes;

		public Delegate Delegate { get; set; }

		public bool WrapArgs { get; }

		public DelegateWrapper(Delegate d, bool wrapArgs)
		{
			Delegate = d;
			WrapArgs = wrapArgs;
		}

		public object Invoke(object[] args)
		{
			if ((object)Delegate == null)
			{
				return null;
			}
			if (!_once)
			{
				PreProcessSignature();
				_once = true;
			}
			if (_cachedTargetTypes != null && _expectedParamsCount == args.Length)
			{
				for (int i = 0; i < _expectedParamsCount; i++)
				{
					Type type = _cachedTargetTypes[i];
					if ((object)type != null)
					{
						args[i] = Enum.ToObject(type, args[i]);
					}
				}
			}
			return Delegate.DynamicInvoke((!WrapArgs) ? args : new object[1] { args });
		}

		private void PreProcessSignature()
		{
			ParameterInfo[] parameters = Delegate.Method.GetParameters();
			_expectedParamsCount = parameters.Length;
			Type[] array = null;
			for (int i = 0; i < _expectedParamsCount; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				if (parameterInfo.ParameterType.IsByRef && parameterInfo.ParameterType.HasElementType && parameterInfo.ParameterType.GetElementType().IsEnum)
				{
					if (array == null)
					{
						array = new Type[_expectedParamsCount];
					}
					array[i] = parameterInfo.ParameterType.GetElementType();
				}
			}
			if (array != null)
			{
				_cachedTargetTypes = array;
			}
		}
	}

	private readonly List<DelegateWrapper> _delegateWrappers = new List<DelegateWrapper>();

	private readonly int _dispid;

	private ComEventsMethod _next;

	public bool Empty
	{
		get
		{
			lock (_delegateWrappers)
			{
				return _delegateWrappers.Count == 0;
			}
		}
	}

	public ComEventsMethod(int dispid)
	{
		_dispid = dispid;
	}

	public static ComEventsMethod Find(ComEventsMethod methods, int dispid)
	{
		while (methods != null && methods._dispid != dispid)
		{
			methods = methods._next;
		}
		return methods;
	}

	public static ComEventsMethod Add(ComEventsMethod methods, ComEventsMethod method)
	{
		method._next = methods;
		return method;
	}

	public static ComEventsMethod Remove(ComEventsMethod methods, ComEventsMethod method)
	{
		if (methods == method)
		{
			return methods._next;
		}
		ComEventsMethod comEventsMethod = methods;
		while (comEventsMethod != null && comEventsMethod._next != method)
		{
			comEventsMethod = comEventsMethod._next;
		}
		if (comEventsMethod != null)
		{
			comEventsMethod._next = method._next;
		}
		return methods;
	}

	public void AddDelegate(Delegate d, bool wrapArgs = false)
	{
		lock (_delegateWrappers)
		{
			foreach (DelegateWrapper delegateWrapper in _delegateWrappers)
			{
				if (delegateWrapper.Delegate.GetType() == d.GetType() && delegateWrapper.WrapArgs == wrapArgs)
				{
					delegateWrapper.Delegate = Delegate.Combine(delegateWrapper.Delegate, d);
					return;
				}
			}
			DelegateWrapper item = new DelegateWrapper(d, wrapArgs);
			_delegateWrappers.Add(item);
		}
	}

	public void RemoveDelegate(Delegate d, bool wrapArgs = false)
	{
		lock (_delegateWrappers)
		{
			int num = -1;
			DelegateWrapper delegateWrapper = null;
			for (int i = 0; i < _delegateWrappers.Count; i++)
			{
				DelegateWrapper delegateWrapper2 = _delegateWrappers[i];
				if (delegateWrapper2.Delegate.GetType() == d.GetType() && delegateWrapper2.WrapArgs == wrapArgs)
				{
					num = i;
					delegateWrapper = delegateWrapper2;
					break;
				}
			}
			if (num >= 0)
			{
				Delegate @delegate = Delegate.Remove(delegateWrapper.Delegate, d);
				if ((object)@delegate != null)
				{
					delegateWrapper.Delegate = @delegate;
				}
				else
				{
					_delegateWrappers.RemoveAt(num);
				}
			}
		}
	}

	public object Invoke(object[] args)
	{
		object result = null;
		lock (_delegateWrappers)
		{
			foreach (DelegateWrapper delegateWrapper in _delegateWrappers)
			{
				result = delegateWrapper.Invoke(args);
			}
			return result;
		}
	}
}
