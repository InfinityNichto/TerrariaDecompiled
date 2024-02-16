namespace System.Xml.Serialization;

public class XmlMemberMapping
{
	private readonly MemberMapping _mapping;

	internal MemberMapping Mapping => _mapping;

	internal Accessor? Accessor => _mapping.Accessor;

	public bool Any => Accessor.Any;

	public string ElementName => System.Xml.Serialization.Accessor.UnescapeName(Accessor.Name);

	public string XsdElementName => Accessor.Name;

	public string? Namespace => Accessor.Namespace;

	public string MemberName => _mapping.Name;

	public string? TypeName
	{
		get
		{
			if (Accessor.Mapping == null)
			{
				return string.Empty;
			}
			return Accessor.Mapping.TypeName;
		}
	}

	public string? TypeNamespace
	{
		get
		{
			if (Accessor.Mapping == null)
			{
				return null;
			}
			return Accessor.Mapping.Namespace;
		}
	}

	public string TypeFullName => _mapping.TypeDesc.FullName;

	public bool CheckSpecified => _mapping.CheckSpecified != SpecifiedAccessor.None;

	internal XmlMemberMapping(MemberMapping mapping)
	{
		_mapping = mapping;
	}
}
