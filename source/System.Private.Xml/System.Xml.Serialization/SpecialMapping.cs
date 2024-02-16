namespace System.Xml.Serialization;

internal class SpecialMapping : TypeMapping
{
	private bool _namedAny;

	internal bool NamedAny
	{
		get
		{
			return _namedAny;
		}
		set
		{
			_namedAny = value;
		}
	}
}
