namespace System.Runtime.Serialization;

internal sealed class ArgBuilder
{
	internal int Index;

	internal Type ArgType;

	internal ArgBuilder(int index, Type argType)
	{
		Index = index;
		ArgType = argType;
	}
}
