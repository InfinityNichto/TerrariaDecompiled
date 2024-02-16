using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection;

internal sealed class RuntimeCustomAttributeData : CustomAttributeData
{
	private ConstructorInfo m_ctor;

	private readonly RuntimeModule m_scope;

	private readonly MemberInfo[] m_members;

	private readonly CustomAttributeCtorParameter[] m_ctorParams;

	private readonly CustomAttributeNamedParameter[] m_namedParams;

	private IList<CustomAttributeTypedArgument> m_typedCtorArgs;

	private IList<CustomAttributeNamedArgument> m_namedArgs;

	public override ConstructorInfo Constructor => m_ctor;

	public override IList<CustomAttributeTypedArgument> ConstructorArguments
	{
		get
		{
			if (m_typedCtorArgs == null)
			{
				CustomAttributeTypedArgument[] array = new CustomAttributeTypedArgument[m_ctorParams.Length];
				for (int i = 0; i < array.Length; i++)
				{
					CustomAttributeEncodedArgument customAttributeEncodedArgument = m_ctorParams[i].CustomAttributeEncodedArgument;
					array[i] = new CustomAttributeTypedArgument(m_scope, customAttributeEncodedArgument);
				}
				m_typedCtorArgs = Array.AsReadOnly(array);
			}
			return m_typedCtorArgs;
		}
	}

	public override IList<CustomAttributeNamedArgument> NamedArguments
	{
		get
		{
			if (m_namedArgs == null)
			{
				if (m_namedParams == null)
				{
					return null;
				}
				int num = 0;
				for (int i = 0; i < m_namedParams.Length; i++)
				{
					if (m_namedParams[i].EncodedArgument.CustomAttributeType.EncodedType != 0)
					{
						num++;
					}
				}
				CustomAttributeNamedArgument[] array = new CustomAttributeNamedArgument[num];
				int j = 0;
				int num2 = 0;
				for (; j < m_namedParams.Length; j++)
				{
					if (m_namedParams[j].EncodedArgument.CustomAttributeType.EncodedType != 0)
					{
						array[num2++] = new CustomAttributeNamedArgument(m_members[j], new CustomAttributeTypedArgument(m_scope, m_namedParams[j].EncodedArgument));
					}
				}
				m_namedArgs = Array.AsReadOnly(array);
			}
			return m_namedArgs;
		}
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
	{
		IList<CustomAttributeData> customAttributes = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
		if (pcas.Count <= 0)
		{
			return customAttributes;
		}
		return GetCombinedList(customAttributes, ref pcas);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeFieldInfo target)
	{
		IList<CustomAttributeData> customAttributes = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
		if (pcas.Count <= 0)
		{
			return customAttributes;
		}
		return GetCombinedList(customAttributes, ref pcas);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeMethodInfo target)
	{
		IList<CustomAttributeData> customAttributes = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
		if (pcas.Count <= 0)
		{
			return customAttributes;
		}
		return GetCombinedList(customAttributes, ref pcas);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeConstructorInfo target)
	{
		return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeEventInfo target)
	{
		return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimePropertyInfo target)
	{
		return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeModule target)
	{
		if (target.IsResource())
		{
			return new List<CustomAttributeData>();
		}
		return GetCustomAttributes(target, target.MetadataToken);
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeAssembly target)
	{
		return GetCustomAttributes((RuntimeModule)target.ManifestModule, RuntimeAssembly.GetToken(target.GetNativeHandle()));
	}

	internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeParameterInfo target)
	{
		RuntimeType.ListBuilder<Attribute> pcas = default(RuntimeType.ListBuilder<Attribute>);
		IList<CustomAttributeData> customAttributes = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
		PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
		if (pcas.Count <= 0)
		{
			return customAttributes;
		}
		return GetCombinedList(customAttributes, ref pcas);
	}

	private static IList<CustomAttributeData> GetCombinedList(IList<CustomAttributeData> customAttributes, ref RuntimeType.ListBuilder<Attribute> pseudoAttributes)
	{
		CustomAttributeData[] array = new CustomAttributeData[customAttributes.Count + pseudoAttributes.Count];
		customAttributes.CopyTo(array, pseudoAttributes.Count);
		for (int i = 0; i < pseudoAttributes.Count; i++)
		{
			array[i] = new RuntimeCustomAttributeData(pseudoAttributes[i]);
		}
		return Array.AsReadOnly(array);
	}

