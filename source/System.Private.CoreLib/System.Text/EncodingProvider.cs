using System.Collections.Generic;
using System.Threading;

namespace System.Text;

public abstract class EncodingProvider
{
	private static volatile EncodingProvider[] s_providers;

	public EncodingProvider()
	{
	}

	public abstract Encoding? GetEncoding(string name);

	public abstract Encoding? GetEncoding(int codepage);

	public virtual Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
	{
		Encoding encoding = GetEncoding(name);
		if (encoding != null)
		{
			encoding = (Encoding)encoding.Clone();
			encoding.EncoderFallback = encoderFallback;
			encoding.DecoderFallback = decoderFallback;
		}
		return encoding;
	}

	public virtual Encoding? GetEncoding(int codepage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
	{
		Encoding encoding = GetEncoding(codepage);
		if (encoding != null)
		{
			encoding = (Encoding)encoding.Clone();
			encoding.EncoderFallback = encoderFallback;
			encoding.DecoderFallback = decoderFallback;
		}
		return encoding;
	}

	public virtual IEnumerable<EncodingInfo> GetEncodings()
	{
		return Array.Empty<EncodingInfo>();
	}

	internal static void AddProvider(EncodingProvider provider)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (s_providers == null && Interlocked.CompareExchange(ref s_providers, new EncodingProvider[1] { provider }, null) == null)
		{
			return;
		}
		EncodingProvider[] array;
		EncodingProvider[] array2;
		do
		{
			array = s_providers;
			if (Array.IndexOf(array, provider) >= 0)
			{
				break;
			}
			array2 = new EncodingProvider[array.Length + 1];
			Array.Copy(array, array2, array.Length);
			array2[^1] = provider;
		}
		while (Interlocked.CompareExchange(ref s_providers, array2, array) != array);
	}

	internal static Encoding GetEncodingFromProvider(int codepage)
	{
		EncodingProvider[] array = s_providers;
		if (array == null)
		{
			return null;
		}
		EncodingProvider[] array2 = array;
		foreach (EncodingProvider encodingProvider in array2)
		{
			Encoding encoding = encodingProvider.GetEncoding(codepage);
			if (encoding != null)
			{
				return encoding;
			}
		}
		return null;
	}

	internal static Dictionary<int, EncodingInfo> GetEncodingListFromProviders()
	{
		EncodingProvider[] array = s_providers;
		if (array == null)
		{
			return null;
		}
		Dictionary<int, EncodingInfo> dictionary = new Dictionary<int, EncodingInfo>();
		EncodingProvider[] array2 = array;
		foreach (EncodingProvider encodingProvider in array2)
		{
			IEnumerable<EncodingInfo> encodings = encodingProvider.GetEncodings();
			if (encodings == null)
			{
				continue;
			}
			foreach (EncodingInfo item in encodings)
			{
				dictionary.TryAdd(item.CodePage, item);
			}
		}
		return dictionary;
	}

	internal static Encoding GetEncodingFromProvider(string encodingName)
	{
		if (s_providers == null)
		{
			return null;
		}
		EncodingProvider[] array = s_providers;
		EncodingProvider[] array2 = array;
		foreach (EncodingProvider encodingProvider in array2)
		{
			Encoding encoding = encodingProvider.GetEncoding(encodingName);
			if (encoding != null)
			{
				return encoding;
			}
		}
		return null;
	}

	internal static Encoding GetEncodingFromProvider(int codepage, EncoderFallback enc, DecoderFallback dec)
	{
		if (s_providers == null)
		{
			return null;
		}
		EncodingProvider[] array = s_providers;
		EncodingProvider[] array2 = array;
		foreach (EncodingProvider encodingProvider in array2)
		{
			Encoding encoding = encodingProvider.GetEncoding(codepage, enc, dec);
			if (encoding != null)
			{
				return encoding;
			}
		}
		return null;
	}

	internal static Encoding GetEncodingFromProvider(string encodingName, EncoderFallback enc, DecoderFallback dec)
	{
		if (s_providers == null)
		{
			return null;
		}
		EncodingProvider[] array = s_providers;
		EncodingProvider[] array2 = array;
		foreach (EncodingProvider encodingProvider in array2)
		{
			Encoding encoding = encodingProvider.GetEncoding(encodingName, enc, dec);
			if (encoding != null)
			{
				return encoding;
			}
		}
		return null;
	}
}
