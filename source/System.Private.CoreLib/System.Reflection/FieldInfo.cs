using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public abstract class FieldInfo : MemberInfo
{
	public override MemberTypes MemberType => MemberTypes.Field;

	public abstract FieldAttributes Attributes { get; }

	public abstract Type FieldType { get; }

	public bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) != 0;

	public bool IsLiteral => (Attributes & FieldAttributes.Literal) != 0;

	public bool IsNotSerialized => (Attributes & FieldAttributes.NotSerialized) != 0;

	public bool IsPinvokeImpl => (Attributes & FieldAttributes.PinvokeImpl) != 0;

	public bool IsSpecialName => (Attributes & FieldAttributes.SpecialName) != 0;

	public bool IsStatic => (Attributes & FieldAttributes.Static) != 0;

	public bool IsAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

	public bool IsFamily => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

	public bool IsFamilyAndAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem;

	public bool IsFamilyOrAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem;

	public bool IsPrivate => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

	public bool IsPublic => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

	public virtual bool IsSecurityCritical => true;

	public virtual bool IsSecuritySafeCritical => false;

	public virtual bool IsSecurityTransparent => false;

	public abstract RuntimeFieldHandle FieldHandle { get; }

	public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle)
	{
		if (handle.IsNullHandle())
		{
			throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
		}
		FieldInfo fieldInfo = RuntimeType.GetFieldInfo(handle.GetRuntimeFieldInfo());
		Type declaringType = fieldInfo.DeclaringType;
		if (declaringType != null && declaringType.IsGenericType)
		{
			throw new ArgumentException(SR.Format(SR.Argument_FieldDeclaringTypeGeneric, fieldInfo.Name, declaringType.GetGenericTypeDefinition()));
		}
		return fieldInfo;
	}

	public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType)
	{
		if (handle.IsNullHandle())
		{
			throw new ArgumentException(SR.Argument_InvalidHandle);
		}
		return RuntimeType.GetFieldInfo(declaringType.GetRuntimeType(), handle.GetRuntimeFieldInfo());
	}

	public override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(FieldInfo? left, FieldInfo? right)
	{
		if ((object)right == null)
		{
			if ((object)left != null)
			{
				return false;
			}
			return true;
		}
		if ((object)left == right)
		{
			return true;
		}
		return left?.Equals(right) ?? false;
	}

	public static bool operator !=(FieldInfo? left, FieldInfo? right)
	{
		return !(left == right);
	}

	public abstract object? GetValue(object? obj);

	[DebuggerHidden]
	[DebuggerStepThrough]
	public void SetValue(object? obj, object? value)
	{
		SetValue(obj, value, BindingFlags.Default, Type.DefaultBinder, null);
	}

	public abstract void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture);

	[CLSCompliant(false)]
	public virtual void SetValueDirect(TypedReference obj, object value)
	{
		throw new NotSupportedException(SR.NotSupported_AbstractNonCLS);
	}

	[CLSCompliant(false)]
	public virtual object? GetValueDirect(TypedReference obj)
	{
		throw new NotSupportedException(SR.NotSupported_AbstractNonCLS);
	}

	public virtual object? GetRawConstantValue()
	{
		throw new NotSupportedException(SR.NotSupported_AbstractNonCLS);
	}

	public virtual Type[] GetOptionalCustomModifiers()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual Type[] GetRequiredCustomModifiers()
	{
		throw NotImplemented.ByDesign;
	}
}
