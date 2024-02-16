using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal abstract class TypeModel
{
	private readonly TypeDesc _typeDesc;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private readonly Type _type;

	private readonly ModelScope _scope;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal Type Type => _type;

	internal ModelScope ModelScope => _scope;

	internal TypeDesc TypeDesc => _typeDesc;

	protected TypeModel([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, TypeDesc typeDesc, ModelScope scope)
	{
		_scope = scope;
		_type = type;
		_typeDesc = typeDesc;
	}
}
