using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Windows.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
[TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
public sealed class ValueSerializerAttribute : Attribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private Type _valueSerializerType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private readonly string _valueSerializerTypeName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public Type ValueSerializerType
	{
		get
		{
			if (_valueSerializerType == null && _valueSerializerTypeName != null)
			{
				_valueSerializerType = Type.GetType(_valueSerializerTypeName);
			}
			return _valueSerializerType;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public string ValueSerializerTypeName
	{
		get
		{
			if (_valueSerializerType != null)
			{
				return _valueSerializerType.AssemblyQualifiedName;
			}
			return _valueSerializerTypeName;
		}
	}

	public ValueSerializerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type valueSerializerType)
	{
		_valueSerializerType = valueSerializerType;
	}

	public ValueSerializerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string valueSerializerTypeName)
	{
		_valueSerializerTypeName = valueSerializerTypeName;
	}
}
