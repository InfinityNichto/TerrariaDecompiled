using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class MethodOnTypeBuilderInstantiation : MethodInfo
{
	internal MethodInfo m_method;

	private TypeBuilderInstantiation m_type;

	public override MemberTypes MemberType => m_method.MemberType;

	public override string Name => m_method.Name;

	public override Type DeclaringType => m_type;

	public override Type ReflectedType => m_type;

	public override Module Module => m_method.Module;

	public override RuntimeMethodHandle MethodHandle => m_method.MethodHandle;

	public override MethodAttributes Attributes => m_method.Attributes;

	public override CallingConventions CallingConvention => m_method.CallingConvention;

	public override bool IsGenericMethodDefinition => m_method.IsGenericMethodDefinition;

	public override bool ContainsGenericParameters => m_method.ContainsGenericParameters;

	public override bool IsGenericMethod => m_method.IsGenericMethod;

	public override Type ReturnType => m_method.ReturnType;

	public override ParameterInfo ReturnParameter
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override ICustomAttributeProvider ReturnTypeCustomAttributes
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
	{
		return new MethodOnTypeBuilderInstantiation(method, type);
	}

	internal MethodOnTypeBuilderInstantiation(MethodInfo method, TypeBuilderInstantiation type)
	{
		m_method = method;
		m_type = type;
	}

	internal override Type[] GetParameterTypes()
	{
		return m_method.GetParameterTypes();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return m_method.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return m_method.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return m_method.IsDefined(attributeType, inherit);
	}

	public override ParameterInfo[] GetParameters()
	{
		return m_method.GetParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return m_method.GetMethodImplementationFlags();
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

	public override Type[] GetGenericArguments()
	{
		return m_method.GetGenericArguments();
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		return m_method;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
	{
		if (!IsGenericMethodDefinition)
		{
			throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
		}
		return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
	}

	public override MethodInfo GetBaseDefinition()
	{
		throw new NotSupportedException();
	}
}
