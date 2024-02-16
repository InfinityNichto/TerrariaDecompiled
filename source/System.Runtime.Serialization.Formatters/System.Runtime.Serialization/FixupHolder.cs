namespace System.Runtime.Serialization;

internal sealed class FixupHolder
{
	internal long _id;

	internal object _fixupInfo;

	internal readonly int _fixupType;

	internal FixupHolder(long id, object fixupInfo, int fixupType)
	{
		_id = id;
		_fixupInfo = fixupInfo;
		_fixupType = fixupType;
	}
}
