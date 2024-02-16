using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Reflection;

public abstract class MethodBase : MemberInfo
{
	private protected struct StackAllocedArguments
	{
		internal object _arg0;

		private object _arg1;

		private object _arg2;

		private object _arg3;
	}

	public abstract MethodAttributes Attributes { get; }

	public virtual MethodImplAttributes MethodImplementationFlags => GetMethodImplementationFlags();

	public virtual CallingConventions CallingConvention => CallingConventions.Standard;

	public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;

	public bool IsConstructor
	{
		get
		{
			if (this is ConstructorInfo && !IsStatic)
			{
				return (Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName;
			}
			return false;
		}
	}

	public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;

	public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) != 0;

	public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;

	public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;

	public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

	public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

	public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

	public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;

	public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

	public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

	public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

	public virtual bool IsConstructedGenericMethod
	{
		get
		{
			if (IsGenericMethod)
			{
				return !IsGenericMethodDefinition;
			}
			return false;
		}
	}

	public virtual bool IsGenericMethod => false;

	public virtual bool IsGenericMethodDefinition => false;

	public virtual bool ContainsGenericParameters => false;

	public abstract RuntimeMethodHandle MethodHandle { get; }

	public virtual bool IsSecurityCritical
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsSecuritySafeCritical
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsSecurityTransparent
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle)
	{
		if (handle.IsNullHandle())
		{
			throw new ArgumentException(SR.Argument_InvalidHandle);
		}
		MethodBase methodBase = RuntimeType.GetMethodBase(handle.GetMethodInfo());
		Type type = methodBase?.DeclaringType;
		if (type != null && type.IsGenericType)
		{
			throw new ArgumentException(SR.Format(SR.Argument_MethodDeclaringTypeGeneric, methodBase, type.GetGenericTypeDefinition()));
		}
		return methodBase;
	}

	public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
	{
		if (handle.IsNullHandle())
		{
			throw new ArgumentException(SR.Argument_InvalidHandle);
		}
		return RuntimeType.GetMethodBase(declaringType.GetRuntimeType(), handle.GetMethodInfo());
	}

	public static MethodBase? GetCurrentMethod()
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeMethodInfo.InternalGetCurrentMethod(ref stackMark);
	}

	private IntPtr GetMethodDesc()
	{
		return MethodHandle.Value;
	}

	internal virtual ParameterInfo[] GetParametersNoCopy()
	{
		return GetParameters();
	}

	internal virtual Type[] GetParameterTypes()
	{
		ParameterInfo[] parametersNoCopy = GetParametersNoCopy();
		Type[] array = new Type[parametersNoCopy.Length];
		for (int i = 0; i < parametersNoCopy.Length; i++)
		{
			array[i] = parametersNoCopy[i].ParameterType;
		}
		return array;
	}

	private protected Span<object> CheckArguments(ref StackAllocedArguments stackArgs, ReadOnlySpan<object> parameters, Binder binder, BindingFlags invokeAttr, CultureInfo culture, Signature sig)
	{
		Span<object> result = ((parameters.Length <= 4) ? MemoryMarshal.CreateSpan(ref stackArgs._arg0, parameters.Length) : new Span<object>(new object[parameters.Length]));
		ParameterInfo[] array = null;
		for (int i = 0; i < parameters.Length; i++)
		{
			object obj = parameters[i];
			RuntimeType runtimeType = sig.Arguments[i];
			if (obj == Type.Missing)
			{
				if (array == null)
				{
					array = GetParametersNoCopy();
				}
				if (array[i].DefaultValue == DBNull.Value)
				{
					throw new ArgumentException(SR.Arg_VarMissNull, "parameters");
				}
				obj = array[i].DefaultValue;
			}
			result[i] = runtimeType.CheckValue(obj, binder, culture, invokeAttr);
		}
		return result;
	}

	public abstract ParameterInfo[] GetParameters();

	public abstract MethodImplAttributes GetMethodImplementationFlags();

	[RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
	public virtual MethodBody? GetMethodBody()
	{
		throw new InvalidOperationException();
	}

	public virtual Type[] GetGenericArguments()
	{
		throw new NotSupportedException(SR.NotSupported_SubclassOverride);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public object? Invoke(object? obj, object?[]? parameters)
	{
		return Invoke(obj, BindingFlags.Default, null, parameters, null);
	}

	public abstract object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

	public override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(MethodBase? left, MethodBase? right)
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

	public static bool operator !=(MethodBase? left, MethodBase? right)
	{
		return !(left == right);
	}

	internal static void AppendParameters(ref ValueStringBuilder sbParamList, Type[] parameterTypes, CallingConventions callingConvention)
	{
		string s = "";
		foreach (Type type in parameterTypes)
		{
			sbParamList.Append(s);
			string text = type.FormatTypeName();
			if (type.IsByRef)
			{
				sbParamList.Append(text.AsSpan().TrimEnd('&'));
				sbParamList.Append(" ByRef");
			}
			else
			{
				sbParamList.Append(text);
			}
			s = ", ";
		}
		if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			sbParamList.Append(s);
			sbParamList.Append("...");
		}
	}
}
