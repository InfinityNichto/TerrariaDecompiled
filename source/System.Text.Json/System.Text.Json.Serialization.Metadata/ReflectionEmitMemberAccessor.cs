using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Text.Json.Serialization.Metadata;

internal sealed class ReflectionEmitMemberAccessor : MemberAccessor
{
	public override JsonTypeInfo.ConstructorDelegate CreateConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
	{
		ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
		if (type.IsAbstract)
		{
			return null;
		}
		if (constructor == null && !type.IsValueType)
		{
			return null;
		}
		DynamicMethod dynamicMethod = new DynamicMethod(ConstructorInfo.ConstructorName, JsonTypeInfo.ObjectType, Type.EmptyTypes, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		if (constructor == null)
		{
			LocalBuilder local = iLGenerator.DeclareLocal(type);
			iLGenerator.Emit(OpCodes.Ldloca_S, local);
			iLGenerator.Emit(OpCodes.Initobj, type);
			iLGenerator.Emit(OpCodes.Ldloc, local);
			iLGenerator.Emit(OpCodes.Box, type);
		}
		else
		{
			iLGenerator.Emit(OpCodes.Newobj, constructor);
		}
		iLGenerator.Emit(OpCodes.Ret);
		return (JsonTypeInfo.ConstructorDelegate)dynamicMethod.CreateDelegate(typeof(JsonTypeInfo.ConstructorDelegate));
	}

	public override Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor)
	{
		return CreateDelegate<Func<object[], T>>(CreateParameterizedConstructor(constructor));
	}

