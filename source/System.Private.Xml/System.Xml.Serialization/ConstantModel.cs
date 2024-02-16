using System.Reflection;

namespace System.Xml.Serialization;

internal sealed class ConstantModel
{
	private readonly FieldInfo _fieldInfo;

	private readonly long _value;

	internal string Name => _fieldInfo.Name;

	internal long Value => _value;

	internal FieldInfo FieldInfo => _fieldInfo;

	internal ConstantModel(FieldInfo fieldInfo, long value)
	{
		_fieldInfo = fieldInfo;
		_value = value;
	}
}
