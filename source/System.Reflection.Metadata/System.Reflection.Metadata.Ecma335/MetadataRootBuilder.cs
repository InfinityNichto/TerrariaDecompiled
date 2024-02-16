using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

public sealed class MetadataRootBuilder
{
	internal static readonly ImmutableArray<int> EmptyRowCounts = ImmutableArray.Create(new int[MetadataTokens.TableCount]);

	private readonly MetadataBuilder _tablesAndHeaps;

	private readonly SerializedMetadata _serializedMetadata;

	public string MetadataVersion { get; }

	public bool SuppressValidation { get; }

	public MetadataSizes Sizes => _serializedMetadata.Sizes;

	public MetadataRootBuilder(MetadataBuilder tablesAndHeaps, string? metadataVersion = null, bool suppressValidation = false)
	{
		if (tablesAndHeaps == null)
		{
			Throw.ArgumentNull("tablesAndHeaps");
		}
		int num = ((metadataVersion != null) ? BlobUtilities.GetUTF8ByteCount(metadataVersion) : "v4.0.30319".Length);
		if (num > 254)
		{
			Throw.InvalidArgument(System.SR.MetadataVersionTooLong, "metadataVersion");
		}
		_tablesAndHeaps = tablesAndHeaps;
		MetadataVersion = metadataVersion ?? "v4.0.30319";
		SuppressValidation = suppressValidation;
		_serializedMetadata = tablesAndHeaps.GetSerializedMetadata(EmptyRowCounts, num, isStandaloneDebugMetadata: false);
	}

	public void Serialize(BlobBuilder builder, int methodBodyStreamRva, int mappedFieldDataStreamRva)
	{
		if (builder == null)
		{
			Throw.ArgumentNull("builder");
		}
		if (methodBodyStreamRva < 0)
		{
			Throw.ArgumentOutOfRange("methodBodyStreamRva");
		}
		if (mappedFieldDataStreamRva < 0)
		{
			Throw.ArgumentOutOfRange("mappedFieldDataStreamRva");
		}
		if (!SuppressValidation)
		{
			_tablesAndHeaps.ValidateOrder();
		}
		MetadataBuilder.SerializeMetadataHeader(builder, MetadataVersion, _serializedMetadata.Sizes);
		_tablesAndHeaps.SerializeMetadataTables(builder, _serializedMetadata.Sizes, _serializedMetadata.StringMap, methodBodyStreamRva, mappedFieldDataStreamRva);
		_tablesAndHeaps.WriteHeapsTo(builder, _serializedMetadata.StringHeap);
	}
}
