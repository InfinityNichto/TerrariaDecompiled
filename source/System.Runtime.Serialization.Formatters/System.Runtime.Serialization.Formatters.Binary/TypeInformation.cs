namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class TypeInformation
{
	internal string FullTypeName { get; }

	internal string AssemblyString { get; }

	internal bool HasTypeForwardedFrom { get; }

	internal TypeInformation(string fullTypeName, string assemblyString, bool hasTypeForwardedFrom)
	{
		FullTypeName = fullTypeName;
		AssemblyString = assemblyString;
		HasTypeForwardedFrom = hasTypeForwardedFrom;
	}
}
