using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Xsl.Runtime;

internal sealed class EarlyBoundInfo
{
	private readonly string _namespaceUri;

	private readonly ConstructorInfo _constrInfo;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	private readonly Type _ebType;

	public string NamespaceUri => _namespaceUri;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type EarlyBoundType => _ebType;

	public EarlyBoundInfo(string namespaceUri, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type ebType)
	{
		_namespaceUri = namespaceUri;
		_ebType = ebType;
		_constrInfo = ebType.GetConstructor(Type.EmptyTypes);
	}

	public object CreateObject()
	{
		return _constrInfo.Invoke(Array.Empty<object>());
	}

	public override bool Equals(object obj)
	{
		if (!(obj is EarlyBoundInfo earlyBoundInfo))
		{
			return false;
		}
		if (_namespaceUri == earlyBoundInfo._namespaceUri)
		{
			return _constrInfo == earlyBoundInfo._constrInfo;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _namespaceUri.GetHashCode();
	}
}
