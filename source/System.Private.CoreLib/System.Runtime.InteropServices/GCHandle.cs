using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public struct GCHandle
{
	private IntPtr _handle;

	public object? Target
	{
		get
		{
			IntPtr handle = _handle;
			ThrowIfInvalid(handle);
			return InternalGet(GetHandleValue(handle));
		}
		set
		{
			IntPtr handle = _handle;
			ThrowIfInvalid(handle);
			if (IsPinned(handle) && !Marshal.IsPinnable(value))
			{
				throw new ArgumentException(SR.ArgumentException_NotIsomorphic, "value");
			}
			InternalSet(GetHandleValue(handle), value);
		}
	}

	public bool IsAllocated => _handle != (IntPtr)0;

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr InternalAlloc(object value, GCHandleType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InternalFree(IntPtr handle);

	internal unsafe static object InternalGet(IntPtr handle)
	{
		return Unsafe.As<IntPtr, object>(ref *(IntPtr*)handle);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InternalSet(IntPtr handle, object value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object InternalCompareExchange(IntPtr handle, object value, object oldValue);

	private GCHandle(object value, GCHandleType type)
	{
		switch (type)
		{
		default:
			throw new ArgumentOutOfRangeException("type", SR.ArgumentOutOfRange_Enum);
		case GCHandleType.Pinned:
			if (!Marshal.IsPinnable(value))
			{
				throw new ArgumentException(SR.ArgumentException_NotIsomorphic, "value");
			}
			break;
		case GCHandleType.Weak:
		case GCHandleType.WeakTrackResurrection:
		case GCHandleType.Normal:
			break;
		}
		nint num = InternalAlloc(value, type);
		if (type == GCHandleType.Pinned)
		{
			num |= 1;
		}
		_handle = num;
	}

	private GCHandle(IntPtr handle)
	{
		_handle = handle;
	}

	public static GCHandle Alloc(object? value)
	{
		return new GCHandle(value, GCHandleType.Normal);
	}

	public static GCHandle Alloc(object? value, GCHandleType type)
	{
		return new GCHandle(value, type);
	}

	public void Free()
	{
		IntPtr handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
		ThrowIfInvalid(handle);
		InternalFree(GetHandleValue(handle));
	}

	public unsafe IntPtr AddrOfPinnedObject()
	{
		IntPtr handle = _handle;
		ThrowIfInvalid(handle);
		if (!IsPinned(handle))
		{
			ThrowHelper.ThrowInvalidOperationException_HandleIsNotPinned();
		}
		object obj = InternalGet(GetHandleValue(handle));
		if (obj == null)
		{
			return (IntPtr)0;
		}
		if (RuntimeHelpers.ObjectHasComponentSize(obj))
		{
			if (obj.GetType() == typeof(string))
			{
				return (IntPtr)Unsafe.AsPointer(ref Unsafe.As<string>(obj).GetRawStringData());
			}
			return (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<Array>(obj)));
		}
		return (IntPtr)Unsafe.AsPointer(ref obj.GetRawData());
	}

	public static explicit operator GCHandle(IntPtr value)
	{
		return FromIntPtr(value);
	}

	public static GCHandle FromIntPtr(IntPtr value)
	{
		ThrowIfInvalid(value);
		return new GCHandle(value);
	}

	public static explicit operator IntPtr(GCHandle value)
	{
		return ToIntPtr(value);
	}

	public static IntPtr ToIntPtr(GCHandle value)
	{
		return value._handle;
	}

	public override int GetHashCode()
	{
		return _handle.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is GCHandle)
		{
			return _handle == ((GCHandle)o)._handle;
		}
		return false;
	}

	public static bool operator ==(GCHandle a, GCHandle b)
	{
		return a._handle == b._handle;
	}

	public static bool operator !=(GCHandle a, GCHandle b)
	{
		return a._handle != b._handle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static IntPtr GetHandleValue(IntPtr handle)
	{
		return new IntPtr((nint)handle & ~(nint)1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPinned(IntPtr handle)
	{
		return ((nint)handle & 1) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ThrowIfInvalid(IntPtr handle)
	{
		if (handle == (IntPtr)0)
		{
			ThrowHelper.ThrowInvalidOperationException_HandleIsNotInitialized();
		}
	}
}
