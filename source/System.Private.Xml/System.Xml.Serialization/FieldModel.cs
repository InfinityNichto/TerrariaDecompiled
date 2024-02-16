using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Serialization;

internal sealed class FieldModel
{
	private readonly SpecifiedAccessor _checkSpecified;

	private readonly MemberInfo _memberInfo;

	private readonly MemberInfo _checkSpecifiedMemberInfo;

	private readonly MethodInfo _checkShouldPersistMethodInfo;

	private readonly bool _checkShouldPersist;

	private readonly bool _readOnly;

	private readonly bool _isProperty;

	private readonly Type _fieldType;

	private readonly string _name;

	private readonly TypeDesc _fieldTypeDesc;

	internal string Name => _name;

	internal Type FieldType => _fieldType;

	internal TypeDesc FieldTypeDesc => _fieldTypeDesc;

	internal bool CheckShouldPersist => _checkShouldPersist;

	internal SpecifiedAccessor CheckSpecified => _checkSpecified;

	internal MemberInfo MemberInfo => _memberInfo;

	internal MemberInfo CheckSpecifiedMemberInfo => _checkSpecifiedMemberInfo;

	internal MethodInfo CheckShouldPersistMethodInfo => _checkShouldPersistMethodInfo;

	internal bool ReadOnly => _readOnly;

	internal bool IsProperty => _isProperty;

	internal FieldModel(string name, Type fieldType, TypeDesc fieldTypeDesc, bool checkSpecified, bool checkShouldPersist)
		: this(name, fieldType, fieldTypeDesc, checkSpecified, checkShouldPersist, readOnly: false)
	{
	}

	internal FieldModel(string name, Type fieldType, TypeDesc fieldTypeDesc, bool checkSpecified, bool checkShouldPersist, bool readOnly)
	{
		_fieldTypeDesc = fieldTypeDesc;
		_name = name;
		_fieldType = fieldType;
		_checkSpecified = (checkSpecified ? SpecifiedAccessor.ReadWrite : SpecifiedAccessor.None);
		_checkShouldPersist = checkShouldPersist;
		_readOnly = readOnly;
	}

	[RequiresUnreferencedCode("Calls GetField on MemberInfo type")]
	internal FieldModel(MemberInfo memberInfo, Type fieldType, TypeDesc fieldTypeDesc)
	{
		_name = memberInfo.Name;
		_fieldType = fieldType;
		_fieldTypeDesc = fieldTypeDesc;
		_memberInfo = memberInfo;
		_checkShouldPersistMethodInfo = memberInfo.DeclaringType.GetMethod("ShouldSerialize" + memberInfo.Name, Type.EmptyTypes);
		_checkShouldPersist = _checkShouldPersistMethodInfo != null;
		FieldInfo field = memberInfo.DeclaringType.GetField(memberInfo.Name + "Specified");
		if (field != null)
		{
			if (field.FieldType != typeof(bool))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidSpecifiedType, field.Name, field.FieldType.FullName, typeof(bool).FullName));
			}
			_checkSpecified = (field.IsInitOnly ? SpecifiedAccessor.ReadOnly : SpecifiedAccessor.ReadWrite);
			_checkSpecifiedMemberInfo = field;
		}
		else
		{
			PropertyInfo property = memberInfo.DeclaringType.GetProperty(memberInfo.Name + "Specified");
			if (property != null)
			{
				if (StructModel.CheckPropertyRead(property))
				{
					_checkSpecified = ((!property.CanWrite) ? SpecifiedAccessor.ReadOnly : SpecifiedAccessor.ReadWrite);
					_checkSpecifiedMemberInfo = property;
				}
				if (_checkSpecified != 0 && property.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidSpecifiedType, property.Name, property.PropertyType.FullName, typeof(bool).FullName));
				}
			}
		}
		if (memberInfo is PropertyInfo)
		{
			_readOnly = !((PropertyInfo)memberInfo).CanWrite;
			_isProperty = true;
		}
		else if (memberInfo is FieldInfo)
		{
			_readOnly = ((FieldInfo)memberInfo).IsInitOnly;
		}
	}
}
