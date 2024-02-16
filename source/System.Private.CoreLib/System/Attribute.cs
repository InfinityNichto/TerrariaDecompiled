using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System;

[Serializable]
[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class Attribute
{
	public virtual object TypeId => GetType();

	private static Attribute[] InternalGetCustomAttributes(PropertyInfo element, Type type, bool inherit)
	{
		Attribute[] array = (Attribute[])element.GetCustomAttributes(type, inherit);
		if (!inherit)
		{
			return array;
		}
		Type[] indexParameterTypes = GetIndexParameterTypes(element);
		PropertyInfo parentDefinition = GetParentDefinition(element, indexParameterTypes);
		if (parentDefinition == null)
		{
			return array;
		}
		Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
		List<Attribute> list = new List<Attribute>();
		CopyToAttributeList(list, array, types);
		do
		{
			array = GetCustomAttributes(parentDefinition, type, inherit: false);
			AddAttributesToList(list, array, types);
			parentDefinition = GetParentDefinition(parentDefinition, indexParameterTypes);
		}
		while (parentDefinition != null);
		Attribute[] array2 = CreateAttributeArrayHelper(type, list.Count);
		list.CopyTo(array2, 0);
		return array2;
	}

	private static bool InternalIsDefined(PropertyInfo element, Type attributeType, bool inherit)
	{
		if (element.IsDefined(attributeType, inherit))
		{
			return true;
		}
		if (inherit)
		{
			AttributeUsageAttribute attributeUsageAttribute = InternalGetAttributeUsage(attributeType);
			if (!attributeUsageAttribute.Inherited)
			{
				return false;
			}
			Type[] indexParameterTypes = GetIndexParameterTypes(element);
			PropertyInfo parentDefinition = GetParentDefinition(element, indexParameterTypes);
			while (parentDefinition != null)
			{
				if (parentDefinition.IsDefined(attributeType, inherit: false))
				{
					return true;
				}
				parentDefinition = GetParentDefinition(parentDefinition, indexParameterTypes);
			}
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "rtPropAccessor.DeclaringType is guaranteed to have the specified property because rtPropAccessor.GetParentDefinition() returned a non-null MethodInfo.")]
	private static PropertyInfo GetParentDefinition(PropertyInfo property, Type[] propertyParameters)
	{
		MethodInfo methodInfo = property.GetGetMethod(nonPublic: true) ?? property.GetSetMethod(nonPublic: true);
		RuntimeMethodInfo runtimeMethodInfo = methodInfo as RuntimeMethodInfo;
		if (runtimeMethodInfo != null)
		{
			runtimeMethodInfo = runtimeMethodInfo.GetParentDefinition();
			if (runtimeMethodInfo != null)
			{
				return runtimeMethodInfo.DeclaringType.GetProperty(property.Name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, property.PropertyType, propertyParameters, null);
			}
		}
		return null;
	}

	private static Attribute[] InternalGetCustomAttributes(EventInfo element, Type type, bool inherit)
	{
		Attribute[] array = (Attribute[])element.GetCustomAttributes(type, inherit);
		if (!inherit)
		{
			return array;
		}
		EventInfo parentDefinition = GetParentDefinition(element);
		if (parentDefinition == null)
		{
			return array;
		}
		Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
		List<Attribute> list = new List<Attribute>();
		CopyToAttributeList(list, array, types);
		do
		{
			array = GetCustomAttributes(parentDefinition, type, inherit: false);
			AddAttributesToList(list, array, types);
			parentDefinition = GetParentDefinition(parentDefinition);
		}
		while (parentDefinition != null);
		Attribute[] array2 = CreateAttributeArrayHelper(type, list.Count);
		list.CopyTo(array2, 0);
		return array2;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "rtAdd.DeclaringType is guaranteed to have the specified event because rtAdd.GetParentDefinition() returned a non-null MethodInfo.")]
	private static EventInfo GetParentDefinition(EventInfo ev)
	{
		MethodInfo addMethod = ev.GetAddMethod(nonPublic: true);
		RuntimeMethodInfo runtimeMethodInfo = addMethod as RuntimeMethodInfo;
		if (runtimeMethodInfo != null)
		{
			runtimeMethodInfo = runtimeMethodInfo.GetParentDefinition();
			if (runtimeMethodInfo != null)
			{
				return runtimeMethodInfo.DeclaringType.GetEvent(ev.Name);
			}
		}
		return null;
	}

	private static bool InternalIsDefined(EventInfo element, Type attributeType, bool inherit)
	{
		if (element.IsDefined(attributeType, inherit))
		{
			return true;
		}
		if (inherit)
		{
			AttributeUsageAttribute attributeUsageAttribute = InternalGetAttributeUsage(attributeType);
			if (!attributeUsageAttribute.Inherited)
			{
				return false;
			}
			EventInfo parentDefinition = GetParentDefinition(element);
			while (parentDefinition != null)
			{
				if (parentDefinition.IsDefined(attributeType, inherit: false))
				{
					return true;
				}
				parentDefinition = GetParentDefinition(parentDefinition);
			}
		}
		return false;
	}

	private static ParameterInfo GetParentDefinition(ParameterInfo param)
	{
		RuntimeMethodInfo runtimeMethodInfo = param.Member as RuntimeMethodInfo;
		if (runtimeMethodInfo != null)
		{
			runtimeMethodInfo = runtimeMethodInfo.GetParentDefinition();
			if (runtimeMethodInfo != null)
			{
				int position = param.Position;
				if (position == -1)
				{
					return runtimeMethodInfo.ReturnParameter;
				}
				ParameterInfo[] parameters = runtimeMethodInfo.GetParameters();
				return parameters[position];
			}
		}
		return null;
	}

	private static Attribute[] InternalParamGetCustomAttributes(ParameterInfo param, Type type, bool inherit)
	{
		List<Type> list = new List<Type>();
		if ((object)type == null)
		{
			type = typeof(Attribute);
		}
		object[] customAttributes = param.GetCustomAttributes(type, inherit: false);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			Type type2 = customAttributes[i].GetType();
			AttributeUsageAttribute attributeUsageAttribute = InternalGetAttributeUsage(type2);
			if (!attributeUsageAttribute.AllowMultiple)
			{
				list.Add(type2);
			}
		}
		Attribute[] array = ((customAttributes.Length != 0) ? ((Attribute[])customAttributes) : CreateAttributeArrayHelper(type, 0));
		if ((object)param.Member.DeclaringType == null)
		{
			return array;
		}
		if (!inherit)
		{
			return array;
		}
		for (ParameterInfo parentDefinition = GetParentDefinition(param); parentDefinition != null; parentDefinition = GetParentDefinition(parentDefinition))
		{
			customAttributes = parentDefinition.GetCustomAttributes(type, inherit: false);
			int num = 0;
			for (int j = 0; j < customAttributes.Length; j++)
			{
				Type type3 = customAttributes[j].GetType();
				AttributeUsageAttribute attributeUsageAttribute2 = InternalGetAttributeUsage(type3);
				if (attributeUsageAttribute2.Inherited && !list.Contains(type3))
				{
					if (!attributeUsageAttribute2.AllowMultiple)
					{
						list.Add(type3);
					}
					num++;
				}
				else
				{
					customAttributes[j] = null;
				}
			}
			Attribute[] array2 = CreateAttributeArrayHelper(type, num);
			num = 0;
			foreach (object obj in customAttributes)
			{
				if (obj != null)
				{
					array2[num] = (Attribute)obj;
					num++;
				}
			}
			Attribute[] array3 = array;
			array = CreateAttributeArrayHelper(type, array3.Length + num);
			Array.Copy(array3, array, array3.Length);
			int num2 = array3.Length;
			for (int l = 0; l < array2.Length; l++)
			{
				array[num2 + l] = array2[l];
			}
		}
		return array;
	}

	private static bool InternalParamIsDefined(ParameterInfo param, Type type, bool inherit)
	{
		if (param.IsDefined(type, inherit: false))
		{
			return true;
		}
		if ((object)param.Member.DeclaringType == null || !inherit)
		{
			return false;
		}
		for (ParameterInfo parentDefinition = GetParentDefinition(param); parentDefinition != null; parentDefinition = GetParentDefinition(parentDefinition))
		{
			object[] customAttributes = parentDefinition.GetCustomAttributes(type, inherit: false);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				Type type2 = customAttributes[i].GetType();
				AttributeUsageAttribute attributeUsageAttribute = InternalGetAttributeUsage(type2);
				if (customAttributes[i] is Attribute && attributeUsageAttribute.Inherited)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void CopyToAttributeList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
	{
		for (int i = 0; i < attributes.Length; i++)
		{
			attributeList.Add(attributes[i]);
			Type type = attributes[i].GetType();
			if (!types.ContainsKey(type))
			{
				types[type] = InternalGetAttributeUsage(type);
			}
		}
	}

	private static Type[] GetIndexParameterTypes(PropertyInfo element)
	{
		ParameterInfo[] indexParameters = element.GetIndexParameters();
		if (indexParameters.Length != 0)
		{
			Type[] array = new Type[indexParameters.Length];
			for (int i = 0; i < indexParameters.Length; i++)
			{
				array[i] = indexParameters[i].ParameterType;
			}
			return array;
		}
		return Type.EmptyTypes;
	}

	private static void AddAttributesToList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
	{
		for (int i = 0; i < attributes.Length; i++)
		{
			Type type = attributes[i].GetType();
			types.TryGetValue(type, out var value);
			if (value == null)
			{
				value = (types[type] = InternalGetAttributeUsage(type));
				if (value.Inherited)
				{
					attributeList.Add(attributes[i]);
				}
			}
			else if (value.Inherited && value.AllowMultiple)
			{
				attributeList.Add(attributes[i]);
			}
		}
	}

	private static AttributeUsageAttribute InternalGetAttributeUsage(Type type)
	{
		object[] customAttributes = type.GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false);
		if (customAttributes.Length == 1)
		{
			return (AttributeUsageAttribute)customAttributes[0];
		}
		if (customAttributes.Length == 0)
		{
			return AttributeUsageAttribute.Default;
		}
		throw new FormatException(SR.Format(SR.Format_AttributeUsage, type));
	}

	private static Attribute[] CreateAttributeArrayHelper(Type elementType, int elementCount)
	{
		return (Attribute[])Array.CreateInstance(elementType, elementCount);
	}

	public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType)
	{
		return GetCustomAttributes(element, attributeType, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(MemberInfo element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		return element.MemberType switch
		{
			MemberTypes.Property => InternalGetCustomAttributes((PropertyInfo)element, attributeType, inherit), 
			MemberTypes.Event => InternalGetCustomAttributes((EventInfo)element, attributeType, inherit), 
			_ => element.GetCustomAttributes(attributeType, inherit) as Attribute[], 
		};
	}

	public static Attribute[] GetCustomAttributes(MemberInfo element)
	{
		return GetCustomAttributes(element, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return element.MemberType switch
		{
			MemberTypes.Property => InternalGetCustomAttributes((PropertyInfo)element, typeof(Attribute), inherit), 
			MemberTypes.Event => InternalGetCustomAttributes((EventInfo)element, typeof(Attribute), inherit), 
			_ => element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[], 
		};
	}

	public static bool IsDefined(MemberInfo element, Type attributeType)
	{
		return IsDefined(element, attributeType, inherit: true);
	}

	public static bool IsDefined(MemberInfo element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		return element.MemberType switch
		{
			MemberTypes.Property => InternalIsDefined((PropertyInfo)element, attributeType, inherit), 
			MemberTypes.Event => InternalIsDefined((EventInfo)element, attributeType, inherit), 
			_ => element.IsDefined(attributeType, inherit), 
		};
	}

	public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType)
	{
		return GetCustomAttribute(element, attributeType, inherit: true);
	}

	public static Attribute? GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit)
	{
		Attribute[] customAttributes = GetCustomAttributes(element, attributeType, inherit);
		if (customAttributes == null || customAttributes.Length == 0)
		{
			return null;
		}
		if (customAttributes.Length == 1)
		{
			return customAttributes[0];
		}
		throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
	}

	public static Attribute[] GetCustomAttributes(ParameterInfo element)
	{
		return GetCustomAttributes(element, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType)
	{
		return GetCustomAttributes(element, attributeType, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		if (element.Member == null)
		{
			throw new ArgumentException(SR.Argument_InvalidParameterInfo, "element");
		}
		MemberInfo member = element.Member;
		if (member.MemberType == MemberTypes.Method && inherit)
		{
			return InternalParamGetCustomAttributes(element, attributeType, inherit);
		}
		return element.GetCustomAttributes(attributeType, inherit) as Attribute[];
	}

	public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (element.Member == null)
		{
			throw new ArgumentException(SR.Argument_InvalidParameterInfo, "element");
		}
		MemberInfo member = element.Member;
		if (member.MemberType == MemberTypes.Method && inherit)
		{
			return InternalParamGetCustomAttributes(element, null, inherit);
		}
		return element.GetCustomAttributes(typeof(Attribute), inherit) as Attribute[];
	}

	public static bool IsDefined(ParameterInfo element, Type attributeType)
	{
		return IsDefined(element, attributeType, inherit: true);
	}

	public static bool IsDefined(ParameterInfo element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		MemberInfo member = element.Member;
		return member.MemberType switch
		{
			MemberTypes.Method => InternalParamIsDefined(element, attributeType, inherit), 
			MemberTypes.Constructor => element.IsDefined(attributeType, inherit: false), 
			MemberTypes.Property => element.IsDefined(attributeType, inherit: false), 
			_ => throw new ArgumentException(SR.Argument_InvalidParamInfo), 
		};
	}

	public static Attribute? GetCustomAttribute(ParameterInfo element, Type attributeType)
	{
		return GetCustomAttribute(element, attributeType, inherit: true);
	}

	public static Attribute? GetCustomAttribute(ParameterInfo element, Type attributeType, bool inherit)
	{
		Attribute[] customAttributes = GetCustomAttributes(element, attributeType, inherit);
		if (customAttributes == null || customAttributes.Length == 0)
		{
			return null;
		}
		if (customAttributes.Length == 0)
		{
			return null;
		}
		if (customAttributes.Length == 1)
		{
			return customAttributes[0];
		}
		throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
	}

	public static Attribute[] GetCustomAttributes(Module element, Type attributeType)
	{
		return GetCustomAttributes(element, attributeType, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(Module element)
	{
		return GetCustomAttributes(element, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(Module element, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return (Attribute[])element.GetCustomAttributes(typeof(Attribute), inherit);
	}

	public static Attribute[] GetCustomAttributes(Module element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
	}

	public static bool IsDefined(Module element, Type attributeType)
	{
		return IsDefined(element, attributeType, inherit: false);
	}

	public static bool IsDefined(Module element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		return element.IsDefined(attributeType, inherit: false);
	}

	public static Attribute? GetCustomAttribute(Module element, Type attributeType)
	{
		return GetCustomAttribute(element, attributeType, inherit: true);
	}

	public static Attribute? GetCustomAttribute(Module element, Type attributeType, bool inherit)
	{
		Attribute[] customAttributes = GetCustomAttributes(element, attributeType, inherit);
		if (customAttributes == null || customAttributes.Length == 0)
		{
			return null;
		}
		if (customAttributes.Length == 1)
		{
			return customAttributes[0];
		}
		throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
	}

	public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType)
	{
		return GetCustomAttributes(element, attributeType, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
	}

	public static Attribute[] GetCustomAttributes(Assembly element)
	{
		return GetCustomAttributes(element, inherit: true);
	}

	public static Attribute[] GetCustomAttributes(Assembly element, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return (Attribute[])element.GetCustomAttributes(typeof(Attribute), inherit);
	}

	public static bool IsDefined(Assembly element, Type attributeType)
	{
		return IsDefined(element, attributeType, inherit: true);
	}

	public static bool IsDefined(Assembly element, Type attributeType, bool inherit)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		if (!attributeType.IsSubclassOf(typeof(Attribute)) && attributeType != typeof(Attribute))
		{
			throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);
		}
		return element.IsDefined(attributeType, inherit: false);
	}

	public static Attribute? GetCustomAttribute(Assembly element, Type attributeType)
	{
		return GetCustomAttribute(element, attributeType, inherit: true);
	}

	public static Attribute? GetCustomAttribute(Assembly element, Type attributeType, bool inherit)
	{
		Attribute[] customAttributes = GetCustomAttributes(element, attributeType, inherit);
		if (customAttributes == null || customAttributes.Length == 0)
		{
			return null;
		}
		if (customAttributes.Length == 1)
		{
			return customAttributes[0];
		}
		throw new AmbiguousMatchException(SR.RFLCT_AmbigCust);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "Unused fields don't make a difference for equality")]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (GetType() != obj.GetType())
		{
			return false;
		}
		Type type = GetType();
		while (type != typeof(Attribute))
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; i++)
			{
				object value = fields[i].GetValue(this);
				object value2 = fields[i].GetValue(obj);
				if (!AreFieldValuesEqual(value, value2))
				{
					return false;
				}
			}
			type = type.BaseType;
		}
		return true;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "Unused fields don't make a difference for hashcode quality")]
	public override int GetHashCode()
	{
		Type type = GetType();
		while (type != typeof(Attribute))
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object obj = null;
			for (int i = 0; i < fields.Length; i++)
			{
				object value = fields[i].GetValue(this);
				if (value != null && !value.GetType().IsArray)
				{
					obj = value;
				}
				if (obj != null)
				{
					break;
				}
			}
			if (obj != null)
			{
				return obj.GetHashCode();
			}
			type = type.BaseType;
		}
		return type.GetHashCode();
	}

	private static bool AreFieldValuesEqual(object thisValue, object thatValue)
	{
		if (thisValue == null && thatValue == null)
		{
			return true;
		}
		if (thisValue == null || thatValue == null)
		{
			return false;
		}
		Type type = thisValue.GetType();
		if (type.IsArray)
		{
			if (!type.Equals(thatValue.GetType()))
			{
				return false;
			}
			Array array = (Array)thisValue;
			Array array2 = (Array)thatValue;
			if (array.Length != array2.Length)
			{
				return false;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (!AreFieldValuesEqual(array.GetValue(i), array2.GetValue(i)))
				{
					return false;
				}
			}
		}
		else if (!thisValue.Equals(thatValue))
		{
			return false;
		}
		return true;
	}

	public virtual bool Match(object? obj)
	{
		return Equals(obj);
	}

	public virtual bool IsDefaultAttribute()
	{
		return false;
	}
}
