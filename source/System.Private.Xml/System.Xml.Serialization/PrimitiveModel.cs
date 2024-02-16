using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class PrimitiveModel : TypeModel
{
	internal PrimitiveModel([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, TypeDesc typeDesc, ModelScope scope)
		: base(type, typeDesc, scope)
	{
	}
}
