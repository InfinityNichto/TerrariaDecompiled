using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace System.Net.Http.Json;

internal static class JsonHelpers
{
	internal static readonly JsonSerializerOptions s_defaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

	internal static MediaTypeHeaderValue GetDefaultMediaType()
	{
		return new MediaTypeHeaderValue("application/json")
		{
			CharSet = "utf-8"
		};
	}

	internal static Encoding GetEncoding(string charset)
	{
		Encoding result = null;
		if (charset != null)
		{
			try
			{
				result = ((charset.Length <= 2 || charset[0] != '"' || charset[charset.Length - 1] != '"') ? Encoding.GetEncoding(charset) : Encoding.GetEncoding(charset.Substring(1, charset.Length - 2)));
			}
			catch (ArgumentException innerException)
			{
				throw new InvalidOperationException(System.SR.CharSetInvalid, innerException);
			}
		}
		return result;
	}
}
