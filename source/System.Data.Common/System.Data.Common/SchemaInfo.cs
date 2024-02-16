using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common;

internal struct SchemaInfo
{
	public string name;

	public string typeName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public Type type;
}
