using System.Resources;
using FxResources.System.IO.Compression;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_Enum => GetResourceString("ArgumentOutOfRange_Enum");

	internal static string CannotReadFromDeflateStream => GetResourceString("CannotReadFromDeflateStream");

	internal static string CannotWriteToDeflateStream => GetResourceString("CannotWriteToDeflateStream");

	internal static string GenericInvalidData => GetResourceString("GenericInvalidData");

	internal static string InvalidBeginCall => GetResourceString("InvalidBeginCall");

	internal static string InvalidBlockLength => GetResourceString("InvalidBlockLength");

	internal static string InvalidHuffmanData => GetResourceString("InvalidHuffmanData");

	internal static string NotSupported => GetResourceString("NotSupported");

	internal static string NotSupported_UnreadableStream => GetResourceString("NotSupported_UnreadableStream");

	internal static string NotSupported_UnwritableStream => GetResourceString("NotSupported_UnwritableStream");

	internal static string ObjectDisposed_StreamClosed => GetResourceString("ObjectDisposed_StreamClosed");

	internal static string UnknownBlockType => GetResourceString("UnknownBlockType");

	internal static string UnknownState => GetResourceString("UnknownState");

	internal static string ZLibErrorDLLLoadError => GetResourceString("ZLibErrorDLLLoadError");

	internal static string ZLibErrorInconsistentStream => GetResourceString("ZLibErrorInconsistentStream");

	internal static string ZLibErrorIncorrectInitParameters => GetResourceString("ZLibErrorIncorrectInitParameters");

	internal static string ZLibErrorNotEnoughMemory => GetResourceString("ZLibErrorNotEnoughMemory");

	internal static string ZLibErrorVersionMismatch => GetResourceString("ZLibErrorVersionMismatch");

	internal static string ZLibErrorUnexpected => GetResourceString("ZLibErrorUnexpected");

	internal static string CannotBeEmpty => GetResourceString("CannotBeEmpty");

	internal static string CDCorrupt => GetResourceString("CDCorrupt");

	internal static string CentralDirectoryInvalid => GetResourceString("CentralDirectoryInvalid");

	internal static string CreateInReadMode => GetResourceString("CreateInReadMode");

	internal static string CreateModeCapabilities => GetResourceString("CreateModeCapabilities");

	internal static string CreateModeCreateEntryWhileOpen => GetResourceString("CreateModeCreateEntryWhileOpen");

	internal static string CreateModeWriteOnceAndOneEntryAtATime => GetResourceString("CreateModeWriteOnceAndOneEntryAtATime");

	internal static string DateTimeOutOfRange => GetResourceString("DateTimeOutOfRange");

	internal static string DeletedEntry => GetResourceString("DeletedEntry");

	internal static string DeleteOnlyInUpdate => GetResourceString("DeleteOnlyInUpdate");

	internal static string DeleteOpenEntry => GetResourceString("DeleteOpenEntry");

	internal static string EntriesInCreateMode => GetResourceString("EntriesInCreateMode");

	internal static string EntryNameEncodingNotSupported => GetResourceString("EntryNameEncodingNotSupported");

	internal static string EntryNamesTooLong => GetResourceString("EntryNamesTooLong");

	internal static string EntryTooLarge => GetResourceString("EntryTooLarge");

	internal static string EOCDNotFound => GetResourceString("EOCDNotFound");

	internal static string FieldTooBigCompressedSize => GetResourceString("FieldTooBigCompressedSize");

	internal static string FieldTooBigLocalHeaderOffset => GetResourceString("FieldTooBigLocalHeaderOffset");

	internal static string FieldTooBigNumEntries => GetResourceString("FieldTooBigNumEntries");

	internal static string FieldTooBigOffsetToCD => GetResourceString("FieldTooBigOffsetToCD");

	internal static string FieldTooBigOffsetToZip64EOCD => GetResourceString("FieldTooBigOffsetToZip64EOCD");

	internal static string FieldTooBigStartDiskNumber => GetResourceString("FieldTooBigStartDiskNumber");

	internal static string FieldTooBigUncompressedSize => GetResourceString("FieldTooBigUncompressedSize");

	internal static string FrozenAfterWrite => GetResourceString("FrozenAfterWrite");

	internal static string HiddenStreamName => GetResourceString("HiddenStreamName");

	internal static string LengthAfterWrite => GetResourceString("LengthAfterWrite");

	internal static string LocalFileHeaderCorrupt => GetResourceString("LocalFileHeaderCorrupt");

	internal static string NumEntriesWrong => GetResourceString("NumEntriesWrong");

	internal static string ReadingNotSupported => GetResourceString("ReadingNotSupported");

	internal static string ReadModeCapabilities => GetResourceString("ReadModeCapabilities");

	internal static string ReadOnlyArchive => GetResourceString("ReadOnlyArchive");

	internal static string SeekingNotSupported => GetResourceString("SeekingNotSupported");

	internal static string SetLengthRequiresSeekingAndWriting => GetResourceString("SetLengthRequiresSeekingAndWriting");

	internal static string SplitSpanned => GetResourceString("SplitSpanned");

	internal static string UnexpectedEndOfStream => GetResourceString("UnexpectedEndOfStream");

	internal static string UnsupportedCompression => GetResourceString("UnsupportedCompression");

	internal static string UnsupportedCompressionMethod => GetResourceString("UnsupportedCompressionMethod");

	internal static string UpdateModeCapabilities => GetResourceString("UpdateModeCapabilities");

	internal static string UpdateModeOneStream => GetResourceString("UpdateModeOneStream");

	internal static string WritingNotSupported => GetResourceString("WritingNotSupported");

	internal static string Zip64EOCDNotWhereExpected => GetResourceString("Zip64EOCDNotWhereExpected");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}
}
