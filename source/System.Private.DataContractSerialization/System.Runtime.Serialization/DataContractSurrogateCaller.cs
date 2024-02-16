using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

internal static class DataContractSurrogateCaller
{
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Type GetDataContractType(ISerializationSurrogateProvider surrogateProvider, Type type)
	{
		if (DataContract.GetBuiltInDataContract(type) != null)
		{
			return type;
		}
		return surrogateProvider.GetSurrogateType(type) ?? type;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	[return: NotNullIfNotNull("obj")]
	internal static object GetObjectToSerialize(ISerializationSurrogateProvider surrogateProvider, object obj, Type objType, Type membertype)
	{
		if (obj == null)
		{
			return null;
		}
		if (DataContract.GetBuiltInDataContract(objType) != null)
		{
			return obj;
		}
		return surrogateProvider.GetObjectToSerialize(obj, membertype);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	[return: NotNullIfNotNull("obj")]
	internal static object GetDeserializedObject(ISerializationSurrogateProvider surrogateProvider, object obj, Type objType, Type memberType)
	{
		if (obj == null)
		{
			return null;
		}
		if (DataContract.GetBuiltInDataContract(objType) != null)
		{
			return obj;
		}
		return surrogateProvider.GetDeserializedObject(obj, memberType);
	}
}
