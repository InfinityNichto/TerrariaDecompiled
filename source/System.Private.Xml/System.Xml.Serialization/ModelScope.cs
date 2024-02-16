using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class ModelScope
{
	private readonly TypeScope _typeScope;

	private readonly Dictionary<Type, TypeModel> _models = new Dictionary<Type, TypeModel>();

	private readonly Dictionary<Type, TypeModel> _arrayModels = new Dictionary<Type, TypeModel>();

	internal TypeScope TypeScope => _typeScope;

	internal ModelScope(TypeScope typeScope)
	{
		_typeScope = typeScope;
	}

	[RequiresUnreferencedCode("calls GetTypeModel")]
	internal TypeModel GetTypeModel(Type type)
	{
		return GetTypeModel(type, directReference: true);
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	internal TypeModel GetTypeModel(Type type, bool directReference)
	{
		if (_models.TryGetValue(type, out var value))
		{
			return value;
		}
		TypeDesc typeDesc = _typeScope.GetTypeDesc(type, null, directReference);
		switch (typeDesc.Kind)
		{
		case TypeKind.Enum:
			value = new EnumModel(type, typeDesc, this);
			break;
		case TypeKind.Primitive:
			value = new PrimitiveModel(type, typeDesc, this);
			break;
		case TypeKind.Array:
		case TypeKind.Collection:
		case TypeKind.Enumerable:
			value = new ArrayModel(type, typeDesc, this);
			break;
		case TypeKind.Root:
		case TypeKind.Struct:
		case TypeKind.Class:
			value = new StructModel(type, typeDesc, this);
			break;
		default:
			if (!typeDesc.IsSpecial)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedTypeKind, type.FullName));
			}
			value = new SpecialModel(type, typeDesc, this);
			break;
		}
		_models.Add(type, value);
		return value;
	}

	[RequiresUnreferencedCode("calls GetArrayTypeDesc")]
	internal ArrayModel GetArrayModel(Type type)
	{
		if (!_arrayModels.TryGetValue(type, out var value))
		{
			value = GetTypeModel(type);
			if (!(value is ArrayModel))
			{
				TypeDesc arrayTypeDesc = _typeScope.GetArrayTypeDesc(type);
				value = new ArrayModel(type, arrayTypeDesc, this);
			}
			_arrayModels.Add(type, value);
		}
		return (ArrayModel)value;
	}
}
