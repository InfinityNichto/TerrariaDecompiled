using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System;

internal sealed class Gen2GcCallback : CriticalFinalizerObject
{
	private readonly Func<bool> _callback0;

	private readonly Func<object, bool> _callback1;

	private GCHandle _weakTargetObj;

	private Gen2GcCallback(Func<bool> callback)
	{
		_callback0 = callback;
	}

	private Gen2GcCallback(Func<object, bool> callback, object targetObj)
	{
		_callback1 = callback;
		_weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
	}

	public static void Register(Func<bool> callback)
	{
		new Gen2GcCallback(callback);
	}

	public static void Register(Func<object, bool> callback, object targetObj)
	{
		new Gen2GcCallback(callback, targetObj);
	}

	~Gen2GcCallback()
	{
		if (_weakTargetObj.IsAllocated)
		{
			object target = _weakTargetObj.Target;
			if (target == null)
			{
				_weakTargetObj.Free();
				return;
			}
			try
			{
				if (!_callback1(target))
				{
					_weakTargetObj.Free();
					return;
				}
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				if (!_callback0())
				{
					return;
				}
			}
			catch
			{
			}
		}
		GC.ReRegisterForFinalize(this);
	}
}
