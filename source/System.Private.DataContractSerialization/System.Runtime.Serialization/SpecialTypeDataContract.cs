using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class SpecialTypeDataContract : DataContract
{
	private sealed class SpecialTypeDataContractCriticalHelper : DataContractCriticalHelper
	{
		internal SpecialTypeDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString name, XmlDictionaryString ns)
			: base(type)
		{
			SetDataContractName(name, ns);
		}
	}

	private readonly SpecialTypeDataContractCriticalHelper _helper;

	public override bool IsBuiltInDataContract => true;

	public SpecialTypeDataContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString name, XmlDictionaryString ns)
		: base(new SpecialTypeDataContractCriticalHelper(type, name, ns))
	{
		_helper = base.Helper as SpecialTypeDataContractCriticalHelper;
	}
}