	private static CustomAttributeEncoding TypeToCustomAttributeEncoding(RuntimeType type)
	{
		if (type == typeof(int))
		{
			return CustomAttributeEncoding.Int32;
		}
		if (type.IsEnum)
		{
			return CustomAttributeEncoding.Enum;
		}
		if (type == typeof(string))
		{
			return CustomAttributeEncoding.String;
		}
		if (type == typeof(Type))
		{
			return CustomAttributeEncoding.Type;
		}
		if (type == typeof(object))
		{
			return CustomAttributeEncoding.Object;
		}
		if (type.IsArray)
		{
			return CustomAttributeEncoding.Array;
		}
		if (type == typeof(char))
		{
			return CustomAttributeEncoding.Char;
		}
		if (type == typeof(bool))
		{
			return CustomAttributeEncoding.Boolean;
		}
		if (type == typeof(byte))
		{
			return CustomAttributeEncoding.Byte;
		}
		if (type == typeof(sbyte))
		{
			return CustomAttributeEncoding.SByte;
		}
		if (type == typeof(short))
		{
			return CustomAttributeEncoding.Int16;
		}
		if (type == typeof(ushort))
		{
			return CustomAttributeEncoding.UInt16;
		}
		if (type == typeof(uint))
		{
			return CustomAttributeEncoding.UInt32;
		}
		if (type == typeof(long))
		{
			return CustomAttributeEncoding.Int64;
		}
		if (type == typeof(ulong))
		{
			return CustomAttributeEncoding.UInt64;
		}
		if (type == typeof(float))
		{
			return CustomAttributeEncoding.Float;
		}
		if (type == typeof(double))
		{
			return CustomAttributeEncoding.Double;
		}
		if (type == typeof(Enum))
		{
			return CustomAttributeEncoding.Object;
		}
		if (type.IsClass)
		{
			return CustomAttributeEncoding.Object;
		}
		if (type.IsInterface)
		{
			return CustomAttributeEncoding.Object;
		}
		if (type.IsValueType)
		{
			return CustomAttributeEncoding.Undefined;
		}
		throw new ArgumentException(SR.Argument_InvalidKindOfTypeForCA, "type");
	}

	private static CustomAttributeType InitCustomAttributeType(RuntimeType parameterType)
	{
		CustomAttributeEncoding customAttributeEncoding = TypeToCustomAttributeEncoding(parameterType);
		CustomAttributeEncoding customAttributeEncoding2 = CustomAttributeEncoding.Undefined;
		CustomAttributeEncoding encodedEnumType = CustomAttributeEncoding.Undefined;
		string enumName = null;
		if (customAttributeEncoding == CustomAttributeEncoding.Array)
		{
			parameterType = (RuntimeType)parameterType.GetElementType();
			customAttributeEncoding2 = TypeToCustomAttributeEncoding(parameterType);
		}
		if (customAttributeEncoding == CustomAttributeEncoding.Enum || customAttributeEncoding2 == CustomAttributeEncoding.Enum)
		{
			encodedEnumType = TypeToCustomAttributeEncoding((RuntimeType)Enum.GetUnderlyingType(parameterType));
			enumName = parameterType.AssemblyQualifiedName;
		}
		return new CustomAttributeType(customAttributeEncoding, customAttributeEncoding2, encodedEnumType, enumName);
	}

	private static IList<CustomAttributeData> GetCustomAttributes(RuntimeModule module, int tkTarget)
	{
		CustomAttributeRecord[] customAttributeRecords = GetCustomAttributeRecords(module, tkTarget);
		if (customAttributeRecords.Length == 0)
		{
			return Array.Empty<CustomAttributeData>();
		}
		CustomAttributeData[] array = new CustomAttributeData[customAttributeRecords.Length];
		for (int i = 0; i < customAttributeRecords.Length; i++)
		{
			array[i] = new RuntimeCustomAttributeData(module, customAttributeRecords[i].tkCtor, in customAttributeRecords[i].blob);
		}
		return Array.AsReadOnly(array);
	}

