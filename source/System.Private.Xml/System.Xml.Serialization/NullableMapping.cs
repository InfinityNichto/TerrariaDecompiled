namespace System.Xml.Serialization;

internal sealed class NullableMapping : TypeMapping
{
	private TypeMapping _baseMapping;

	internal TypeMapping BaseMapping
	{
		get
		{
			return _baseMapping;
		}
		set
		{
			_baseMapping = value;
		}
	}

	internal override string DefaultElementName => BaseMapping.DefaultElementName;
}
