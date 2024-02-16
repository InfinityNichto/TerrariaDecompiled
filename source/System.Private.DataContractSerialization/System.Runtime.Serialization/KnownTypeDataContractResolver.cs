using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class KnownTypeDataContractResolver : DataContractResolver
{
	private readonly XmlObjectSerializerContext _context;

	internal KnownTypeDataContractResolver(XmlObjectSerializerContext context)
	{
		_context = context;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
	{
		if (type == null)
		{
			typeName = null;
			typeNamespace = null;
			return false;
		}
		if (declaredType != null && declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
		{
			typeName = null;
			typeNamespace = null;
			return true;
		}
		DataContract dataContract = DataContract.GetDataContract(type);
		if (_context.IsKnownType(dataContract, dataContract.KnownDataContracts, declaredType))
		{
			typeName = dataContract.Name;
			typeNamespace = dataContract.Namespace;
			return true;
		}
		typeName = null;
		typeNamespace = null;
		return false;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
	{
		if (typeName == null || typeNamespace == null)
		{
			return null;
		}
		return _context.ResolveNameFromKnownTypes(new XmlQualifiedName(typeName, typeNamespace));
	}
}
