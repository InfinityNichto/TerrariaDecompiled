using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class SpecialModel : TypeModel
{
	internal SpecialModel([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, TypeDesc typeDesc, ModelScope scope)
		: base(type, typeDesc, scope)
	{
	}
}
