using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public abstract class ConstructorInfo : MethodBase
{
	public static readonly string ConstructorName = ".ctor";

	public static readonly string TypeConstructorName = ".cctor";

	public override MemberTypes MemberType => MemberTypes.Constructor;

	internal virtual Type GetReturnType()
	{
		throw new NotImplementedException();
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public object Invoke(object?[]? parameters)
	{
		return Invoke(BindingFlags.Default, null, parameters, null);
	}

	public abstract object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

	public override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(ConstructorInfo? left, ConstructorInfo? right)
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

	public static bool operator !=(ConstructorInfo? left, ConstructorInfo? right)
	{
		return !(left == right);
	}
}
