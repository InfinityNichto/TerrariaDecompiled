using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Object
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public extern Type GetType();

	[Intrinsic]
	protected unsafe object MemberwiseClone()
	{
		object obj = RuntimeHelpers.AllocateUninitializedClone(this);
		nuint rawObjectDataSize = RuntimeHelpers.GetRawObjectDataSize(obj);
		ref byte rawData = ref this.GetRawData();
		ref byte rawData2 = ref obj.GetRawData();
		if (RuntimeHelpers.GetMethodTable(obj)->ContainsGCPointers)
		{
			Buffer.BulkMoveWithWriteBarrier(ref rawData2, ref rawData, rawObjectDataSize);
		}
		else
		{
			Buffer.Memmove(ref rawData2, ref rawData, rawObjectDataSize);
		}
		return obj;
	}

	[NonVersionable]
	public Object()
	{
	}

	[NonVersionable]
	~Object()
	{
	}

	public virtual string? ToString()
	{
		return GetType().ToString();
	}

	public virtual bool Equals(object? obj)
	{
		return RuntimeHelpers.Equals(this, obj);
	}

	public static bool Equals(object? objA, object? objB)
	{
		if (objA == objB)
		{
			return true;
		}
		if (objA == null || objB == null)
		{
			return false;
		}
		return objA.Equals(objB);
	}

	[NonVersionable]
	public static bool ReferenceEquals(object? objA, object? objB)
	{
		return objA == objB;
	}

	public virtual int GetHashCode()
	{
		return RuntimeHelpers.GetHashCode(this);
	}
}
