using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System;

[CLSCompliant(false)]
[NonVersionable]
public ref struct TypedReference
{
	private readonly ByReference<byte> _value;

	private readonly IntPtr _type;

	internal bool IsNull
	{
		get
		{
			if (Unsafe.IsNullRef(ref _value.Value))
			{
				return _type == IntPtr.Zero;
			}
			return false;
		}
	}

	public unsafe static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (flds == null)
		{
			throw new ArgumentNullException("flds");
		}
		if (flds.Length == 0)
		{
			throw new ArgumentException(SR.Arg_ArrayZeroError, "flds");
		}
		IntPtr[] array = new IntPtr[flds.Length];
		RuntimeType runtimeType = (RuntimeType)target.GetType();
		for (int i = 0; i < flds.Length; i++)
		{
			RuntimeFieldInfo runtimeFieldInfo = flds[i] as RuntimeFieldInfo;
			if (runtimeFieldInfo == null)
			{
				throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo);
			}
			if (runtimeFieldInfo.IsStatic)
			{
				throw new ArgumentException(SR.Format(SR.Argument_TypedReferenceInvalidField, runtimeFieldInfo.Name));
			}
			if (runtimeType != runtimeFieldInfo.GetDeclaringTypeInternal() && !runtimeType.IsSubclassOf(runtimeFieldInfo.GetDeclaringTypeInternal()))
			{
				throw new MissingMemberException(SR.MissingMemberTypeRef);
			}
			RuntimeType runtimeType2 = (RuntimeType)runtimeFieldInfo.FieldType;
			if (runtimeType2.IsPrimitive)
			{
				throw new ArgumentException(SR.Format(SR.Arg_TypeRefPrimitve, runtimeFieldInfo.Name));
			}
			if (i < flds.Length - 1 && !runtimeType2.IsValueType)
			{
				throw new MissingMemberException(SR.MissingMemberNestErr);
			}
			array[i] = runtimeFieldInfo.FieldHandle.Value;
			runtimeType = runtimeType2;
		}
		TypedReference result = default(TypedReference);
		InternalMakeTypedReference(&result, target, array, runtimeType);
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void InternalMakeTypedReference(void* result, object target, IntPtr[] flds, RuntimeType lastFieldType);

	public override int GetHashCode()
	{
		if (_type == IntPtr.Zero)
		{
			return 0;
		}
		return __reftype(this).GetHashCode();
	}

	public override bool Equals(object? o)
	{
		throw new NotSupportedException(SR.NotSupported_NYI);
	}

	public unsafe static object ToObject(TypedReference value)
	{
		return InternalToObject(&value);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern object InternalToObject(void* value);

	public static Type GetTargetType(TypedReference value)
	{
		return __reftype(value);
	}

	public static RuntimeTypeHandle TargetTypeToken(TypedReference value)
	{
		return __reftype(value).TypeHandle;
	}

	public static void SetTypedReference(TypedReference target, object? value)
	{
		throw new NotSupportedException();
	}
}
