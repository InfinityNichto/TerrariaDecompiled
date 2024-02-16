using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

public sealed class PortablePdbBuilder
{
	private Blob _pdbIdBlob;

	private readonly MethodDefinitionHandle _entryPoint;

	private readonly MetadataBuilder _builder;

	private readonly SerializedMetadata _serializedMetadata;

	public string MetadataVersion => "PDB v1.0";

	public ushort FormatVersion => 256;

	public Func<IEnumerable<Blob>, BlobContentId> IdProvider { get; }

	public PortablePdbBuilder(MetadataBuilder tablesAndHeaps, ImmutableArray<int> typeSystemRowCounts, MethodDefinitionHandle entryPoint, Func<IEnumerable<Blob>, BlobContentId>? idProvider = null)
	{
		if (tablesAndHeaps == null)
		{
			Throw.ArgumentNull("tablesAndHeaps");
		}
		ValidateTypeSystemRowCounts(typeSystemRowCounts);
		_builder = tablesAndHeaps;
		_entryPoint = entryPoint;
		_serializedMetadata = tablesAndHeaps.GetSerializedMetadata(typeSystemRowCounts, MetadataVersion.Length, isStandaloneDebugMetadata: true);
		IdProvider = idProvider ?? BlobContentId.GetTimeBasedProvider();
	}

	private static void ValidateTypeSystemRowCounts(ImmutableArray<int> typeSystemRowCounts)
	{
		if (typeSystemRowCounts.IsDefault)
		{
			Throw.ArgumentNull("typeSystemRowCounts");
		}
		if (typeSystemRowCounts.Length != MetadataTokens.TableCount)
		{
			throw new ArgumentException(System.SR.Format(System.SR.ExpectedArrayOfSize, MetadataTokens.TableCount), "typeSystemRowCounts");
		}
		for (int i = 0; i < typeSystemRowCounts.Length; i++)
		{
			if (typeSystemRowCounts[i] != 0)
			{
				if (((uint)typeSystemRowCounts[i] & 0xFF000000u) != 0)
				{
					throw new ArgumentOutOfRangeException("typeSystemRowCounts", System.SR.Format(System.SR.RowCountOutOfRange, i));
				}
				if (((1L << i) & 0x1FC93FB7FF57L) == 0L)
				{
					throw new ArgumentException(System.SR.Format(System.SR.RowCountMustBeZero, i), "typeSystemRowCounts");
				}
			}
		}
	}

	private void SerializeStandalonePdbStream(BlobBuilder builder)
	{
		int count = builder.Count;
		_pdbIdBlob = builder.ReserveBytes(20);
		builder.WriteInt32((!_entryPoint.IsNil) ? MetadataTokens.GetToken(_entryPoint) : 0);
		builder.WriteUInt64(_serializedMetadata.Sizes.ExternalTablesMask);
		MetadataWriterUtilities.SerializeRowCounts(builder, _serializedMetadata.Sizes.ExternalRowCounts);
		int count2 = builder.Count;
	}

	public BlobContentId Serialize(BlobBuilder builder)
	{
		if (builder == null)
		{
			Throw.ArgumentNull("builder");
		}
		MetadataBuilder.SerializeMetadataHeader(builder, MetadataVersion, _serializedMetadata.Sizes);
		SerializeStandalonePdbStream(builder);
		_builder.SerializeMetadataTables(builder, _serializedMetadata.Sizes, _serializedMetadata.StringMap, 0, 0);
		_builder.WriteHeapsTo(builder, _serializedMetadata.StringHeap);
		BlobContentId result = IdProvider(builder.GetBlobs());
		BlobWriter blobWriter = new BlobWriter(_pdbIdBlob);
		blobWriter.WriteGuid(result.Guid);
		blobWriter.WriteUInt32(result.Stamp);
		return result;
	}
}
