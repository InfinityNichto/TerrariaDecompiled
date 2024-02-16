using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace System.Runtime.Serialization;

public static class FormatterServices
{
	private static readonly ConcurrentDictionary<MemberHolder, MemberInfo[]> s_memberInfoTable = new ConcurrentDictionary<MemberHolder, MemberInfo[]>();

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "The Type is annotated with All, which will preserve base type fields.")]
	private static FieldInfo[] InternalGetSerializableMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		if (type.IsInterface)
		{
			return Array.Empty<FieldInfo>();
		}
		if (!type.IsSerializable)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_NonSerType, type.FullName, type.Assembly.FullName));
		}
		FieldInfo[] array = GetSerializableFields(type);
		Type baseType = type.BaseType;
		if (baseType != null && baseType != typeof(object))
		{
			Type[] parentTypes;
			int parentTypeCount;
			bool parentTypes2 = GetParentTypes(baseType, out parentTypes, out parentTypeCount);
			if (parentTypeCount > 0)
			{
				List<FieldInfo> list = new List<FieldInfo>();
				for (int i = 0; i < parentTypeCount; i++)
				{
					baseType = parentTypes[i];
					if (!baseType.IsSerializable)
					{
						throw new SerializationException(System.SR.Format(System.SR.Serialization_NonSerType, baseType.FullName, baseType.Module.Assembly.FullName));
					}
					FieldInfo[] fields = baseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
					string namePrefix = (parentTypes2 ? baseType.Name : baseType.FullName);
					FieldInfo[] array2 = fields;
					foreach (FieldInfo fieldInfo in array2)
					{
						if (!fieldInfo.IsNotSerialized)
						{
							list.Add(new SerializationFieldInfo(fieldInfo, namePrefix));
						}
					}
				}
				if (list != null && list.Count > 0)
				{
					FieldInfo[] array3 = new FieldInfo[list.Count + array.Length];
					Array.Copy(array, array3, array.Length);
					list.CopyTo(array3, array.Length);
					array = array3;
				}
			}
		}
		return array;
	}

	private static FieldInfo[] GetSerializableFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		int num = 0;
		for (int i = 0; i < fields.Length; i++)
		{
			if ((fields[i].Attributes & FieldAttributes.NotSerialized) != FieldAttributes.NotSerialized)
			{
				num++;
			}
		}
		if (num != fields.Length)
		{
			FieldInfo[] array = new FieldInfo[num];
			num = 0;
			for (int j = 0; j < fields.Length; j++)
			{
				if ((fields[j].Attributes & FieldAttributes.NotSerialized) != FieldAttributes.NotSerialized)
				{
					array[num] = fields[j];
					num++;
				}
			}
			return array;
		}
		return fields;
	}

	private static bool GetParentTypes(Type parentType, out Type[] parentTypes, out int parentTypeCount)
	{
		parentTypes = null;
		parentTypeCount = 0;
		bool flag = true;
		Type typeFromHandle = typeof(object);
		Type type = parentType;
		while (type != typeFromHandle)
		{
			if (!type.IsInterface)
			{
				string name = type.Name;
				int num = 0;
				while (flag && num < parentTypeCount)
				{
					string name2 = parentTypes[num].Name;
					if (name2.Length == name.Length && name2[0] == name[0] && name == name2)
					{
						flag = false;
						break;
					}
					num++;
				}
				if (parentTypes == null || parentTypeCount == parentTypes.Length)
				{
					Array.Resize(ref parentTypes, Math.Max(parentTypeCount * 2, 12));
				}
				parentTypes[parentTypeCount++] = type;
			}
			type = type.BaseType;
		}
		return flag;
	}

	public static MemberInfo[] GetSerializableMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		return GetSerializableMembers(type, new StreamingContext(StreamingContextStates.All));
	}

	public static MemberInfo[] GetSerializableMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, StreamingContext context)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return s_memberInfoTable.GetOrAdd(new MemberHolder(type, context), (MemberHolder mh) => InternalGetSerializableMembers(mh._memberType));
	}

	public static void CheckTypeSecurity(Type t, TypeFilterLevel securityLevel)
	{
	}

	public static object GetUninitializedObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
	{
		return RuntimeHelpers.GetUninitializedObject(type);
	}

	public static object GetSafeUninitializedObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
	{
		return RuntimeHelpers.GetUninitializedObject(type);
	}

	internal static void SerializationSetValue(MemberInfo fi, object target, object value)
	{
		if (fi is FieldInfo fieldInfo)
		{
			fieldInfo.SetValue(target, value);
			return;
		}
		throw new ArgumentException(System.SR.Argument_InvalidFieldInfo);
	}

	public static object PopulateObjectMembers(object obj, MemberInfo[] members, object?[] data)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (members == null)
		{
			throw new ArgumentNullException("members");
		}
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (members.Length != data.Length)
		{
			throw new ArgumentException(System.SR.Argument_DataLengthDifferent);
		}
		for (int i = 0; i < members.Length; i++)
		{
			MemberInfo memberInfo = members[i];
			if (memberInfo == null)
			{
				throw new ArgumentNullException("members", System.SR.Format(System.SR.ArgumentNull_NullMember, i));
			}
			object obj2 = data[i];
			if (obj2 != null)
			{
				if (!(memberInfo is FieldInfo fieldInfo))
				{
					throw new SerializationException(System.SR.Serialization_UnknownMemberInfo);
				}
				fieldInfo.SetValue(obj, data[i]);
			}
		}
		return obj;
	}

	public static object?[] GetObjectData(object obj, MemberInfo[] members)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (members == null)
		{
			throw new ArgumentNullException("members");
		}
		object[] array = new object[members.Length];
		for (int i = 0; i < members.Length; i++)
		{
			MemberInfo memberInfo = members[i];
			if (memberInfo == null)
			{
				throw new ArgumentNullException("members", System.SR.Format(System.SR.ArgumentNull_NullMember, i));
			}
			FieldInfo fieldInfo = memberInfo as FieldInfo;
			if (fieldInfo == null)
			{
				throw new SerializationException(System.SR.Serialization_UnknownMemberInfo);
			}
			array[i] = fieldInfo.GetValue(obj);
		}
		return array;
	}

	public static ISerializationSurrogate GetSurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
	{
		if (innerSurrogate == null)
		{
			throw new ArgumentNullException("innerSurrogate");
		}
		return new SurrogateForCyclicalReference(innerSurrogate);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public static Type? GetTypeFromAssembly(Assembly assem, string name)
	{
		if (assem == null)
		{
			throw new ArgumentNullException("assem");
		}
		return assem.GetType(name, throwOnError: false, ignoreCase: false);
	}

	internal static Assembly LoadAssemblyFromString(string assemblyName)
	{
		return Assembly.Load(new AssemblyName(assemblyName));
	}

	internal static Assembly LoadAssemblyFromStringNoThrow(string assemblyName)
	{
		try
		{
			return LoadAssemblyFromString(assemblyName);
		}
		catch (Exception)
		{
		}
		return null;
	}

	internal static string GetClrAssemblyName(Type type, out bool hasTypeForwardedFrom)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		Type type2 = type;
		while (type2.HasElementType)
		{
			type2 = type2.GetElementType();
		}
		object[] customAttributes = type2.GetCustomAttributes(typeof(TypeForwardedFromAttribute), inherit: false);
		int num = 0;
		if (num < customAttributes.Length)
		{
			Attribute attribute = (Attribute)customAttributes[num];
			hasTypeForwardedFrom = true;
			return ((TypeForwardedFromAttribute)attribute).AssemblyFullName;
		}
		hasTypeForwardedFrom = false;
		return type.Assembly.FullName;
	}

	internal static string GetClrTypeFullName(Type type)
	{
		if (!type.IsArray)
		{
			return GetClrTypeFullNameForNonArrayTypes(type);
		}
		return GetClrTypeFullNameForArray(type);
	}

	private static string GetClrTypeFullNameForArray(Type type)
	{
		int arrayRank = type.GetArrayRank();
		string clrTypeFullName = GetClrTypeFullName(type.GetElementType());
		if (arrayRank != 1)
		{
			return clrTypeFullName + "[" + new string(',', arrayRank - 1) + "]";
		}
		return clrTypeFullName + "[]";
	}

	private static string GetClrTypeFullNameForNonArrayTypes(Type type)
	{
		if (!type.IsGenericType)
		{
			return type.FullName;
		}
		StringBuilder stringBuilder = new StringBuilder(type.GetGenericTypeDefinition().FullName).Append('[');
		Type[] genericArguments = type.GetGenericArguments();
		foreach (Type type2 in genericArguments)
		{
			stringBuilder.Append('[').Append(GetClrTypeFullName(type2)).Append(", ");
			stringBuilder.Append(GetClrAssemblyName(type2, out var _)).Append("],");
		}
		return stringBuilder.Remove(stringBuilder.Length - 1, 1).Append(']').ToString();
	}
}
