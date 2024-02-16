using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public readonly struct PropertyAccessors
{
	private readonly int _getterRowId;

	private readonly int _setterRowId;

	private readonly ImmutableArray<MethodDefinitionHandle> _others;

	public MethodDefinitionHandle Getter => MethodDefinitionHandle.FromRowId(_getterRowId);

	public MethodDefinitionHandle Setter => MethodDefinitionHandle.FromRowId(_setterRowId);

	public ImmutableArray<MethodDefinitionHandle> Others => _others;

	internal PropertyAccessors(int getterRowId, int setterRowId, ImmutableArray<MethodDefinitionHandle> others)
	{
		_getterRowId = getterRowId;
		_setterRowId = setterRowId;
		_others = others;
	}
}
