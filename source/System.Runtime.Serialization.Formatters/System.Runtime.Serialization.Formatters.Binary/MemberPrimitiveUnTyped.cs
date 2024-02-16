namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class MemberPrimitiveUnTyped : IStreamable
{
	internal InternalPrimitiveTypeE _typeInformation;

	internal object _value;

	internal MemberPrimitiveUnTyped()
	{
	}

	internal void Set(InternalPrimitiveTypeE typeInformation, object value)
	{
		_typeInformation = typeInformation;
		_value = value;
	}

	internal void Set(InternalPrimitiveTypeE typeInformation)
	{
		_typeInformation = typeInformation;
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteValue(_typeInformation, _value);
	}

	public void Read(BinaryParser input)
	{
		_value = input.ReadValue(_typeInformation);
	}
}
