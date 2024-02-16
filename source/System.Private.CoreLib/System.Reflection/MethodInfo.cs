using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public abstract class MethodInfo : MethodBase
{
	public override MemberTypes MemberType => MemberTypes.Method;

	public virtual ParameterInfo ReturnParameter
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual Type ReturnType
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public abstract ICustomAttributeProvider ReturnTypeCustomAttributes { get; }

	internal virtual int GenericParameterCount => GetGenericArguments().Length;

	public override Type[] GetGenericArguments()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual MethodInfo GetGenericMethodDefinition()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments)
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public abstract MethodInfo GetBaseDefinition();

	public virtual Delegate CreateDelegate(Type delegateType)
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public virtual Delegate CreateDelegate(Type delegateType, object? target)
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	public T CreateDelegate<T>() where T : Delegate
	{
		return (T)CreateDelegate(typeof(T));
	}

	public T CreateDelegate<T>(object? target) where T : Delegate
	{
		return (T)CreateDelegate(typeof(T), target);
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
	public static bool operator ==(MethodInfo? left, MethodInfo? right)
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

	public static bool operator !=(MethodInfo? left, MethodInfo? right)
	{
		return !(left == right);
	}
}
