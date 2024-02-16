using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization;

internal sealed class DataMember
{
	private sealed class CriticalHelper
	{
		private DataContract _memberTypeContract;

		private string _name;

		private int _order;

		private bool _isRequired;

		private bool _emitDefaultValue;

		private bool _isNullable;

		private bool _isGetOnlyCollection;

		private readonly MemberInfo _memberInfo;

		private bool _hasConflictingNameAndType;

		private DataMember _conflictingMember;

		private Type _memberType;

		private PrimitiveDataContract _memberPrimitiveContract;

		internal MemberInfo MemberInfo => _memberInfo;

		internal string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		internal int Order
		{
			get
			{
				return _order;
			}
			set
			{
				_order = value;
			}
		}

		internal bool IsRequired
		{
			get
			{
				return _isRequired;
			}
			set
			{
				_isRequired = value;
			}
		}

		internal bool EmitDefaultValue
		{
			get
			{
				return _emitDefaultValue;
			}
			set
			{
				_emitDefaultValue = value;
			}
		}

		internal bool IsNullable
		{
			get
			{
				return _isNullable;
			}
			set
			{
				_isNullable = value;
			}
		}

		internal bool IsGetOnlyCollection
		{
			get
			{
				return _isGetOnlyCollection;
			}
			set
			{
				_isGetOnlyCollection = value;
			}
		}

		internal Type MemberType
		{
			get
			{
				if (_memberType == null)
				{
					FieldInfo fieldInfo = MemberInfo as FieldInfo;
					if (fieldInfo != null)
					{
						_memberType = fieldInfo.FieldType;
					}
					else
					{
						_memberType = ((PropertyInfo)MemberInfo).PropertyType;
					}
				}
				return _memberType;
			}
		}

		internal DataContract MemberTypeContract
		{
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (_memberTypeContract == null)
				{
					if (IsGetOnlyCollection)
					{
						_memberTypeContract = DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(MemberType.TypeHandle), MemberType.TypeHandle, MemberType, SerializationMode.SharedContract);
					}
					else
					{
						_memberTypeContract = DataContract.GetDataContract(MemberType);
					}
				}
				return _memberTypeContract;
			}
			set
			{
				_memberTypeContract = value;
			}
		}

		internal bool HasConflictingNameAndType
		{
			get
			{
				return _hasConflictingNameAndType;
			}
			set
			{
				_hasConflictingNameAndType = value;
			}
		}

		internal DataMember ConflictingMember
		{
			get
			{
				return _conflictingMember;
			}
			set
			{
				_conflictingMember = value;
			}
		}

		internal PrimitiveDataContract MemberPrimitiveContract
		{
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (_memberPrimitiveContract == PrimitiveDataContract.NullContract)
				{
					_memberPrimitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(MemberType);
				}
				return _memberPrimitiveContract;
			}
		}

		internal CriticalHelper(MemberInfo memberInfo)
		{
			_emitDefaultValue = true;
			_memberInfo = memberInfo;
			_memberPrimitiveContract = PrimitiveDataContract.NullContract;
		}
	}

	private readonly CriticalHelper _helper;

	private FastInvokerBuilder.Getter _getter;

	private FastInvokerBuilder.Setter _setter;

	internal MemberInfo MemberInfo => _helper.MemberInfo;

	public string Name
	{
		get
		{
			return _helper.Name;
		}
		set
		{
			_helper.Name = value;
		}
	}

	public int Order
	{
		get
		{
			return _helper.Order;
		}
		set
		{
			_helper.Order = value;
		}
	}

	public bool IsRequired
	{
		get
		{
			return _helper.IsRequired;
		}
		set
		{
			_helper.IsRequired = value;
		}
	}

	public bool EmitDefaultValue
	{
		get
		{
			return _helper.EmitDefaultValue;
		}
		set
		{
			_helper.EmitDefaultValue = value;
		}
	}

	public bool IsNullable
	{
		get
		{
			return _helper.IsNullable;
		}
		set
		{
			_helper.IsNullable = value;
		}
	}

	public bool IsGetOnlyCollection
	{
		get
		{
			return _helper.IsGetOnlyCollection;
		}
		set
		{
			_helper.IsGetOnlyCollection = value;
		}
	}

	internal Type MemberType => _helper.MemberType;

	internal DataContract MemberTypeContract
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.MemberTypeContract;
		}
	}

	internal PrimitiveDataContract MemberPrimitiveContract
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.MemberPrimitiveContract;
		}
	}

	public bool HasConflictingNameAndType
	{
		get
		{
			return _helper.HasConflictingNameAndType;
		}
		set
		{
			_helper.HasConflictingNameAndType = value;
		}
	}

	internal DataMember ConflictingMember
	{
		get
		{
			return _helper.ConflictingMember;
		}
		set
		{
			_helper.ConflictingMember = value;
		}
	}

	internal FastInvokerBuilder.Getter Getter
	{
		get
		{
			if (_getter == null)
			{
				_getter = FastInvokerBuilder.CreateGetter(MemberInfo);
			}
			return _getter;
		}
	}

	internal FastInvokerBuilder.Setter Setter
	{
		get
		{
			if (_setter == null)
			{
				_setter = FastInvokerBuilder.CreateSetter(MemberInfo);
			}
			return _setter;
		}
	}

	internal DataMember(MemberInfo memberInfo)
	{
		_helper = new CriticalHelper(memberInfo);
	}

	internal bool RequiresMemberAccessForGet()
	{
		MemberInfo memberInfo = MemberInfo;
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if (fieldInfo != null)
		{
			return DataContract.FieldRequiresMemberAccess(fieldInfo);
		}
		PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
		MethodInfo getMethod = propertyInfo.GetMethod;
		if (getMethod != null)
		{
			if (!DataContract.MethodRequiresMemberAccess(getMethod))
			{
				return !DataContract.IsTypeVisible(propertyInfo.PropertyType);
			}
			return true;
		}
		return false;
	}

	internal bool RequiresMemberAccessForSet()
	{
		MemberInfo memberInfo = MemberInfo;
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if (fieldInfo != null)
		{
			return DataContract.FieldRequiresMemberAccess(fieldInfo);
		}
		PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
		MethodInfo setMethod = propertyInfo.SetMethod;
		if (setMethod != null)
		{
			if (!DataContract.MethodRequiresMemberAccess(setMethod))
			{
				return !DataContract.IsTypeVisible(propertyInfo.PropertyType);
			}
			return true;
		}
		return false;
	}
}