	private static DynamicMethod CreateParameterizedConstructor(ConstructorInfo constructor)
	{
		Type declaringType = constructor.DeclaringType;
		ParameterInfo[] parameters = constructor.GetParameters();
		int num = parameters.Length;
		if (num > 64)
		{
			return null;
		}
		DynamicMethod dynamicMethod = new DynamicMethod(ConstructorInfo.ConstructorName, declaringType, new Type[1] { typeof(object[]) }, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		for (int i = 0; i < num; i++)
		{
			Type parameterType = parameters[i].ParameterType;
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldc_I4_S, i);
			iLGenerator.Emit(OpCodes.Ldelem_Ref);
			iLGenerator.Emit(OpCodes.Unbox_Any, parameterType);
		}
		iLGenerator.Emit(OpCodes.Newobj, constructor);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3> CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
	{
		return CreateDelegate<JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>>(CreateParameterizedConstructor(constructor, typeof(TArg0), typeof(TArg1), typeof(TArg2), typeof(TArg3)));
	}

	private static DynamicMethod CreateParameterizedConstructor(ConstructorInfo constructor, Type parameterType1, Type parameterType2, Type parameterType3, Type parameterType4)
	{
		Type declaringType = constructor.DeclaringType;
		ParameterInfo[] parameters = constructor.GetParameters();
		int num = parameters.Length;
		DynamicMethod dynamicMethod = new DynamicMethod(ConstructorInfo.ConstructorName, declaringType, new Type[4] { parameterType1, parameterType2, parameterType3, parameterType4 }, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		for (int i = 0; i < num; i++)
		{
			ILGenerator iLGenerator2 = iLGenerator;
			iLGenerator2.Emit(i switch
			{
				0 => OpCodes.Ldarg_0, 
				1 => OpCodes.Ldarg_1, 
				2 => OpCodes.Ldarg_2, 
				3 => OpCodes.Ldarg_3, 
				_ => throw new InvalidOperationException(), 
			});
		}
		iLGenerator.Emit(OpCodes.Newobj, constructor);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	public override Action<TCollection, object> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>()
	{
		return CreateDelegate<Action<TCollection, object>>(CreateAddMethodDelegate(typeof(TCollection)));
	}

	private static DynamicMethod CreateAddMethodDelegate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type collectionType)
	{
		MethodInfo methodInfo = collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue");
		DynamicMethod dynamicMethod = new DynamicMethod(methodInfo.Name, typeof(void), new Type[2]
		{
			collectionType,
			JsonTypeInfo.ObjectType
		}, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Ldarg_1);
		iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>()
	{
		return CreateDelegate<Func<IEnumerable<TElement>, TCollection>>(CreateImmutableEnumerableCreateRangeDelegate(typeof(TCollection), typeof(TElement), typeof(IEnumerable<TElement>)));
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	private static DynamicMethod CreateImmutableEnumerableCreateRangeDelegate(Type collectionType, Type elementType, Type enumerableType)
	{
		MethodInfo immutableEnumerableCreateRangeMethod = collectionType.GetImmutableEnumerableCreateRangeMethod(elementType);
		DynamicMethod dynamicMethod = new DynamicMethod(immutableEnumerableCreateRangeMethod.Name, collectionType, new Type[1] { enumerableType }, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, immutableEnumerableCreateRangeMethod);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public override Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>()
	{
		return CreateDelegate<Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection>>(CreateImmutableDictionaryCreateRangeDelegate(typeof(TCollection), typeof(TKey), typeof(TValue), typeof(IEnumerable<KeyValuePair<TKey, TValue>>)));
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	private static DynamicMethod CreateImmutableDictionaryCreateRangeDelegate(Type collectionType, Type keyType, Type valueType, Type enumerableType)
	{
		MethodInfo immutableDictionaryCreateRangeMethod = collectionType.GetImmutableDictionaryCreateRangeMethod(keyType, valueType);
		DynamicMethod dynamicMethod = new DynamicMethod(immutableDictionaryCreateRangeMethod.Name, collectionType, new Type[1] { enumerableType }, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, immutableDictionaryCreateRangeMethod);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
	{
		return CreateDelegate<Func<object, TProperty>>(CreatePropertyGetter(propertyInfo, typeof(TProperty)));
	}

	private static DynamicMethod CreatePropertyGetter(PropertyInfo propertyInfo, Type runtimePropertyType)
	{
		MethodInfo getMethod = propertyInfo.GetMethod;
		Type declaringType = propertyInfo.DeclaringType;
		Type propertyType = propertyInfo.PropertyType;
		DynamicMethod dynamicMethod = CreateGetterMethod(propertyInfo.Name, runtimePropertyType);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		if (declaringType.IsValueType)
		{
			iLGenerator.Emit(OpCodes.Unbox, declaringType);
			iLGenerator.Emit(OpCodes.Call, getMethod);
		}
		else
		{
			iLGenerator.Emit(OpCodes.Castclass, declaringType);
			iLGenerator.Emit(OpCodes.Callvirt, getMethod);
		}
		if (propertyType != runtimePropertyType && propertyType.IsValueType)
		{
			iLGenerator.Emit(OpCodes.Box, propertyType);
		}
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
	{
		return CreateDelegate<Action<object, TProperty>>(CreatePropertySetter(propertyInfo, typeof(TProperty)));
	}

	private static DynamicMethod CreatePropertySetter(PropertyInfo propertyInfo, Type runtimePropertyType)
	{
		MethodInfo setMethod = propertyInfo.SetMethod;
		Type declaringType = propertyInfo.DeclaringType;
		Type propertyType = propertyInfo.PropertyType;
		DynamicMethod dynamicMethod = CreateSetterMethod(propertyInfo.Name, runtimePropertyType);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
		iLGenerator.Emit(OpCodes.Ldarg_1);
		if (propertyType != runtimePropertyType && propertyType.IsValueType)
		{
			iLGenerator.Emit(OpCodes.Unbox_Any, propertyType);
		}
		iLGenerator.Emit(declaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, setMethod);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo)
	{
		return CreateDelegate<Func<object, TProperty>>(CreateFieldGetter(fieldInfo, typeof(TProperty)));
	}

	private static DynamicMethod CreateFieldGetter(FieldInfo fieldInfo, Type runtimeFieldType)
	{
		Type declaringType = fieldInfo.DeclaringType;
		Type fieldType = fieldInfo.FieldType;
		DynamicMethod dynamicMethod = CreateGetterMethod(fieldInfo.Name, runtimeFieldType);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
		iLGenerator.Emit(OpCodes.Ldfld, fieldInfo);
		if (fieldType.IsValueType && fieldType != runtimeFieldType)
		{
			iLGenerator.Emit(OpCodes.Box, fieldType);
		}
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo)
	{
		return CreateDelegate<Action<object, TProperty>>(CreateFieldSetter(fieldInfo, typeof(TProperty)));
	}

	private static DynamicMethod CreateFieldSetter(FieldInfo fieldInfo, Type runtimeFieldType)
	{
		Type declaringType = fieldInfo.DeclaringType;
		Type fieldType = fieldInfo.FieldType;
		DynamicMethod dynamicMethod = CreateSetterMethod(fieldInfo.Name, runtimeFieldType);
		ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
		iLGenerator.Emit(OpCodes.Ldarg_1);
		if (fieldType != runtimeFieldType && fieldType.IsValueType)
		{
			iLGenerator.Emit(OpCodes.Unbox_Any, fieldType);
		}
		iLGenerator.Emit(OpCodes.Stfld, fieldInfo);
		iLGenerator.Emit(OpCodes.Ret);
		return dynamicMethod;
	}

	private static DynamicMethod CreateGetterMethod(string memberName, Type memberType)
	{
		return new DynamicMethod(memberName + "Getter", memberType, new Type[1] { JsonTypeInfo.ObjectType }, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
	}

	private static DynamicMethod CreateSetterMethod(string memberName, Type memberType)
	{
		return new DynamicMethod(memberName + "Setter", typeof(void), new Type[2]
		{
			JsonTypeInfo.ObjectType,
			memberType
		}, typeof(ReflectionEmitMemberAccessor).Module, skipVisibility: true);
	}

	[return: NotNullIfNotNull("method")]
	private static T CreateDelegate<T>(DynamicMethod method) where T : Delegate
	{
		return (T)(method?.CreateDelegate(typeof(T)));
	}
}
