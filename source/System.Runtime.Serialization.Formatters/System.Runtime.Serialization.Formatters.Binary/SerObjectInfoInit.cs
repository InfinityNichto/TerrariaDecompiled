using System.Collections.Generic;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class SerObjectInfoInit
{
	internal readonly Dictionary<Type, SerObjectInfoCache> _seenBeforeTable = new Dictionary<Type, SerObjectInfoCache>();

	internal int _objectInfoIdCount = 1;

	internal SerStack _oiPool = new SerStack("SerObjectInfo Pool");
}
