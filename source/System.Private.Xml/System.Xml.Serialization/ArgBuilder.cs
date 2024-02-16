namespace System.Xml.Serialization;

internal sealed class ArgBuilder
{
	internal string Name;

	internal int Index;

	internal Type ArgType;

	internal ArgBuilder(string name, int index, Type argType)
	{
		Name = name;
		Index = index;
		ArgType = argType;
	}
}
