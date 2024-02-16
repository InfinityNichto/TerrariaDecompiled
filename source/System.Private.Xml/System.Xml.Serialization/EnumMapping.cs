namespace System.Xml.Serialization;

internal sealed class EnumMapping : PrimitiveMapping
{
	private ConstantMapping[] _constants;

	private bool _isFlags;

	internal bool IsFlags
	{
		get
		{
			return _isFlags;
		}
		set
		{
			_isFlags = value;
		}
	}

	internal ConstantMapping[] Constants
	{
		get
		{
			return _constants;
		}
		set
		{
			_constants = value;
		}
	}
}
