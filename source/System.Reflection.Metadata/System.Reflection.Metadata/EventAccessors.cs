using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public readonly struct EventAccessors
{
	private readonly int _adderRowId;

	private readonly int _removerRowId;

	private readonly int _raiserRowId;

	private readonly ImmutableArray<MethodDefinitionHandle> _others;

	public MethodDefinitionHandle Adder => MethodDefinitionHandle.FromRowId(_adderRowId);

	public MethodDefinitionHandle Remover => MethodDefinitionHandle.FromRowId(_removerRowId);

	public MethodDefinitionHandle Raiser => MethodDefinitionHandle.FromRowId(_raiserRowId);

	public ImmutableArray<MethodDefinitionHandle> Others => _others;

	internal EventAccessors(int adderRowId, int removerRowId, int raiserRowId, ImmutableArray<MethodDefinitionHandle> others)
	{
		_adderRowId = adderRowId;
		_removerRowId = removerRowId;
		_raiserRowId = raiserRowId;
		_others = others;
	}
}
