using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata;

internal sealed class ReflectionMemberAccessor : MemberAccessor
{
	private class ConstructorContext
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		private readonly Type _type;

		public ConstructorContext([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
		{
			_type = type;
		}

		public object CreateInstance()
		{
			return Activator.CreateInstance(_type, nonPublic: false);
		}
	}

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
		return new ConstructorContext(type).CreateInstance;
	}

	public override Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor)
	{
		Type typeFromHandle = typeof(T);
		int parameterCount = constructor.GetParameters().Length;
		if (parameterCount > 64)
		{
			return null;
		}
		return delegate(object[] arguments)
		{
			object[] array = new object[parameterCount];
			for (int i = 0; i < parameterCount; i++)
			{
				array[i] = arguments[i];
			}
			try
			{
				return (T)constructor.Invoke(array);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException ?? ex;
			}
		};
	}

	public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3> CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
	{
		Type typeFromHandle = typeof(T);
		int parameterCount = constructor.GetParameters().Length;
		return delegate(TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			object[] array = new object[parameterCount];
			for (int i = 0; i < parameterCount; i++)
			{
				switch (i)
				{
				case 0:
					array[0] = arg0;
					break;
				case 1:
					array[1] = arg1;
					break;
				case 2:
					array[2] = arg2;
					break;
				case 3:
					array[3] = arg3;
					break;
				default:
					throw new InvalidOperationException();
				}
			}
			return (T)constructor.Invoke(array);
		};
	}

	public override Action<TCollection, object> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>()
	{
		Type typeFromHandle = typeof(TCollection);
		Type objectType = JsonTypeInfo.ObjectType;
		MethodInfo addMethod = typeFromHandle.GetMethod("Push") ?? typeFromHandle.GetMethod("Enqueue");
		return delegate(TCollection collection, object element)
		{
			addMethod.Invoke(collection, new object[1] { element });
		};
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>()
	{
		MethodInfo immutableEnumerableCreateRangeMethod = typeof(TCollection).GetImmutableEnumerableCreateRangeMethod(typeof(TElement));
		return (Func<IEnumerable<TElement>, TCollection>)immutableEnumerableCreateRangeMethod.CreateDelegate(typeof(Func<IEnumerable<TElement>, TCollection>));
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public override Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>()
	{
		MethodInfo immutableDictionaryCreateRangeMethod = typeof(TCollection).GetImmutableDictionaryCreateRangeMethod(typeof(TKey), typeof(TValue));
		return (Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection>)immutableDictionaryCreateRangeMethod.CreateDelegate(typeof(Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection>));
	}

	public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
	{
		MethodInfo getMethodInfo = propertyInfo.GetMethod;
		return (object obj) => (TProperty)getMethodInfo.Invoke(obj, null);
	}

	public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
	{
		MethodInfo setMethodInfo = propertyInfo.SetMethod;
		return delegate(object obj, TProperty value)
		{
			setMethodInfo.Invoke(obj, new object[1] { value });
		};
	}

	public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo)
	{
		return (object obj) => (TProperty)fieldInfo.GetValue(obj);
	}

	public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo)
	{
		return delegate(object obj, TProperty value)
		{
			fieldInfo.SetValue(obj, value);
		};
	}
}
