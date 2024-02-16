namespace System.Xml.Serialization;

internal abstract class Mapping
{
	private bool _isSoap;

	internal bool IsSoap
	{
		get
		{
			return _isSoap;
		}
		set
		{
			_isSoap = value;
		}
	}

	internal Mapping()
	{
	}

	protected Mapping(Mapping mapping)
	{
		_isSoap = mapping._isSoap;
	}
}
