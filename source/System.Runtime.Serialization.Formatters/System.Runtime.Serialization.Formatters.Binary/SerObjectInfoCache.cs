using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class SerObjectInfoCache
{
	internal readonly string _fullTypeName;

	internal readonly string _assemblyString;

	internal readonly bool _hasTypeForwardedFrom;

	internal MemberInfo[] _memberInfos;

	internal string[] _memberNames;

	internal Type[] _memberTypes;

	internal SerObjectInfoCache(string typeName, string assemblyName, bool hasTypeForwardedFrom)
	{
		_fullTypeName = typeName;
		_assemblyString = assemblyName;
		_hasTypeForwardedFrom = hasTypeForwardedFrom;
	}

	internal SerObjectInfoCache(Type type)
	{
		TypeInformation typeInformation = BinaryFormatter.GetTypeInformation(type);
		_fullTypeName = typeInformation.FullTypeName;
		_assemblyString = typeInformation.AssemblyString;
		_hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
	}
}
