using System.ComponentModel;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class PEReaderExtensions
{
	public static MethodBodyBlock GetMethodBody(this PEReader peReader, int relativeVirtualAddress)
	{
		if (peReader == null)
		{
			throw new ArgumentNullException("peReader");
		}
		PEMemoryBlock sectionData = peReader.GetSectionData(relativeVirtualAddress);
		if (sectionData.Length == 0)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidMethodRva, relativeVirtualAddress));
		}
		return MethodBodyBlock.Create(sectionData.GetReader());
	}

	public static MetadataReader GetMetadataReader(this PEReader peReader)
	{
		return peReader.GetMetadataReader(MetadataReaderOptions.Default, null);
	}

	public static MetadataReader GetMetadataReader(this PEReader peReader, MetadataReaderOptions options)
	{
		return peReader.GetMetadataReader(options, null);
	}

	public unsafe static MetadataReader GetMetadataReader(this PEReader peReader, MetadataReaderOptions options, MetadataStringDecoder? utf8Decoder)
	{
		if (peReader == null)
		{
			throw new ArgumentNullException("peReader");
		}
		PEMemoryBlock metadata = peReader.GetMetadata();
		return new MetadataReader(metadata.Pointer, metadata.Length, options, utf8Decoder, peReader);
	}
}
