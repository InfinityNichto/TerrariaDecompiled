using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

public static class HttpClientJsonExtensions
{
	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore(async, type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore(async, type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore<TValue>(async, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore<TValue>(async, options, cancellationToken);
	}

	public static Task<object?> GetFromJsonAsync(this HttpClient client, string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore(async, type, context, cancellationToken);
	}

	public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore(async, type, context, cancellationToken);
	}

	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore(async, jsonTypeInfo, cancellationToken);
	}

	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		Task<HttpResponseMessage> async = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		return GetFromJsonAsyncCore(async, jsonTypeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, string? requestUri, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(requestUri, type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(requestUri, type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, string? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private static async Task<object> GetFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		using (HttpResponseMessage response = await taskResponse.ConfigureAwait(continueOnCapturedContext: false))
		{
			response.EnsureSuccessStatusCode();
			return await ReadFromJsonAsyncHelper(response.Content, type, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
		static Task<object> ReadFromJsonAsyncHelper(HttpContent content, Type type, JsonSerializerOptions options, CancellationToken cancellationToken)
		{
			return content.ReadFromJsonAsync(type, options, cancellationToken);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private static async Task<T> GetFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await taskResponse.ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		return await ReadFromJsonAsyncHelper<T>(response.Content, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
	private static Task<T> ReadFromJsonAsyncHelper<T>(HttpContent content, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		return content.ReadFromJsonAsync<T>(options, cancellationToken);
	}

	private static async Task<object> GetFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerContext context, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await taskResponse.ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync(type, context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<T> GetFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await taskResponse.ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PostAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PostAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent<TValue> content = new JsonContent<TValue>(value, jsonTypeInfo);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent<TValue> content = new JsonContent<TValue>(value, jsonTypeInfo);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PutAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PutAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent<TValue> content = new JsonContent<TValue>(value, jsonTypeInfo);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent<TValue> content = new JsonContent<TValue>(value, jsonTypeInfo);
		return client.PutAsync(requestUri, content, cancellationToken);
	}
}
