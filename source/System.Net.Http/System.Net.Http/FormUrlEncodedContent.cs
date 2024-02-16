using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class FormUrlEncodedContent : ByteArrayContent
{
	public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
		: base(GetContentByteArray(nameValueCollection))
	{
		base.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
	}

	private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
	{
		if (nameValueCollection == null)
		{
			throw new ArgumentNullException("nameValueCollection");
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> item in nameValueCollection)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('&');
			}
			stringBuilder.Append(Encode(item.Key));
			stringBuilder.Append('=');
			stringBuilder.Append(Encode(item.Value));
		}
		return HttpRuleParser.DefaultHttpEncoding.GetBytes(stringBuilder.ToString());
	}

	private static string Encode(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return string.Empty;
		}
		return Uri.EscapeDataString(data).Replace("%20", "+");
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(FormUrlEncodedContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, cancellationToken);
	}

	internal override Stream TryCreateContentReadStream()
	{
		if (!(GetType() == typeof(FormUrlEncodedContent)))
		{
			return null;
		}
		return CreateMemoryStreamForByteArray();
	}
}
