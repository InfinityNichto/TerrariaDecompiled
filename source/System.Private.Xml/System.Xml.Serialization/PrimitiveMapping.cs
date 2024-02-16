namespace System.Xml.Serialization;

internal class PrimitiveMapping : TypeMapping
{
	private bool _isList;

	internal override bool IsList
	{
		get
		{
			return _isList;
		}
		set
		{
			_isList = value;
		}
	}
}
