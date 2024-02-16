using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class TypeDesc
{
	private readonly string _name;

	private readonly string _fullName;

	private string _cSharpName;

	private TypeDesc _arrayElementTypeDesc;

	private TypeDesc _arrayTypeDesc;

	private TypeDesc _nullableTypeDesc;

	private readonly TypeKind _kind;

	private readonly XmlSchemaType _dataType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private Type _type;

	private TypeDesc _baseTypeDesc;

	private TypeFlags _flags;

	private readonly string _formatterName;

	private readonly bool _isXsdType;

	private bool _isMixed;

	private int _weight;

	private Exception _exception;

	internal TypeFlags Flags => _flags;

	internal bool IsXsdType => _isXsdType;

	internal bool IsMappedType => false;

	internal string Name => _name;

	internal string FullName => _fullName;

	internal string CSharpName
	{
		get
		{
			if (_cSharpName == null)
			{
				_cSharpName = ((_type == null) ? CodeIdentifier.GetCSharpName(_fullName) : CodeIdentifier.GetCSharpName(_type));
			}
			return _cSharpName;
		}
	}

	internal XmlSchemaType DataType => _dataType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal Type Type => _type;

	internal string FormatterName => _formatterName;

	internal TypeKind Kind => _kind;

	internal bool IsValueType => (_flags & TypeFlags.Reference) == 0;

	internal bool CanBeAttributeValue => (_flags & TypeFlags.CanBeAttributeValue) != 0;

	internal bool XmlEncodingNotRequired => (_flags & TypeFlags.XmlEncodingNotRequired) != 0;

	internal bool CanBeElementValue => (_flags & TypeFlags.CanBeElementValue) != 0;

	internal bool CanBeTextValue => (_flags & TypeFlags.CanBeTextValue) != 0;

	internal bool IsMixed
	{
		get
		{
			if (!_isMixed)
			{
				return CanBeTextValue;
			}
			return true;
		}
		set
		{
			_isMixed = value;
		}
	}

	internal bool IsSpecial => (_flags & TypeFlags.Special) != 0;

	internal bool IsAmbiguousDataType => (_flags & TypeFlags.AmbiguousDataType) != 0;

	internal bool HasCustomFormatter => (_flags & TypeFlags.HasCustomFormatter) != 0;

	internal bool HasDefaultSupport => (_flags & TypeFlags.IgnoreDefault) == 0;

	internal bool HasIsEmpty => (_flags & TypeFlags.HasIsEmpty) != 0;

	internal bool CollapseWhitespace => (_flags & TypeFlags.CollapseWhitespace) != 0;

	internal bool HasDefaultConstructor => (_flags & TypeFlags.HasDefaultConstructor) != 0;

	internal bool IsUnsupported => (_flags & TypeFlags.Unsupported) != 0;

	internal bool IsGenericInterface => (_flags & TypeFlags.GenericInterface) != 0;

	internal bool IsPrivateImplementation => (_flags & TypeFlags.UsePrivateImplementation) != 0;

	internal bool CannotNew
	{
		get
		{
			if (HasDefaultConstructor)
			{
				return ConstructorInaccessible;
			}
			return true;
		}
	}

	internal bool IsAbstract => (_flags & TypeFlags.Abstract) != 0;

	internal bool IsOptionalValue => (_flags & TypeFlags.OptionalValue) != 0;

	internal bool UseReflection => (_flags & TypeFlags.UseReflection) != 0;

	internal bool IsVoid => _kind == TypeKind.Void;

	internal bool IsClass => _kind == TypeKind.Class;

	internal bool IsStructLike
	{
		get
		{
			if (_kind != TypeKind.Struct)
			{
				return _kind == TypeKind.Class;
			}
			return true;
		}
	}

	internal bool IsArrayLike
	{
		get
		{
			if (_kind != TypeKind.Array && _kind != TypeKind.Collection)
			{
				return _kind == TypeKind.Enumerable;
			}
			return true;
		}
	}

	internal bool IsCollection => _kind == TypeKind.Collection;

	internal bool IsEnumerable => _kind == TypeKind.Enumerable;

	internal bool IsArray => _kind == TypeKind.Array;

	internal bool IsPrimitive => _kind == TypeKind.Primitive;

	internal bool IsEnum => _kind == TypeKind.Enum;

	internal bool IsNullable => !IsValueType;

	internal bool IsRoot => _kind == TypeKind.Root;

	internal bool ConstructorInaccessible => (_flags & TypeFlags.CtorInaccessible) != 0;

	internal Exception Exception
	{
		get
		{
			return _exception;
		}
		set
		{
			_exception = value;
		}
	}

	internal TypeDesc ArrayElementTypeDesc
	{
		get
		{
			return _arrayElementTypeDesc;
		}
		set
		{
			_arrayElementTypeDesc = value;
		}
	}

	internal int Weight => _weight;

	internal TypeDesc BaseTypeDesc
	{
		get
		{
			return _baseTypeDesc;
		}
		set
		{
			_baseTypeDesc = value;
			_weight = ((_baseTypeDesc != null) ? (_baseTypeDesc.Weight + 1) : 0);
		}
	}

	internal TypeDesc(string name, string fullName, XmlSchemaType dataType, TypeKind kind, TypeDesc baseTypeDesc, TypeFlags flags, string formatterName)
	{
		_name = name.Replace('+', '.');
		_fullName = fullName.Replace('+', '.');
		_kind = kind;
		_baseTypeDesc = baseTypeDesc;
		_flags = flags;
		_isXsdType = kind == TypeKind.Primitive;
		if (_isXsdType)
		{
			_weight = 1;
		}
		else if (kind == TypeKind.Enum)
		{
			_weight = 2;
		}
		else if (_kind == TypeKind.Root)
		{
			_weight = -1;
		}
		else
		{
			_weight = ((baseTypeDesc != null) ? (baseTypeDesc.Weight + 1) : 0);
		}
		_dataType = dataType;
		_formatterName = formatterName;
	}

	internal TypeDesc(string name, string fullName, TypeKind kind, TypeDesc baseTypeDesc, TypeFlags flags)
		: this(name, fullName, null, kind, baseTypeDesc, flags, null)
	{
	}

	internal TypeDesc([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, bool isXsdType, XmlSchemaType dataType, string formatterName, TypeFlags flags)
		: this(type.Name, type.FullName, dataType, TypeKind.Primitive, null, flags, formatterName)
	{
		_isXsdType = isXsdType;
		_type = type;
	}

	internal TypeDesc([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string name, string fullName, TypeKind kind, TypeDesc baseTypeDesc, TypeFlags flags, TypeDesc arrayElementTypeDesc)
		: this(name, fullName, null, kind, baseTypeDesc, flags, null)
	{
		_arrayElementTypeDesc = arrayElementTypeDesc;
		_type = type;
	}

	public override string ToString()
	{
		return _fullName;
	}

	internal TypeDesc GetNullableTypeDesc([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		if (IsOptionalValue)
		{
			return this;
		}
		if (_nullableTypeDesc == null)
		{
			_nullableTypeDesc = new TypeDesc("NullableOf" + _name, "System.Nullable`1[" + _fullName + "]", null, TypeKind.Struct, this, _flags | TypeFlags.OptionalValue, _formatterName);
			_nullableTypeDesc._type = type;
		}
		return _nullableTypeDesc;
	}

	internal void CheckSupported()
	{
		if (IsUnsupported)
		{
			if (Exception != null)
			{
				throw Exception;
			}
			throw new NotSupportedException(System.SR.Format(System.SR.XmlSerializerUnsupportedType, FullName));
		}
		if (_baseTypeDesc != null)
		{
			_baseTypeDesc.CheckSupported();
		}
		if (_arrayElementTypeDesc != null)
		{
			_arrayElementTypeDesc.CheckSupported();
		}
	}

	internal void CheckNeedConstructor()
	{
		if (!IsValueType && !IsAbstract && !HasDefaultConstructor)
		{
			_flags |= TypeFlags.Unsupported;
			_exception = new InvalidOperationException(System.SR.Format(System.SR.XmlConstructorInaccessible, FullName));
		}
	}

	internal TypeDesc CreateArrayTypeDesc()
	{
		if (_arrayTypeDesc == null)
		{
			_arrayTypeDesc = new TypeDesc(null, _name + "[]", _fullName + "[]", TypeKind.Array, null, TypeFlags.Reference | (_flags & TypeFlags.UseReflection), this);
		}
		return _arrayTypeDesc;
	}

	internal bool IsDerivedFrom(TypeDesc baseTypeDesc)
	{
		for (TypeDesc typeDesc = this; typeDesc != null; typeDesc = typeDesc.BaseTypeDesc)
		{
			if (typeDesc == baseTypeDesc)
			{
				return true;
			}
		}
		return baseTypeDesc.IsRoot;
	}

	internal static TypeDesc FindCommonBaseTypeDesc(TypeDesc[] typeDescs)
	{
		if (typeDescs.Length == 0)
		{
			return null;
		}
		TypeDesc typeDesc = null;
		int num = int.MaxValue;
		for (int i = 0; i < typeDescs.Length; i++)
		{
			int weight = typeDescs[i].Weight;
			if (weight < num)
			{
				num = weight;
				typeDesc = typeDescs[i];
			}
		}
		while (typeDesc != null)
		{
			int j;
			for (j = 0; j < typeDescs.Length && typeDescs[j].IsDerivedFrom(typeDesc); j++)
			{
			}
			if (j == typeDescs.Length)
			{
				break;
			}
			typeDesc = typeDesc.BaseTypeDesc;
		}
		return typeDesc;
	}
}
