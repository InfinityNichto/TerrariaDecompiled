using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public abstract class TypeInfo : Type, IReflectableType
{
	public virtual Type[] GenericTypeParameters
	{
		get
		{
			if (!IsGenericTypeDefinition)
			{
				return Type.EmptyTypes;
			}
			return GetGenericArguments();
		}
	}

	public virtual IEnumerable<ConstructorInfo> DeclaredConstructors
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		get
		{
			return GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual IEnumerable<EventInfo> DeclaredEvents
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
		get
		{
			return GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual IEnumerable<FieldInfo> DeclaredFields
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
		get
		{
			return GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual IEnumerable<MemberInfo> DeclaredMembers
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		get
		{
			return GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual IEnumerable<MethodInfo> DeclaredMethods
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		get
		{
			return GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual IEnumerable<TypeInfo> DeclaredNestedTypes
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		get
		{
			Type[] array = GetDeclaredOnlyNestedTypes(this);
			foreach (Type type2 in array)
			{
				yield return type2.GetTypeInfo();
			}
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The yield return state machine doesn't propagate annotations")]
			static Type[] GetDeclaredOnlyNestedTypes(Type type)
			{
				return type.GetNestedTypes(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}
	}

	public virtual IEnumerable<PropertyInfo> DeclaredProperties
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
		get
		{
			return GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual IEnumerable<Type> ImplementedInterfaces
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		get
		{
			return GetInterfaces();
		}
	}

	TypeInfo IReflectableType.GetTypeInfo()
	{
		return this;
	}

	public virtual Type AsType()
	{
		return this;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public virtual EventInfo? GetDeclaredEvent(string name)
	{
		return GetEvent(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public virtual FieldInfo? GetDeclaredField(string name)
	{
		return GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public virtual MethodInfo? GetDeclaredMethod(string name)
	{
		return GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public virtual TypeInfo? GetDeclaredNestedType(string name)
	{
		return GetNestedType(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetTypeInfo();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public virtual PropertyInfo? GetDeclaredProperty(string name)
	{
		return GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public virtual IEnumerable<MethodInfo> GetDeclaredMethods(string name)
	{
		MethodInfo[] array = GetDeclaredOnlyMethods(this);
		foreach (MethodInfo methodInfo in array)
		{
			if (methodInfo.Name == name)
			{
				yield return methodInfo;
			}
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The yield return state machine doesn't propagate annotations")]
		static MethodInfo[] GetDeclaredOnlyMethods(Type type)
		{
			return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public virtual bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		if (this == typeInfo)
		{
			return true;
		}
		if (typeInfo.IsSubclassOf(this))
		{
			return true;
		}
		if (base.IsInterface)
		{
			return typeInfo.ImplementInterface(this);
		}
		if (IsGenericParameter)
		{
			Type[] genericParameterConstraints = GetGenericParameterConstraints();
			for (int i = 0; i < genericParameterConstraints.Length; i++)
			{
				if (!genericParameterConstraints[i].IsAssignableFrom(typeInfo))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	internal static string GetRankString(int rank)
	{
		if (rank <= 0)
		{
			throw new IndexOutOfRangeException();
		}
		if (rank != 1)
		{
			return "[" + new string(',', rank - 1) + "]";
		}
		return "[*]";
	}
}
