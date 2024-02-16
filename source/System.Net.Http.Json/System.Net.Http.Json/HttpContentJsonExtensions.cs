using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

public static class HttpContentJsonExtensions
{
	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		Encoding encoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);
		return ReadFromJsonAsyncCore(content, type, encoding, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		Encoding encoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);
		return ReadFromJsonAsyncCore<T>(content, encoding, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private static async Task<object> ReadFromJsonAsyncCore(HttpContent content, Type type, Encoding sourceEncoding, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		using (Stream contentStream2 = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			return await DeserializeAsyncHelper(contentStream2, type, options ?? JsonHelpers.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
		static ValueTask<object> DeserializeAsyncHelper(Stream contentStream, Type returnType, JsonSerializerOptions options, CancellationToken cancellationToken)
		{
			return JsonSerializer.DeserializeAsync(contentStream, returnType, options, cancellationToken);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, Encoding sourceEncoding, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await DeserializeAsyncHelper<T>(contentStream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
	private static ValueTask<TValue> DeserializeAsyncHelper<TValue>(Stream contentStream, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		return JsonSerializer.DeserializeAsync<TValue>(contentStream, options, cancellationToken);
	}

	public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		Encoding encoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);
		return ReadFromJsonAsyncCore(content, type, encoding, context, cancellationToken);
	}

	public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		Encoding encoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);
		return ReadFromJsonAsyncCore(content, encoding, jsonTypeInfo, cancellationToken);
	}

	private static async Task<object> ReadFromJsonAsyncCore(HttpContent content, Type type, Encoding sourceEncoding, JsonSerializerContext context, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await JsonSerializer.DeserializeAsync(contentStream, type, context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, Encoding sourceEncoding, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await JsonSerializer.DeserializeAsync(contentStream, jsonTypeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<Stream> GetContentStream(HttpContent content, Encoding sourceEncoding, CancellationToken cancellationToken)
	{
		Stream stream = await ReadHttpContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
		{
			stream = GetTranscodingStream(stream, sourceEncoding);
		}
		return stream;
	}

	private static Task<Stream> ReadHttpContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
	{
		return content.ReadAsStreamAsync(cancellationToken);
	}

	private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
	{
		return Encoding.CreateTranscodingStream(contentStream, sourceEncoding, Encoding.UTF8);
	}
}
