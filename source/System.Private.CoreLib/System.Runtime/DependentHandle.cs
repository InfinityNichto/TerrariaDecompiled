using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime;

public struct DependentHandle : IDisposable
{
	private IntPtr _handle;

	public bool IsAllocated => _handle != (IntPtr)0;

	public object? Target
	{
		get
		{
			IntPtr handle = _handle;
			if (handle == (IntPtr)0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			return InternalGetTarget(handle);
		}
		set
		{
			IntPtr handle = _handle;
			if (handle == (IntPtr)0 || value != null)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			InternalSetTargetToNull(handle);
		}
	}

	public object? Dependent
	{
		get
		{
			IntPtr handle = _handle;
			if (handle == (IntPtr)0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			return InternalGetDependent(handle);
		}
		set
		{
			IntPtr handle = _handle;
			if (handle == (IntPtr)0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			InternalSetDependent(handle, value);
		}
	}

	public (object? Target, object? Dependent) TargetAndDependent
	{
		get
		{
			IntPtr handle = _handle;
			if (handle == (IntPtr)0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			object dependent;
			object item = InternalGetTargetAndDependent(handle, out dependent);
			return (Target: item, Dependent: dependent);
		}
	}

	public DependentHandle(object? target, object? dependent)
	{
		_handle = InternalInitialize(target, dependent);
	}

	internal object UnsafeGetTarget()
	{
		return InternalGetTarget(_handle);
	}

	internal object UnsafeGetTargetAndDependent(out object dependent)
	{
		return InternalGetTargetAndDependent(_handle, out dependent);
	}

	internal void UnsafeSetTargetToNull()
	{
		InternalSetTargetToNull(_handle);
	}

	internal void UnsafeSetDependent(object dependent)
	{
		InternalSetDependent(_handle, dependent);
	}

	public void Dispose()
	{
		IntPtr handle = _handle;
		if (handle != (IntPtr)0)
		{
			_handle = IntPtr.Zero;
			InternalFree(handle);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr InternalInitialize(object target, object dependent);

	private unsafe static object InternalGetTarget(IntPtr dependentHandle)
	{
		return Unsafe.As<IntPtr, object>(ref *(IntPtr*)dependentHandle);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalGetDependent(IntPtr dependentHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalGetTargetAndDependent(IntPtr dependentHandle, out object dependent);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void InternalSetDependent(IntPtr dependentHandle, object dependent);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void InternalSetTargetToNull(IntPtr dependentHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void InternalFree(IntPtr dependentHandle);
}
