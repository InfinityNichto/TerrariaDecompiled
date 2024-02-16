using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection;

internal sealed class RuntimeParameterInfo : ParameterInfo
{
	private static readonly Type s_DecimalConstantAttributeType = typeof(DecimalConstantAttribute);

	private static readonly Type s_CustomConstantAttributeType = typeof(CustomConstantAttribute);

	private int m_tkParamDef;

	private MetadataImport m_scope;

	private Signature m_signature;

	private volatile bool m_nameIsCached;

	private readonly bool m_noMetadata;

	private bool m_noDefaultValue;

	private MethodBase m_originalMember;

	public override Type ParameterType
	{
		get
		{
			if (ClassImpl == null)
			{
				RuntimeType classImpl = ((PositionImpl != -1) ? m_signature.Arguments[PositionImpl] : m_signature.ReturnType);
				ClassImpl = classImpl;
			}
			return ClassImpl;
		}
	}

	public override string Name
	{
		get
		{
			if (!m_nameIsCached)
			{
				if (!System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
				{
					string nameImpl = m_scope.GetName(m_tkParamDef).ToString();
					NameImpl = nameImpl;
				}
				m_nameIsCached = true;
			}
			return NameImpl;
		}
	}

	public override bool HasDefaultValue
	{
		get
		{
			if (m_noMetadata || m_noDefaultValue)
			{
				return false;
			}
			object defaultValueInternal = GetDefaultValueInternal(raw: false);
			return defaultValueInternal != DBNull.Value;
		}
	}

	public override object DefaultValue => GetDefaultValue(raw: false);

	public override object RawDefaultValue => GetDefaultValue(raw: true);

	public override int MetadataToken => m_tkParamDef;

	internal static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
	{
		ParameterInfo returnParameter;
		return GetParameters(method, member, sig, out returnParameter, fetchReturnParameter: false);
	}

	internal static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
	{
		GetParameters(method, member, sig, out var returnParameter, fetchReturnParameter: true);
		return returnParameter;
	}

	private static ParameterInfo[] GetParameters(IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo returnParameter, bool fetchReturnParameter)
	{
		returnParameter = null;
		int num = sig.Arguments.Length;
		ParameterInfo[] array = (fetchReturnParameter ? null : ((num == 0) ? Array.Empty<ParameterInfo>() : new ParameterInfo[num]));
		int methodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
		int num2 = 0;
		if (!System.Reflection.MetadataToken.IsNullToken(methodDef))
		{
			MetadataImport metadataImport = RuntimeTypeHandle.GetMetadataImport(RuntimeMethodHandle.GetDeclaringType(methodHandle));
			metadataImport.EnumParams(methodDef, out var result);
			num2 = result.Length;
			if (num2 > num + 1)
			{
				throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);
			}
			for (int i = 0; i < num2; i++)
			{
				int num3 = result[i];
				metadataImport.GetParamDefProps(num3, out var sequence, out var attributes);
				sequence--;
				if (fetchReturnParameter && sequence == -1)
				{
					if (returnParameter != null)
					{
						throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);
					}
					returnParameter = new RuntimeParameterInfo(sig, metadataImport, num3, sequence, attributes, member);
				}
				else if (!fetchReturnParameter && sequence >= 0)
				{
					if (sequence >= num)
					{
						throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);
					}
					array[sequence] = new RuntimeParameterInfo(sig, metadataImport, num3, sequence, attributes, member);
				}
			}
		}
		if (fetchReturnParameter)
		{
			if (returnParameter == null)
			{
				returnParameter = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, -1, ParameterAttributes.None, member);
			}
		}
		else if (num2 < array.Length + 1)
		{
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] == null)
				{
					array[j] = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, j, ParameterAttributes.None, member);
				}
			}
		}
		return array;
	}

	internal void SetName(string name)
	{
		NameImpl = name;
	}

	internal void SetAttributes(ParameterAttributes attributes)
	{
		AttrsImpl = attributes;
	}

	internal RuntimeParameterInfo(RuntimeParameterInfo accessor, RuntimePropertyInfo property)
		: this(accessor, (MemberInfo)property)
	{
		m_signature = property.Signature;
	}

	private RuntimeParameterInfo(RuntimeParameterInfo accessor, MemberInfo member)
	{
		MemberImpl = member;
		m_originalMember = accessor.MemberImpl as MethodBase;
		NameImpl = accessor.Name;
		m_nameIsCached = true;
		ClassImpl = accessor.ParameterType;
		PositionImpl = accessor.Position;
		AttrsImpl = accessor.Attributes;
		m_tkParamDef = (System.Reflection.MetadataToken.IsNullToken(accessor.MetadataToken) ? 134217728 : accessor.MetadataToken);
		m_scope = accessor.m_scope;
	}

	private RuntimeParameterInfo(Signature signature, MetadataImport scope, int tkParamDef, int position, ParameterAttributes attributes, MemberInfo member)
	{
		PositionImpl = position;
		MemberImpl = member;
		m_signature = signature;
		m_tkParamDef = (System.Reflection.MetadataToken.IsNullToken(tkParamDef) ? 134217728 : tkParamDef);
		m_scope = scope;
		AttrsImpl = attributes;
		ClassImpl = null;
		NameImpl = null;
	}

	internal RuntimeParameterInfo(MethodInfo owner, string name, Type parameterType, int position)
	{
		MemberImpl = owner;
		NameImpl = name;
		m_nameIsCached = true;
		m_noMetadata = true;
		ClassImpl = parameterType;
		PositionImpl = position;
		AttrsImpl = ParameterAttributes.None;
		m_tkParamDef = 134217728;
		m_scope = MetadataImport.EmptyImport;
	}

	private object GetDefaultValue(bool raw)
	{
		if (m_noMetadata)
		{
			return null;
		}
		object obj = GetDefaultValueInternal(raw);
		if (obj == DBNull.Value && base.IsOptional)
		{
			obj = Type.Missing;
		}
		return obj;
	}

	private object GetDefaultValueInternal(bool raw)
	{
		if (m_noDefaultValue)
		{
			return DBNull.Value;
		}
		object obj = null;
		if (ParameterType == typeof(DateTime))
		{
			if (raw)
			{
				CustomAttributeTypedArgument customAttributeTypedArgument = RuntimeCustomAttributeData.Filter(CustomAttributeData.GetCustomAttributes(this), typeof(DateTimeConstantAttribute), 0);
				if (customAttributeTypedArgument.ArgumentType != null)
				{
					return new DateTime((long)customAttributeTypedArgument.Value);
				}
			}
			else
			{
				object[] customAttributes = GetCustomAttributes(typeof(DateTimeConstantAttribute), inherit: false);
				if (customAttributes != null && customAttributes.Length != 0)
				{
					return ((DateTimeConstantAttribute)customAttributes[0]).Value;
				}
			}
		}
		if (!System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			obj = MdConstant.GetValue(m_scope, m_tkParamDef, ParameterType.GetTypeHandleInternal(), raw);
		}
		if (obj == DBNull.Value)
		{
			if (raw)
			{
				foreach (CustomAttributeData customAttribute in CustomAttributeData.GetCustomAttributes(this))
				{
					Type declaringType = customAttribute.Constructor.DeclaringType;
					if (declaringType == typeof(DateTimeConstantAttribute))
					{
						obj = GetRawDateTimeConstant(customAttribute);
					}
					else if (declaringType == typeof(DecimalConstantAttribute))
					{
						obj = GetRawDecimalConstant(customAttribute);
					}
					else if (declaringType.IsSubclassOf(s_CustomConstantAttributeType))
					{
						obj = GetRawConstant(customAttribute);
					}
				}
			}
			else
			{
				object[] customAttributes2 = GetCustomAttributes(s_CustomConstantAttributeType, inherit: false);
				if (customAttributes2.Length != 0)
				{
					obj = ((CustomConstantAttribute)customAttributes2[0]).Value;
				}
				else
				{
					customAttributes2 = GetCustomAttributes(s_DecimalConstantAttributeType, inherit: false);
					if (customAttributes2.Length != 0)
					{
						obj = ((DecimalConstantAttribute)customAttributes2[0]).Value;
					}
				}
			}
		}
		if (obj == DBNull.Value)
		{
			m_noDefaultValue = true;
		}
		return obj;
	}

	private static decimal GetRawDecimalConstant(CustomAttributeData attr)
	{
		foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
		{
			if (namedArgument.MemberInfo.Name.Equals("Value"))
			{
				return (decimal)namedArgument.TypedValue.Value;
			}
		}
		ParameterInfo[] parameters = attr.Constructor.GetParameters();
		IList<CustomAttributeTypedArgument> constructorArguments = attr.ConstructorArguments;
		if (parameters[2].ParameterType == typeof(uint))
		{
			int lo = (int)(uint)constructorArguments[4].Value;
			int mid = (int)(uint)constructorArguments[3].Value;
			int hi = (int)(uint)constructorArguments[2].Value;
			byte b = (byte)constructorArguments[1].Value;
			byte scale = (byte)constructorArguments[0].Value;
			return new decimal(lo, mid, hi, b != 0, scale);
		}
		int lo2 = (int)constructorArguments[4].Value;
		int mid2 = (int)constructorArguments[3].Value;
		int hi2 = (int)constructorArguments[2].Value;
		byte b2 = (byte)constructorArguments[1].Value;
		byte scale2 = (byte)constructorArguments[0].Value;
		return new decimal(lo2, mid2, hi2, b2 != 0, scale2);
	}

	private static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
	{
		foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
		{
			if (namedArgument.MemberInfo.Name.Equals("Value"))
			{
				return new DateTime((long)namedArgument.TypedValue.Value);
			}
		}
		return new DateTime((long)attr.ConstructorArguments[0].Value);
	}

	private static object GetRawConstant(CustomAttributeData attr)
	{
		foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
		{
			if (namedArgument.MemberInfo.Name.Equals("Value"))
			{
				return namedArgument.TypedValue.Value;
			}
		}
		return DBNull.Value;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		RuntimeMethodInfo runtimeMethodInfo = Member as RuntimeMethodInfo;
		RuntimeConstructorInfo runtimeConstructorInfo = Member as RuntimeConstructorInfo;
		RuntimePropertyInfo runtimePropertyInfo = Member as RuntimePropertyInfo;
		if (runtimeMethodInfo != null)
		{
			return runtimeMethodInfo.GetRuntimeModule();
		}
		if (runtimeConstructorInfo != null)
		{
			return runtimeConstructorInfo.GetRuntimeModule();
		}
		if (runtimePropertyInfo != null)
		{
			return runtimePropertyInfo.GetRuntimeModule();
		}
		return null;
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		if (m_signature != null)
		{
			return m_signature.GetCustomModifiers(PositionImpl + 1, required: true);
		}
		return Type.EmptyTypes;
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		if (m_signature != null)
		{
			return m_signature.GetCustomModifiers(PositionImpl + 1, required: false);
		}
		return Type.EmptyTypes;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		if (System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			return Array.Empty<object>();
		}
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			return Array.Empty<object>();
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, runtimeType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (System.Reflection.MetadataToken.IsNullToken(m_tkParamDef))
		{
			return false;
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, runtimeType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}
}
