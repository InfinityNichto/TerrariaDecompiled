using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class ArrayModel : TypeModel
{
	internal TypeModel Element
	{
		[RequiresUnreferencedCode("Calls GetTypeModel")]
		get
		{
			return base.ModelScope.GetTypeModel(TypeScope.GetArrayElementType(base.Type, null));
		}
	}

	internal ArrayModel([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, TypeDesc typeDesc, ModelScope scope)
		: base(type, typeDesc, scope)
	{
	}
}
