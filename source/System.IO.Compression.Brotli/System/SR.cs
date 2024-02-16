using System.Resources;
using FxResources.System.IO.Compression.Brotli;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Stream_FalseCanRead => GetResourceString("Stream_FalseCanRead");

	internal static string Stream_FalseCanWrite => GetResourceString("Stream_FalseCanWrite");

	internal static string ArgumentOutOfRange_Enum => GetResourceString("ArgumentOutOfRange_Enum");

	internal static string ObjectDisposed_StreamClosed => GetResourceString("ObjectDisposed_StreamClosed");

	internal static string InvalidBeginCall => GetResourceString("InvalidBeginCall");

	internal static string BrotliEncoder_Create => GetResourceString("BrotliEncoder_Create");

	internal static string BrotliEncoder_Disposed => GetResourceString("BrotliEncoder_Disposed");

	internal static string BrotliEncoder_Quality => GetResourceString("BrotliEncoder_Quality");

	internal static string BrotliEncoder_Window => GetResourceString("BrotliEncoder_Window");

	internal static string BrotliEncoder_InvalidSetParameter => GetResourceString("BrotliEncoder_InvalidSetParameter");

	internal static string BrotliDecoder_Create => GetResourceString("BrotliDecoder_Create");

	internal static string BrotliDecoder_Disposed => GetResourceString("BrotliDecoder_Disposed");

	internal static string BrotliStream_Compress_UnsupportedOperation => GetResourceString("BrotliStream_Compress_UnsupportedOperation");

	internal static string BrotliStream_Compress_InvalidData => GetResourceString("BrotliStream_Compress_InvalidData");

	internal static string BrotliStream_Decompress_UnsupportedOperation => GetResourceString("BrotliStream_Decompress_UnsupportedOperation");

	internal static string BrotliStream_Decompress_InvalidData => GetResourceString("BrotliStream_Decompress_InvalidData");

	internal static string BrotliStream_Decompress_InvalidStream => GetResourceString("BrotliStream_Decompress_InvalidStream");

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

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}
}
