using System.Collections;

namespace System.Runtime.Serialization;

internal sealed class SurrogateHashtable : Hashtable
{
	internal SurrogateHashtable(int size)
		: base(size)
	{
	}

	protected override bool KeyEquals(object key, object item)
	{
		SurrogateKey surrogateKey = (SurrogateKey)item;
		SurrogateKey surrogateKey2 = (SurrogateKey)key;
		if (surrogateKey2._type == surrogateKey._type && (surrogateKey2._context.State & surrogateKey._context.State) == surrogateKey._context.State)
		{
			return surrogateKey2._context.Context == surrogateKey._context.Context;
		}
		return false;
	}
}
