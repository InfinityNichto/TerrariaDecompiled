using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public sealed class DebugMetadataHeader
{
	public ImmutableArray<byte> Id { get; }

	public MethodDefinitionHandle EntryPoint { get; }

	public int IdStartOffset { get; }

	internal DebugMetadataHeader(ImmutableArray<byte> id, MethodDefinitionHandle entryPoint, int idStartOffset)
	{
		Id = id;
		EntryPoint = entryPoint;
		IdStartOffset = idStartOffset;
	}
}
