using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public abstract class PropertyInfo : MemberInfo
{
	public override MemberTypes MemberType => MemberTypes.Property;

	public abstract Type PropertyType { get; }

	public abstract PropertyAttributes Attributes { get; }

	public bool IsSpecialName => (Attributes & PropertyAttributes.SpecialName) != 0;

	public abstract bool CanRead { get; }

	public abstract bool CanWrite { get; }

	public virtual MethodInfo? GetMethod => GetGetMethod(nonPublic: true);

	public virtual MethodInfo? SetMethod => GetSetMethod(nonPublic: true);

	public abstract ParameterInfo[] GetIndexParameters();

	public MethodInfo[] GetAccessors()
	{
		return GetAccessors(nonPublic: false);
	}

	public abstract MethodInfo[] GetAccessors(bool nonPublic);

	public MethodInfo? GetGetMethod()
	{
		return GetGetMethod(nonPublic: false);
	}

	public abstract MethodInfo? GetGetMethod(bool nonPublic);

	public MethodInfo? GetSetMethod()
	{
		return GetSetMethod(nonPublic: false);
	}

	public abstract MethodInfo? GetSetMethod(bool nonPublic);

	public virtual Type[] GetOptionalCustomModifiers()
	{
		return Type.EmptyTypes;
	}

	public virtual Type[] GetRequiredCustomModifiers()
	{
		return Type.EmptyTypes;
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public object? GetValue(object? obj)
	{
		return GetValue(obj, null);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public virtual object? GetValue(object? obj, object?[]? index)
	{
		return GetValue(obj, BindingFlags.Default, null, index, null);
	}

	public abstract object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture);

	public virtual object? GetConstantValue()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual object? GetRawConstantValue()
	{
		throw NotImplemented.ByDesign;
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public void SetValue(object? obj, object? value)
	{
		SetValue(obj, value, null);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public virtual void SetValue(object? obj, object? value, object?[]? index)
	{
		SetValue(obj, value, BindingFlags.Default, null, index, null);
	}

	public abstract void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture);

	public override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(PropertyInfo? left, PropertyInfo? right)
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

	public static bool operator !=(PropertyInfo? left, PropertyInfo? right)
	{
		return !(left == right);
	}
}
