using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public abstract class EventInfo : MemberInfo
{
	public override MemberTypes MemberType => MemberTypes.Event;

	public abstract EventAttributes Attributes { get; }

	public bool IsSpecialName => (Attributes & EventAttributes.SpecialName) != 0;

	public virtual MethodInfo? AddMethod => GetAddMethod(nonPublic: true);

	public virtual MethodInfo? RemoveMethod => GetRemoveMethod(nonPublic: true);

	public virtual MethodInfo? RaiseMethod => GetRaiseMethod(nonPublic: true);

	public virtual bool IsMulticast
	{
		get
		{
			Type eventHandlerType = EventHandlerType;
			Type typeFromHandle = typeof(MulticastDelegate);
			return typeFromHandle.IsAssignableFrom(eventHandlerType);
		}
	}

	public virtual Type? EventHandlerType
	{
		get
		{
			MethodInfo addMethod = GetAddMethod(nonPublic: true);
			ParameterInfo[] parametersNoCopy = addMethod.GetParametersNoCopy();
			Type typeFromHandle = typeof(Delegate);
			for (int i = 0; i < parametersNoCopy.Length; i++)
			{
				Type parameterType = parametersNoCopy[i].ParameterType;
				if (parameterType.IsSubclassOf(typeFromHandle))
				{
					return parameterType;
				}
			}
			return null;
		}
	}

	public MethodInfo[] GetOtherMethods()
	{
		return GetOtherMethods(nonPublic: false);
	}

	public virtual MethodInfo[] GetOtherMethods(bool nonPublic)
	{
		throw NotImplemented.ByDesign;
	}

	public MethodInfo? GetAddMethod()
	{
		return GetAddMethod(nonPublic: false);
	}

	public MethodInfo? GetRemoveMethod()
	{
		return GetRemoveMethod(nonPublic: false);
	}

	public MethodInfo? GetRaiseMethod()
	{
		return GetRaiseMethod(nonPublic: false);
	}

	public abstract MethodInfo? GetAddMethod(bool nonPublic);

	public abstract MethodInfo? GetRemoveMethod(bool nonPublic);

	public abstract MethodInfo? GetRaiseMethod(bool nonPublic);

	[DebuggerHidden]
	[DebuggerStepThrough]
	public virtual void AddEventHandler(object? target, Delegate? handler)
	{
		MethodInfo addMethod = GetAddMethod(nonPublic: false);
		if (addMethod == null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NoPublicAddMethod);
		}
		addMethod.Invoke(target, new object[1] { handler });
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public virtual void RemoveEventHandler(object? target, Delegate? handler)
	{
		MethodInfo removeMethod = GetRemoveMethod(nonPublic: false);
		if (removeMethod == null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NoPublicRemoveMethod);
		}
		removeMethod.Invoke(target, new object[1] { handler });
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
	public static bool operator ==(EventInfo? left, EventInfo? right)
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

	public static bool operator !=(EventInfo? left, EventInfo? right)
	{
		return !(left == right);
	}
}
