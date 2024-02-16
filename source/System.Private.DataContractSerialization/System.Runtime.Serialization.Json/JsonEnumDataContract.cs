using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonEnumDataContract : JsonDataContract
{
	private sealed class JsonEnumDataContractCriticalHelper : JsonDataContractCriticalHelper
	{
		private readonly bool _isULong;

		public bool IsULong => _isULong;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public JsonEnumDataContractCriticalHelper(EnumDataContract traditionalEnumDataContract)
			: base(traditionalEnumDataContract)
		{
			_isULong = traditionalEnumDataContract.IsULong;
		}
	}

	private readonly JsonEnumDataContractCriticalHelper _helper;

	public bool IsULong => _helper.IsULong;

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonEnumDataContract(EnumDataContract traditionalDataContract)
		: base(new JsonEnumDataContractCriticalHelper(traditionalDataContract))
	{
		_helper = base.Helper as JsonEnumDataContractCriticalHelper;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
	{
		object obj = ((!IsULong) ? Enum.ToObject(base.TraditionalDataContract.UnderlyingType, jsonReader.ReadElementContentAsLong()) : Enum.ToObject(base.TraditionalDataContract.UnderlyingType, jsonReader.ReadElementContentAsUnsignedLong()));
		context?.AddNewObject(obj);
		return obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
	{
		if (IsULong)
		{
			jsonWriter.WriteUnsignedLong(Convert.ToUInt64(obj));
		}
		else
		{
			jsonWriter.WriteLong(Convert.ToInt64(obj));
		}
	}
}
