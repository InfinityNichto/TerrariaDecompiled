using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

internal interface IGenericNameProvider
{
	bool ParametersFromBuiltInNamespaces
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get;
	}

	int GetParameterCount();

	IList<int> GetNestedParameterCounts();

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	string GetParameterName(int paramIndex);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	string GetNamespaces();

	string GetGenericTypeName();
}
