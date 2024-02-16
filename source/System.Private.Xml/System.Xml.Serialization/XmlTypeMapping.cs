namespace System.Xml.Serialization;

public class XmlTypeMapping : XmlMapping
{
	internal TypeMapping? Mapping => base.Accessor.Mapping;

	public string TypeName => Mapping.TypeDesc.Name;

	public string TypeFullName => Mapping.TypeDesc.FullName;

	public string? XsdTypeName => Mapping.TypeName;

	public string? XsdTypeNamespace => Mapping.Namespace;

	internal XmlTypeMapping(TypeScope scope, ElementAccessor accessor)
		: base(scope, accessor)
	{
	}
}
