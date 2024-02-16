using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public sealed class NullabilityInfoContext
{
	[Flags]
	private enum NotAnnotatedStatus
	{
		None = 0,
		Private = 1,
		Internal = 2
	}

	private readonly Dictionary<Module, NotAnnotatedStatus> _publicOnlyModules = new Dictionary<Module, NotAnnotatedStatus>();

	private readonly Dictionary<MemberInfo, NullabilityState> _context = new Dictionary<MemberInfo, NullabilityState>();

	internal static bool IsSupported { get; } = !AppContext.TryGetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", out var isEnabled) || isEnabled;


	private NullabilityState GetNullableContext(MemberInfo memberInfo)
	{
		while (memberInfo != null)
		{
			if (_context.TryGetValue(memberInfo, out var value))
			{
				return value;
			}
			foreach (CustomAttributeData customAttributesDatum in memberInfo.GetCustomAttributesData())
			{
				if (customAttributesDatum.AttributeType.Name == "NullableContextAttribute" && customAttributesDatum.AttributeType.Namespace == "System.Runtime.CompilerServices" && customAttributesDatum.ConstructorArguments.Count == 1)
				{
					value = TranslateByte(customAttributesDatum.ConstructorArguments[0].Value);
					_context.Add(memberInfo, value);
					return value;
				}
			}
			memberInfo = memberInfo.DeclaringType;
		}
		return NullabilityState.Unknown;
	}

	public NullabilityInfo Create(ParameterInfo parameterInfo)
	{
		if (parameterInfo == null)
		{
			throw new ArgumentNullException("parameterInfo");
		}
		EnsureIsSupported();
		if (parameterInfo.Member is MethodInfo method && IsPrivateOrInternalMethodAndAnnotationDisabled(method))
		{
			return new NullabilityInfo(parameterInfo.ParameterType, NullabilityState.Unknown, NullabilityState.Unknown, null, Array.Empty<NullabilityInfo>());
		}
		IList<CustomAttributeData> customAttributesData = parameterInfo.GetCustomAttributesData();
		NullabilityInfo nullabilityInfo = GetNullabilityInfo(parameterInfo.Member, parameterInfo.ParameterType, customAttributesData);
		if (nullabilityInfo.ReadState != 0)
		{
			CheckParameterMetadataType(parameterInfo, nullabilityInfo);
		}
		CheckNullabilityAttributes(nullabilityInfo, customAttributesData);
		return nullabilityInfo;
	}

	private void CheckParameterMetadataType(ParameterInfo parameter, NullabilityInfo nullability)
	{
		if (!(parameter.Member is MethodInfo method))
		{
			return;
		}
		MethodInfo methodMetadataDefinition = GetMethodMetadataDefinition(method);
		ParameterInfo parameterInfo = null;
		if (string.IsNullOrEmpty(parameter.Name))
		{
			parameterInfo = methodMetadataDefinition.ReturnParameter;
		}
		else
		{
			ParameterInfo[] parameters = methodMetadataDefinition.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameter.Position == i && parameter.Name == parameters[i].Name)
				{
					parameterInfo = parameters[i];
					break;
				}
			}
		}
		if (parameterInfo != null)
		{
			CheckGenericParameters(nullability, methodMetadataDefinition, parameterInfo.ParameterType);
		}
	}

	private static MethodInfo GetMethodMetadataDefinition(MethodInfo method)
	{
		if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
		{
			method = method.GetGenericMethodDefinition();
		}
		return (MethodInfo)GetMemberMetadataDefinition(method);
	}

	private void CheckNullabilityAttributes(NullabilityInfo nullability, IList<CustomAttributeData> attributes)
	{
		foreach (CustomAttributeData attribute in attributes)
		{
			if (attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
			{
				if (attribute.AttributeType.Name == "NotNullAttribute" && nullability.ReadState == NullabilityState.Nullable)
				{
					nullability.ReadState = NullabilityState.NotNull;
					break;
				}
				if ((attribute.AttributeType.Name == "MaybeNullAttribute" || attribute.AttributeType.Name == "MaybeNullWhenAttribute") && nullability.ReadState == NullabilityState.NotNull && !nullability.Type.IsValueType)
				{
					nullability.ReadState = NullabilityState.Nullable;
					break;
				}
				if (attribute.AttributeType.Name == "DisallowNullAttribute" && nullability.WriteState == NullabilityState.Nullable)
				{
					nullability.WriteState = NullabilityState.NotNull;
					break;
				}
				if (attribute.AttributeType.Name == "AllowNullAttribute" && nullability.WriteState == NullabilityState.NotNull && !nullability.Type.IsValueType)
				{
					nullability.WriteState = NullabilityState.Nullable;
					break;
				}
			}
		}
	}

	public NullabilityInfo Create(PropertyInfo propertyInfo)
	{
		if ((object)propertyInfo == null)
		{
			throw new ArgumentNullException("propertyInfo");
		}
		EnsureIsSupported();
		NullabilityInfo nullabilityInfo = GetNullabilityInfo(propertyInfo, propertyInfo.PropertyType, propertyInfo.GetCustomAttributesData());
		MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: true);
		MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
		if (getMethod != null)
		{
			if (IsPrivateOrInternalMethodAndAnnotationDisabled(getMethod))
			{
				nullabilityInfo.ReadState = NullabilityState.Unknown;
			}
			CheckNullabilityAttributes(nullabilityInfo, getMethod.ReturnParameter.GetCustomAttributesData());
		}
		else
		{
			nullabilityInfo.ReadState = NullabilityState.Unknown;
		}
		if (setMethod != null)
		{
			if (IsPrivateOrInternalMethodAndAnnotationDisabled(setMethod))
			{
				nullabilityInfo.WriteState = NullabilityState.Unknown;
			}
			CheckNullabilityAttributes(nullabilityInfo, setMethod.GetParameters()[0].GetCustomAttributesData());
		}
		else
		{
			nullabilityInfo.WriteState = NullabilityState.Unknown;
		}
		return nullabilityInfo;
	}

	private bool IsPrivateOrInternalMethodAndAnnotationDisabled(MethodInfo method)
	{
		if ((method.IsPrivate || method.IsFamilyAndAssembly || method.IsAssembly) && IsPublicOnly(method.IsPrivate, method.IsFamilyAndAssembly, method.IsAssembly, method.Module))
		{
			return true;
		}
		return false;
	}

	public NullabilityInfo Create(EventInfo eventInfo)
	{
		if ((object)eventInfo == null)
		{
			throw new ArgumentNullException("eventInfo");
		}
		EnsureIsSupported();
		return GetNullabilityInfo(eventInfo, eventInfo.EventHandlerType, eventInfo.GetCustomAttributesData());
	}

	public NullabilityInfo Create(FieldInfo fieldInfo)
	{
		if ((object)fieldInfo == null)
		{
			throw new ArgumentNullException("fieldInfo");
		}
		EnsureIsSupported();
		if (IsPrivateOrInternalFieldAndAnnotationDisabled(fieldInfo))
		{
			return new NullabilityInfo(fieldInfo.FieldType, NullabilityState.Unknown, NullabilityState.Unknown, null, Array.Empty<NullabilityInfo>());
		}
		IList<CustomAttributeData> customAttributesData = fieldInfo.GetCustomAttributesData();
		NullabilityInfo nullabilityInfo = GetNullabilityInfo(fieldInfo, fieldInfo.FieldType, customAttributesData);
		CheckNullabilityAttributes(nullabilityInfo, customAttributesData);
		return nullabilityInfo;
	}

	private static void EnsureIsSupported()
	{
		if (!IsSupported)
		{
			throw new InvalidOperationException(SR.NullabilityInfoContext_NotSupported);
		}
	}

	private bool IsPrivateOrInternalFieldAndAnnotationDisabled(FieldInfo fieldInfo)
	{
		if ((fieldInfo.IsPrivate || fieldInfo.IsFamilyAndAssembly || fieldInfo.IsAssembly) && IsPublicOnly(fieldInfo.IsPrivate, fieldInfo.IsFamilyAndAssembly, fieldInfo.IsAssembly, fieldInfo.Module))
		{
			return true;
		}
		return false;
	}

	private bool IsPublicOnly(bool isPrivate, bool isFamilyAndAssembly, bool isAssembly, Module module)
	{
		if (!_publicOnlyModules.TryGetValue(module, out var value))
		{
			value = PopulateAnnotationInfo(module.GetCustomAttributesData());
			_publicOnlyModules.Add(module, value);
		}
		if (value == NotAnnotatedStatus.None)
		{
			return false;
		}
		if (((isPrivate || isFamilyAndAssembly) && value.HasFlag(NotAnnotatedStatus.Private)) || (isAssembly && value.HasFlag(NotAnnotatedStatus.Internal)))
		{
			return true;
		}
		return false;
	}

	private NotAnnotatedStatus PopulateAnnotationInfo(IList<CustomAttributeData> customAttributes)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out bool flag);
		foreach (CustomAttributeData customAttribute in customAttributes)
		{
			if (customAttribute.AttributeType.Name == "NullablePublicOnlyAttribute" && customAttribute.AttributeType.Namespace == "System.Runtime.CompilerServices" && customAttribute.ConstructorArguments.Count == 1)
			{
				object value = customAttribute.ConstructorArguments[0].Value;
				int num;
				if (value is bool)
				{
					flag = (bool)value;
					num = 1;
				}
				else
				{
					num = 0;
				}
				if (((uint)num & (flag ? 1u : 0u)) != 0)
				{
					return NotAnnotatedStatus.Private | NotAnnotatedStatus.Internal;
				}
				return NotAnnotatedStatus.Private;
			}
		}
		return NotAnnotatedStatus.None;
	}

	private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes)
	{
		return GetNullabilityInfo(memberInfo, type, customAttributes, 0);
	}

	private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes, int index)
	{
		NullabilityState state = NullabilityState.Unknown;
		NullabilityInfo elementType = null;
		NullabilityInfo[] array = Array.Empty<NullabilityInfo>();
		Type type2 = type;
		if (type.IsValueType)
		{
			type2 = Nullable.GetUnderlyingType(type);
			if (type2 != null)
			{
				state = NullabilityState.Nullable;
			}
			else
			{
				type2 = type;
				state = NullabilityState.NotNull;
			}
		}
		else
		{
			if (!ParseNullableState(customAttributes, index, ref state))
			{
				state = GetNullableContext(memberInfo);
			}
			if (type.IsArray)
			{
				elementType = GetNullabilityInfo(memberInfo, type.GetElementType(), customAttributes, index + 1);
			}
		}
		if (type2.IsGenericType)
		{
			Type[] genericArguments = type2.GetGenericArguments();
			array = new NullabilityInfo[genericArguments.Length];
			int i = 0;
			int num = 0;
			for (; i < genericArguments.Length; i++)
			{
				Type type3 = Nullable.GetUnderlyingType(genericArguments[i]) ?? genericArguments[i];
				if (!type3.IsValueType || type3.IsGenericType)
				{
					num++;
				}
				array[i] = GetNullabilityInfo(memberInfo, genericArguments[i], customAttributes, index + num);
			}
		}
		NullabilityInfo nullabilityInfo = new NullabilityInfo(type, state, state, elementType, array);
		if (!type.IsValueType && state != 0)
		{
			TryLoadGenericMetaTypeNullability(memberInfo, nullabilityInfo);
		}
		return nullabilityInfo;
	}

	private static bool ParseNullableState(IList<CustomAttributeData> customAttributes, int index, ref NullabilityState state)
	{
		foreach (CustomAttributeData customAttribute in customAttributes)
		{
			if (customAttribute.AttributeType.Name == "NullableAttribute" && customAttribute.AttributeType.Namespace == "System.Runtime.CompilerServices" && customAttribute.ConstructorArguments.Count == 1)
			{
				object value = customAttribute.ConstructorArguments[0].Value;
				if (value is byte b)
				{
					state = TranslateByte(b);
					return true;
				}
				if (value is ReadOnlyCollection<CustomAttributeTypedArgument> readOnlyCollection && index < readOnlyCollection.Count && readOnlyCollection[index].Value is byte b2)
				{
					state = TranslateByte(b2);
					return true;
				}
				break;
			}
		}
		return false;
	}

	private void TryLoadGenericMetaTypeNullability(MemberInfo memberInfo, NullabilityInfo nullability)
	{
		MemberInfo memberMetadataDefinition = GetMemberMetadataDefinition(memberInfo);
		Type type = null;
		if (memberMetadataDefinition is FieldInfo fieldInfo)
		{
			type = fieldInfo.FieldType;
		}
		else if (memberMetadataDefinition is PropertyInfo property)
		{
			type = GetPropertyMetaType(property);
		}
		if (type != null)
		{
			CheckGenericParameters(nullability, memberMetadataDefinition, type);
		}
	}

	private static MemberInfo GetMemberMetadataDefinition(MemberInfo member)
	{
		Type declaringType = member.DeclaringType;
		if (declaringType != null && declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition)
		{
			return declaringType.GetGenericTypeDefinition().GetMemberWithSameMetadataDefinitionAs(member);
		}
		return member;
	}

	private static Type GetPropertyMetaType(PropertyInfo property)
	{
		MethodInfo getMethod = property.GetGetMethod(nonPublic: true);
		if ((object)getMethod != null)
		{
			return getMethod.ReturnType;
		}
		return property.GetSetMethod(nonPublic: true).GetParameters()[0].ParameterType;
	}

	private void CheckGenericParameters(NullabilityInfo nullability, MemberInfo metaMember, Type metaType)
	{
		if (metaType.IsGenericParameter)
		{
			NullabilityState state = nullability.ReadState;
			if (state == NullabilityState.NotNull && !ParseNullableState(metaType.GetCustomAttributesData(), 0, ref state))
			{
				state = GetNullableContext(metaType);
			}
			nullability.ReadState = state;
			nullability.WriteState = state;
		}
		else
		{
			if (!metaType.ContainsGenericParameters)
			{
				return;
			}
			if (nullability.GenericTypeArguments.Length != 0)
			{
				Type[] genericArguments = metaType.GetGenericArguments();
				for (int i = 0; i < genericArguments.Length; i++)
				{
					if (genericArguments[i].IsGenericParameter)
					{
						NullabilityInfo nullabilityInfo = GetNullabilityInfo(metaMember, genericArguments[i], genericArguments[i].GetCustomAttributesData(), i + 1);
						nullability.GenericTypeArguments[i].ReadState = nullabilityInfo.ReadState;
						nullability.GenericTypeArguments[i].WriteState = nullabilityInfo.WriteState;
					}
					else
					{
						UpdateGenericArrayElements(nullability.GenericTypeArguments[i].ElementType, metaMember, genericArguments[i]);
					}
				}
			}
			else
			{
				UpdateGenericArrayElements(nullability.ElementType, metaMember, metaType);
			}
		}
	}

	private void UpdateGenericArrayElements(NullabilityInfo elementState, MemberInfo metaMember, Type metaType)
	{
		if (metaType.IsArray && elementState != null && metaType.GetElementType().IsGenericParameter)
		{
			Type elementType = metaType.GetElementType();
			NullabilityInfo nullabilityInfo = GetNullabilityInfo(metaMember, elementType, elementType.GetCustomAttributesData(), 0);
			elementState.ReadState = nullabilityInfo.ReadState;
			elementState.WriteState = nullabilityInfo.WriteState;
		}
	}

	private static NullabilityState TranslateByte(object value)
	{
		if (!(value is byte b))
		{
			return NullabilityState.Unknown;
		}
		return TranslateByte(b);
	}

	private static NullabilityState TranslateByte(byte b)
	{
		return b switch
		{
			1 => NullabilityState.NotNull, 
			2 => NullabilityState.Nullable, 
			_ => NullabilityState.Unknown, 
		};
	}
}
