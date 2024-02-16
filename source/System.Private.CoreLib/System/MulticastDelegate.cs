using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System;

[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
public abstract class MulticastDelegate : Delegate
{
	private object _invocationList;

	private IntPtr _invocationCount;

	[RequiresUnreferencedCode("The target method might be removed")]
	protected MulticastDelegate(object target, string method)
		: base(target, method)
	{
	}

	protected MulticastDelegate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method)
		: base(target, method)
	{
	}

	internal bool IsUnmanagedFunctionPtr()
	{
		return _invocationCount == (IntPtr)(-1);
	}

	internal bool InvocationListLogicallyNull()
	{
		if (_invocationList != null && !(_invocationList is LoaderAllocator))
		{
			return _invocationList is DynamicResolver;
		}
		return true;
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new SerializationException(SR.Serialization_DelegatesNotSupported);
	}

	public sealed override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (!Delegate.InternalEqualTypes(this, obj))
		{
			return false;
		}
		MulticastDelegate multicastDelegate = Unsafe.As<MulticastDelegate>(obj);
		if (_invocationCount != (IntPtr)0)
		{
			if (InvocationListLogicallyNull())
			{
				if (IsUnmanagedFunctionPtr())
				{
					if (!multicastDelegate.IsUnmanagedFunctionPtr())
					{
						return false;
					}
					return Delegate.CompareUnmanagedFunctionPtrs(this, multicastDelegate);
				}
				if (multicastDelegate._invocationList is Delegate)
				{
					return Equals(multicastDelegate._invocationList);
				}
				return base.Equals(obj);
			}
			if (_invocationList is Delegate @delegate)
			{
				return @delegate.Equals(obj);
			}
			return InvocationListEquals(multicastDelegate);
		}
		if (!InvocationListLogicallyNull())
		{
			if (!_invocationList.Equals(multicastDelegate._invocationList))
			{
				return false;
			}
			return base.Equals((object?)multicastDelegate);
		}
		if (multicastDelegate._invocationList is Delegate)
		{
			return Equals(multicastDelegate._invocationList);
		}
		return base.Equals((object?)multicastDelegate);
	}

	private bool InvocationListEquals(MulticastDelegate d)
	{
		object[] array = (object[])_invocationList;
		if (d._invocationCount != _invocationCount)
		{
			return false;
		}
		int num = (int)_invocationCount;
		for (int i = 0; i < num; i++)
		{
			Delegate @delegate = (Delegate)array[i];
			object[] array2 = d._invocationList as object[];
			if (!@delegate.Equals(array2[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TrySetSlot(object[] a, int index, object o)
	{
		if (a[index] == null && Interlocked.CompareExchange<object>(ref a[index], o, (object)null) == null)
		{
			return true;
		}
		object obj = a[index];
		if (obj != null)
		{
			MulticastDelegate multicastDelegate = (MulticastDelegate)o;
			MulticastDelegate multicastDelegate2 = (MulticastDelegate)obj;
			if (multicastDelegate2._methodPtr == multicastDelegate._methodPtr && multicastDelegate2._target == multicastDelegate._target && multicastDelegate2._methodPtrAux == multicastDelegate._methodPtrAux)
			{
				return true;
			}
		}
		return false;
	}

	private MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount, bool thisIsMultiCastAlready)
	{
		MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
		if (thisIsMultiCastAlready)
		{
			multicastDelegate._methodPtr = _methodPtr;
			multicastDelegate._methodPtrAux = _methodPtrAux;
		}
		else
		{
			multicastDelegate._methodPtr = GetMulticastInvoke();
			multicastDelegate._methodPtrAux = GetInvokeMethod();
		}
		multicastDelegate._target = multicastDelegate;
		multicastDelegate._invocationList = invocationList;
		multicastDelegate._invocationCount = (IntPtr)invocationCount;
		return multicastDelegate;
	}

	internal MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount)
	{
		return NewMulticastDelegate(invocationList, invocationCount, thisIsMultiCastAlready: false);
	}

	internal void StoreDynamicMethod(MethodInfo dynamicMethod)
	{
		if (_invocationCount != (IntPtr)0)
		{
			MulticastDelegate multicastDelegate = (MulticastDelegate)_invocationList;
			multicastDelegate._methodBase = dynamicMethod;
		}
		else
		{
			_methodBase = dynamicMethod;
		}
	}

	protected sealed override Delegate CombineImpl(Delegate? follow)
	{
		if ((object)follow == null)
		{
			return this;
		}
		if (!Delegate.InternalEqualTypes(this, follow))
		{
			throw new ArgumentException(SR.Arg_DlgtTypeMis);
		}
		MulticastDelegate multicastDelegate = (MulticastDelegate)follow;
		int num = 1;
		object[] array = multicastDelegate._invocationList as object[];
		if (array != null)
		{
			num = (int)multicastDelegate._invocationCount;
		}
		int num2;
		object[] array3;
		if (!(_invocationList is object[] array2))
		{
			num2 = 1 + num;
			array3 = new object[num2];
			array3[0] = this;
			if (array == null)
			{
				array3[1] = multicastDelegate;
			}
			else
			{
				for (int i = 0; i < num; i++)
				{
					array3[1 + i] = array[i];
				}
			}
			return NewMulticastDelegate(array3, num2);
		}
		int num3 = (int)_invocationCount;
		num2 = num3 + num;
		array3 = null;
		if (num2 <= array2.Length)
		{
			array3 = array2;
			if (array == null)
			{
				if (!TrySetSlot(array3, num3, multicastDelegate))
				{
					array3 = null;
				}
			}
			else
			{
				for (int j = 0; j < num; j++)
				{
					if (!TrySetSlot(array3, num3 + j, array[j]))
					{
						array3 = null;
						break;
					}
				}
			}
		}
		if (array3 == null)
		{
			int num4;
			for (num4 = array2.Length; num4 < num2; num4 *= 2)
			{
			}
			array3 = new object[num4];
			for (int k = 0; k < num3; k++)
			{
				array3[k] = array2[k];
			}
			if (array == null)
			{
				array3[num3] = multicastDelegate;
			}
			else
			{
				for (int l = 0; l < num; l++)
				{
					array3[num3 + l] = array[l];
				}
			}
		}
		return NewMulticastDelegate(array3, num2, thisIsMultiCastAlready: true);
	}

	private object[] DeleteFromInvocationList(object[] invocationList, int invocationCount, int deleteIndex, int deleteCount)
	{
		object[] array = (object[])_invocationList;
		int num = array.Length;
		while (num / 2 >= invocationCount - deleteCount)
		{
			num /= 2;
		}
		object[] array2 = new object[num];
		for (int i = 0; i < deleteIndex; i++)
		{
			array2[i] = invocationList[i];
		}
		for (int j = deleteIndex + deleteCount; j < invocationCount; j++)
		{
			array2[j - deleteCount] = invocationList[j];
		}
		return array2;
	}

	private static bool EqualInvocationLists(object[] a, object[] b, int start, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (!a[start + i].Equals(b[i]))
			{
				return false;
			}
		}
		return true;
	}

	protected sealed override Delegate? RemoveImpl(Delegate value)
	{
		if (!(value is MulticastDelegate multicastDelegate))
		{
			return this;
		}
		if (!(multicastDelegate._invocationList is object[]))
		{
			if (!(_invocationList is object[] array))
			{
				if (Equals(value))
				{
					return null;
				}
			}
			else
			{
				int num = (int)_invocationCount;
				int num2 = num;
				while (--num2 >= 0)
				{
					if (value.Equals(array[num2]))
					{
						if (num == 2)
						{
							return (Delegate)array[1 - num2];
						}
						object[] invocationList = DeleteFromInvocationList(array, num, num2, 1);
						return NewMulticastDelegate(invocationList, num - 1, thisIsMultiCastAlready: true);
					}
				}
			}
		}
		else if (_invocationList is object[] array2)
		{
			int num3 = (int)_invocationCount;
			int num4 = (int)multicastDelegate._invocationCount;
			for (int num5 = num3 - num4; num5 >= 0; num5--)
			{
				if (EqualInvocationLists(array2, multicastDelegate._invocationList as object[], num5, num4))
				{
					if (num3 - num4 == 0)
					{
						return null;
					}
					if (num3 - num4 == 1)
					{
						return (Delegate)array2[(num5 == 0) ? (num3 - 1) : 0];
					}
					object[] invocationList2 = DeleteFromInvocationList(array2, num3, num5, num4);
					return NewMulticastDelegate(invocationList2, num3 - num4, thisIsMultiCastAlready: true);
				}
			}
		}
		return this;
	}

	public sealed override Delegate[] GetInvocationList()
	{
		Delegate[] array2;
		if (!(_invocationList is object[] array))
		{
			array2 = new Delegate[1] { this };
		}
		else
		{
			array2 = new Delegate[(int)_invocationCount];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (Delegate)array[i];
			}
		}
		return array2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(MulticastDelegate? d1, MulticastDelegate? d2)
	{
		if ((object)d2 == null)
		{
			if ((object)d1 != null)
			{
				return false;
			}
			return true;
		}
		if ((object)d2 != d1)
		{
			return d2.Equals(d1);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(MulticastDelegate? d1, MulticastDelegate? d2)
	{
		if ((object)d2 == null)
		{
			if ((object)d1 != null)
			{
				return true;
			}
			return false;
		}
		if ((object)d2 != d1)
		{
			return !d2.Equals(d1);
		}
		return false;
	}

	public sealed override int GetHashCode()
	{
		if (IsUnmanagedFunctionPtr())
		{
			return ValueType.GetHashCodeOfPtr(_methodPtr) ^ ValueType.GetHashCodeOfPtr(_methodPtrAux);
		}
		if (_invocationCount != (IntPtr)0 && _invocationList is Delegate @delegate)
		{
			return @delegate.GetHashCode();
		}
		if (!(_invocationList is object[] array))
		{
			return base.GetHashCode();
		}
		int num = 0;
		for (int i = 0; i < (int)_invocationCount; i++)
		{
			num = num * 33 + array[i].GetHashCode();
		}
		return num;
	}

	internal override object GetTarget()
	{
		if (_invocationCount != (IntPtr)0)
		{
			if (InvocationListLogicallyNull())
			{
				return null;
			}
			if (_invocationList is object[] array)
			{
				int num = (int)_invocationCount;
				return ((Delegate)array[num - 1]).GetTarget();
			}
			if (_invocationList is Delegate @delegate)
			{
				return @delegate.GetTarget();
			}
		}
		return base.GetTarget();
	}

	protected override MethodInfo GetMethodImpl()
	{
		if (_invocationCount != (IntPtr)0 && _invocationList != null)
		{
			if (_invocationList is object[] array)
			{
				int num = (int)_invocationCount - 1;
				return ((Delegate)array[num]).Method;
			}
			if (_invocationList is MulticastDelegate multicastDelegate)
			{
				return multicastDelegate.GetMethodImpl();
			}
		}
		else if (IsUnmanagedFunctionPtr())
		{
			if (_methodBase == null || !(_methodBase is MethodInfo))
			{
				IRuntimeMethodInfo runtimeMethodInfo = FindMethodHandle();
				RuntimeType runtimeType = RuntimeMethodHandle.GetDeclaringType(runtimeMethodInfo);
				if (RuntimeTypeHandle.IsGenericTypeDefinition(runtimeType) || RuntimeTypeHandle.HasInstantiation(runtimeType))
				{
					RuntimeType runtimeType2 = (RuntimeType)GetType();
					runtimeType = runtimeType2;
				}
				_methodBase = (MethodInfo)RuntimeType.GetMethodBase(runtimeType, runtimeMethodInfo);
			}
			return (MethodInfo)_methodBase;
		}
		return base.GetMethodImpl();
	}

	[DoesNotReturn]
	[DebuggerNonUserCode]
	private static void ThrowNullThisInDelegateToInstance()
	{
		throw new ArgumentException(SR.Arg_DlgtNullInst);
	}

	[DebuggerNonUserCode]
	private void CtorClosed(object target, IntPtr methodPtr)
	{
		if (target == null)
		{
			ThrowNullThisInDelegateToInstance();
		}
		_target = target;
		_methodPtr = methodPtr;
	}

	[DebuggerNonUserCode]
	private void CtorClosedStatic(object target, IntPtr methodPtr)
	{
		_target = target;
		_methodPtr = methodPtr;
	}

	[DebuggerNonUserCode]
	private void CtorRTClosed(object target, IntPtr methodPtr)
	{
		_target = target;
		_methodPtr = AdjustTarget(target, methodPtr);
	}

	[DebuggerNonUserCode]
	private void CtorOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk)
	{
		_target = this;
		_methodPtr = shuffleThunk;
		_methodPtrAux = methodPtr;
	}

	[DebuggerNonUserCode]
	private void CtorVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk)
	{
		_target = this;
		_methodPtr = shuffleThunk;
		_methodPtrAux = GetCallStub(methodPtr);
	}

	[DebuggerNonUserCode]
	private void CtorCollectibleClosedStatic(object target, IntPtr methodPtr, IntPtr gchandle)
	{
		_target = target;
		_methodPtr = methodPtr;
		_methodBase = GCHandle.InternalGet(gchandle);
	}

	[DebuggerNonUserCode]
	private void CtorCollectibleOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
	{
		_target = this;
		_methodPtr = shuffleThunk;
		_methodPtrAux = methodPtr;
		_methodBase = GCHandle.InternalGet(gchandle);
	}

	[DebuggerNonUserCode]
	private void CtorCollectibleVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
	{
		_target = this;
		_methodPtr = shuffleThunk;
		_methodPtrAux = GetCallStub(methodPtr);
		_methodBase = GCHandle.InternalGet(gchandle);
	}
}
