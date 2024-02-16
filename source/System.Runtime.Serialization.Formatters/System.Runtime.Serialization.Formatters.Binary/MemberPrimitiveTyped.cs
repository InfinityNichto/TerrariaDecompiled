namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class MemberPrimitiveTyped : IStreamable
{
	internal InternalPrimitiveTypeE _primitiveTypeEnum;

	internal object _value;

	internal MemberPrimitiveTyped()
	{
	}

	internal void Set(InternalPrimitiveTypeE primitiveTypeEnum, object value)
	{
		_primitiveTypeEnum = primitiveTypeEnum;
		_value = value;
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(8);
		output.WriteByte((byte)_primitiveTypeEnum);
		output.WriteValue(_primitiveTypeEnum, _value);
	}

	public void Read(BinaryParser input)
	{
		_primitiveTypeEnum = (InternalPrimitiveTypeE)input.ReadByte();
		_value = input.ReadValue(_primitiveTypeEnum);
	}
}
