using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Reflection;

[CLSCompliant(false)]
public sealed class Pointer : ISerializable
{
	private unsafe readonly void* _ptr;

	private readonly Type _ptrType;

	private unsafe Pointer(void* ptr, Type ptrType)
	{
		_ptr = ptr;
		_ptrType = ptrType;
	}

	public unsafe static object Box(void* ptr, Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (!type.IsPointer)
		{
			throw new ArgumentException(SR.Arg_MustBePointer, "ptr");
		}
		if (!type.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Arg_MustBeType, "ptr");
		}
		return new Pointer(ptr, type);
	}

	public unsafe static void* Unbox(object ptr)
	{
		if (!(ptr is Pointer))
		{
			throw new ArgumentException(SR.Arg_MustBePointer, "ptr");
		}
		return ((Pointer)ptr)._ptr;
	}

	public unsafe override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Pointer pointer)
		{
			return _ptr == pointer._ptr;
		}
		return false;
	}

	public unsafe override int GetHashCode()
	{
		UIntPtr ptr = (UIntPtr)_ptr;
		return ptr.GetHashCode();
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	internal Type GetPointerType()
	{
		return _ptrType;
	}

	internal unsafe IntPtr GetPointerValue()
	{
		return (IntPtr)_ptr;
	}
}
