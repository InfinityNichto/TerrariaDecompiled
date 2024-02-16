using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization;

internal static class FastInvokerBuilder
{
	public delegate void Setter(ref object obj, object value);

	public delegate object Getter(object obj);

	private delegate void StructSetDelegate<T, TArg>(ref T obj, TArg value);

	private delegate TResult StructGetDelegate<T, out TResult>(ref T obj);

	private static readonly MethodInfo s_createGetterInternal = typeof(FastInvokerBuilder).GetMethod("CreateGetterInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly MethodInfo s_createSetterInternal = typeof(FastInvokerBuilder).GetMethod("CreateSetterInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly MethodInfo s_make = typeof(FastInvokerBuilder).GetMethod("Make", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that we are preserving the constructors of type which is what Make() is doing.")]
	public static Func<object> GetMakeNewInstanceFunc([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
	{
		return s_make.MakeGenericMethod(type).CreateDelegate<Func<object>>();
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that FastInvokerBuilder.CreateGetterInternal<T, T1> is not annotated.")]
	public static Getter CreateGetter(MemberInfo memberInfo)
	{
		if (memberInfo is PropertyInfo propertyInfo)
		{
			Func<PropertyInfo, Getter> func = s_createGetterInternal.MakeGenericMethod(propertyInfo.DeclaringType, propertyInfo.PropertyType).CreateDelegate<Func<PropertyInfo, Getter>>();
			return func(propertyInfo);
		}
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if ((object)fieldInfo != null)
		{
			return (object obj) => fieldInfo.GetValue(obj);
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name)));
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that FastInvokerBuilder.CreateSetterInternal<T, T1> is not annotated.")]
	public static Setter CreateSetter(MemberInfo memberInfo)
	{
		if (memberInfo is PropertyInfo)
		{
			PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
			if (propertyInfo.CanWrite)
			{
				Func<PropertyInfo, Setter> func = s_createSetterInternal.MakeGenericMethod(propertyInfo.DeclaringType, propertyInfo.PropertyType).CreateDelegate<Func<PropertyInfo, Setter>>();
				return func(propertyInfo);
			}
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.NoSetMethodForProperty, propertyInfo.DeclaringType, propertyInfo.Name)));
		}
		if (memberInfo is FieldInfo)
		{
			FieldInfo fieldInfo = (FieldInfo)memberInfo;
			return delegate(ref object obj, object val)
			{
				fieldInfo.SetValue(obj, val);
			};
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name)));
	}

	private static object Make<T>() where T : new()
	{
		T val = new T();
		return val;
	}

	private static Getter CreateGetterInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
	{
		if (typeof(DeclaringType).IsGenericType && typeof(DeclaringType).GetGenericTypeDefinition() == typeof(KeyValue<, >))
		{
			if (propInfo.Name == "Key")
			{
				return (object obj) => ((IKeyValue)obj).Key;
			}
			return (object obj) => ((IKeyValue)obj).Value;
		}
		if (typeof(DeclaringType).IsValueType)
		{
			StructGetDelegate<DeclaringType, PropertyType> getMethod2 = propInfo.GetMethod.CreateDelegate<StructGetDelegate<DeclaringType, PropertyType>>();
			return delegate(object obj)
			{
				DeclaringType obj2 = (DeclaringType)obj;
				return getMethod2(ref obj2);
			};
		}
		Func<DeclaringType, PropertyType> getMethod = propInfo.GetMethod.CreateDelegate<Func<DeclaringType, PropertyType>>();
		return (object obj) => getMethod((DeclaringType)obj);
	}

	private static Setter CreateSetterInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
	{
		if (typeof(DeclaringType).IsGenericType && typeof(DeclaringType).GetGenericTypeDefinition() == typeof(KeyValue<, >))
		{
			if (propInfo.Name == "Key")
			{
				return delegate(ref object obj, object val)
				{
					((IKeyValue)obj).Key = val;
				};
			}
			return delegate(ref object obj, object val)
			{
				((IKeyValue)obj).Value = val;
			};
		}
		if (typeof(DeclaringType).IsValueType)
		{
			StructSetDelegate<DeclaringType, PropertyType> setMethod2 = propInfo.SetMethod.CreateDelegate<StructSetDelegate<DeclaringType, PropertyType>>();
			return delegate(ref object obj, object val)
			{
				DeclaringType obj2 = (DeclaringType)obj;
				setMethod2(ref obj2, (PropertyType)val);
				obj = obj2;
			};
		}
		Action<DeclaringType, PropertyType> setMethod = propInfo.SetMethod.CreateDelegate<Action<DeclaringType, PropertyType>>();
		return delegate(ref object obj, object val)
		{
			setMethod((DeclaringType)obj, (PropertyType)val);
		};
	}
}
