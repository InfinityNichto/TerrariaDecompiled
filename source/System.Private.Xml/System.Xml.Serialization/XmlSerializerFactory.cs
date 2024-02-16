using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

public class XmlSerializerFactory
{
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace)
	{
		return CreateSerializer(type, overrides, extraTypes, root, defaultNamespace, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type, XmlRootAttribute? root)
	{
		return CreateSerializer(type, null, Type.EmptyTypes, root, null, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type, Type[]? extraTypes)
	{
		return CreateSerializer(type, null, extraTypes, null, null, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides? overrides)
	{
		return CreateSerializer(type, overrides, Type.EmptyTypes, null, null, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(XmlTypeMapping xmlTypeMapping)
	{
		return new XmlSerializer(xmlTypeMapping);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type)
	{
		return CreateSerializer(type, (string?)null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type, string? defaultNamespace)
	{
		return new XmlSerializer(type, defaultNamespace);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace, string? location)
	{
		return new XmlSerializer(type, overrides, extraTypes, root, defaultNamespace, location);
	}
}
