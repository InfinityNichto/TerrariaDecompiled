using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class ConstructorOnTypeBuilderInstantiation : ConstructorInfo
{
	internal ConstructorInfo m_ctor;

	private TypeBuilderInstantiation m_type;

	public override MemberTypes MemberType => m_ctor.MemberType;

	public override string Name => m_ctor.Name;

	public override Type DeclaringType => m_type;

	public override Type ReflectedType => m_type;

	public override int MetadataToken
	{
		get
		{
			ConstructorBuilder constructorBuilder = m_ctor as ConstructorBuilder;
			if (constructorBuilder != null)
			{
				return constructorBuilder.MetadataToken;
			}
			return m_ctor.MetadataToken;
		}
	}

	public override Module Module => m_ctor.Module;

	public override RuntimeMethodHandle MethodHandle => m_ctor.MethodHandle;

	public override MethodAttributes Attributes => m_ctor.Attributes;

	public override CallingConventions CallingConvention => m_ctor.CallingConvention;

	public override bool IsGenericMethodDefinition => false;

	public override bool ContainsGenericParameters => false;

	public override bool IsGenericMethod => false;

	internal static ConstructorInfo GetConstructor(ConstructorInfo Constructor, TypeBuilderInstantiation type)
	{
		return new ConstructorOnTypeBuilderInstantiation(Constructor, type);
	}

	internal ConstructorOnTypeBuilderInstantiation(ConstructorInfo constructor, TypeBuilderInstantiation type)
	{
		m_ctor = constructor;
		m_type = type;
	}

	internal override Type[] GetParameterTypes()
	{
		return m_ctor.GetParameterTypes();
	}

	internal override Type GetReturnType()
	{
		return m_type;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return m_ctor.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return m_ctor.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return m_ctor.IsDefined(attributeType, inherit);
	}

	public override ParameterInfo[] GetParameters()
	{
		return m_ctor.GetParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return m_ctor.GetMethodImplementationFlags();
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

	public override Type[] GetGenericArguments()
	{
		return m_ctor.GetGenericArguments();
	}

	public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new InvalidOperationException();
	}
}