	internal static CustomAttributeRecord[] GetCustomAttributeRecords(RuntimeModule module, int targetToken)
	{
		MetadataImport metadataImport = module.MetadataImport;
		metadataImport.EnumCustomAttributes(targetToken, out var result);
		if (result.Length == 0)
		{
			return Array.Empty<CustomAttributeRecord>();
		}
		CustomAttributeRecord[] array = new CustomAttributeRecord[result.Length];
		for (int i = 0; i < array.Length; i++)
		{
			metadataImport.GetCustomAttributeProps(result[i], out array[i].tkCtor.Value, out array[i].blob);
		}
		return array;
	}

	internal static CustomAttributeTypedArgument Filter(IList<CustomAttributeData> attrs, Type caType, int parameter)
	{
		for (int i = 0; i < attrs.Count; i++)
		{
			if (attrs[i].Constructor.DeclaringType == caType)
			{
				return attrs[i].ConstructorArguments[parameter];
			}
		}
		return default(CustomAttributeTypedArgument);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "Property setters and fields which are accessed by any attribute instantiation which is present in the code linker has analyzed.As such enumerating all fields and properties may return different results after trimmingbut all those which are needed to actually have data will be there.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "We're getting a MethodBase of a constructor that we found in the metadata. The attribute constructor won't be trimmed.")]
	private RuntimeCustomAttributeData(RuntimeModule scope, MetadataToken caCtorToken, in ConstArray blob)
	{
		m_scope = scope;
		m_ctor = (RuntimeConstructorInfo)RuntimeType.GetMethodBase(scope, caCtorToken);
		if (m_ctor.DeclaringType.IsGenericType)
		{
			Type type = scope.ResolveType(scope.MetadataImport.GetParentToken(caCtorToken), null, null);
			m_ctor = (RuntimeConstructorInfo)scope.ResolveMethod(caCtorToken, type.GenericTypeArguments, null).MethodHandle.GetMethodInfo();
		}
		ParameterInfo[] parametersNoCopy = m_ctor.GetParametersNoCopy();
		m_ctorParams = new CustomAttributeCtorParameter[parametersNoCopy.Length];
		for (int i = 0; i < parametersNoCopy.Length; i++)
		{
			m_ctorParams[i] = new CustomAttributeCtorParameter(InitCustomAttributeType((RuntimeType)parametersNoCopy[i].ParameterType));
		}
		FieldInfo[] fields = m_ctor.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		PropertyInfo[] properties = m_ctor.DeclaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		m_namedParams = new CustomAttributeNamedParameter[properties.Length + fields.Length];
		for (int j = 0; j < fields.Length; j++)
		{
			m_namedParams[j] = new CustomAttributeNamedParameter(fields[j].Name, CustomAttributeEncoding.Field, InitCustomAttributeType((RuntimeType)fields[j].FieldType));
		}
		for (int k = 0; k < properties.Length; k++)
		{
			m_namedParams[k + fields.Length] = new CustomAttributeNamedParameter(properties[k].Name, CustomAttributeEncoding.Property, InitCustomAttributeType((RuntimeType)properties[k].PropertyType));
		}
		m_members = new MemberInfo[fields.Length + properties.Length];
		fields.CopyTo(m_members, 0);
		properties.CopyTo(m_members, fields.Length);
		CustomAttributeEncodedArgument.ParseAttributeArguments(blob, ref m_ctorParams, ref m_namedParams, m_scope);
	}

	internal RuntimeCustomAttributeData(Attribute attribute)
	{
		if (attribute is DllImportAttribute dllImport)
		{
			Init(dllImport);
		}
		else if (attribute is FieldOffsetAttribute fieldOffset)
		{
			Init(fieldOffset);
		}
		else if (attribute is MarshalAsAttribute marshalAs)
		{
			Init(marshalAs);
		}
		else if (attribute is TypeForwardedToAttribute forwardedTo)
		{
			Init(forwardedTo);
		}
		else
		{
			Init(attribute);
		}
	}

	private void Init(DllImportAttribute dllImport)
	{
		Type typeFromHandle = typeof(DllImportAttribute);
		m_ctor = typeFromHandle.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
		m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[1]
		{
			new CustomAttributeTypedArgument(dllImport.Value)
		});
		m_namedArgs = Array.AsReadOnly(new CustomAttributeNamedArgument[8]
		{
			new CustomAttributeNamedArgument(typeFromHandle.GetField("EntryPoint"), dllImport.EntryPoint),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("CharSet"), dllImport.CharSet),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("ExactSpelling"), dllImport.ExactSpelling),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("SetLastError"), dllImport.SetLastError),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("PreserveSig"), dllImport.PreserveSig),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("CallingConvention"), dllImport.CallingConvention),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("BestFitMapping"), dllImport.BestFitMapping),
			new CustomAttributeNamedArgument(typeFromHandle.GetField("ThrowOnUnmappableChar"), dllImport.ThrowOnUnmappableChar)
		});
	}

	private void Init(FieldOffsetAttribute fieldOffset)
	{
		m_ctor = typeof(FieldOffsetAttribute).GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
		m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[1]
		{
			new CustomAttributeTypedArgument(fieldOffset.Value)
		});
		m_namedArgs = Array.AsReadOnly(Array.Empty<CustomAttributeNamedArgument>());
	}

	private void Init(MarshalAsAttribute marshalAs)
	{
		Type typeFromHandle = typeof(MarshalAsAttribute);
		m_ctor = typeFromHandle.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
		m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[1]
		{
			new CustomAttributeTypedArgument(marshalAs.Value)
		});
		int num = 3;
		if (marshalAs.MarshalType != null)
		{
			num++;
		}
		if ((object)marshalAs.MarshalTypeRef != null)
		{
			num++;
		}
		if (marshalAs.MarshalCookie != null)
		{
			num++;
		}
		num++;
		num++;
		if ((object)marshalAs.SafeArrayUserDefinedSubType != null)
		{
			num++;
		}
		CustomAttributeNamedArgument[] array = new CustomAttributeNamedArgument[num];
		num = 0;
		array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("ArraySubType"), marshalAs.ArraySubType);
		array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("SizeParamIndex"), marshalAs.SizeParamIndex);
		array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("SizeConst"), marshalAs.SizeConst);
		array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("IidParameterIndex"), marshalAs.IidParameterIndex);
		array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("SafeArraySubType"), marshalAs.SafeArraySubType);
		if (marshalAs.MarshalType != null)
		{
			array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("MarshalType"), marshalAs.MarshalType);
		}
		if ((object)marshalAs.MarshalTypeRef != null)
		{
			array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("MarshalTypeRef"), marshalAs.MarshalTypeRef);
		}
		if (marshalAs.MarshalCookie != null)
		{
			array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("MarshalCookie"), marshalAs.MarshalCookie);
		}
		if ((object)marshalAs.SafeArrayUserDefinedSubType != null)
		{
			array[num++] = new CustomAttributeNamedArgument(typeFromHandle.GetField("SafeArrayUserDefinedSubType"), marshalAs.SafeArrayUserDefinedSubType);
		}
		m_namedArgs = Array.AsReadOnly(array);
	}

	private void Init(TypeForwardedToAttribute forwardedTo)
	{
		Type typeFromHandle = typeof(TypeForwardedToAttribute);
		Type[] types = new Type[1] { typeof(Type) };
		m_ctor = typeFromHandle.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, types, null);
		m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[1]
		{
			new CustomAttributeTypedArgument(typeof(Type), forwardedTo.Destination)
		});
		CustomAttributeNamedArgument[] array = Array.Empty<CustomAttributeNamedArgument>();
		m_namedArgs = Array.AsReadOnly(array);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "The pca object had to be created by the single ctor on the Type. So the ctor couldn't have been trimmed.")]
	private void Init(object pca)
	{
		Type type = pca.GetType();
		m_ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
		m_typedCtorArgs = Array.AsReadOnly(Array.Empty<CustomAttributeTypedArgument>());
		m_namedArgs = Array.AsReadOnly(Array.Empty<CustomAttributeNamedArgument>());
	}
}
