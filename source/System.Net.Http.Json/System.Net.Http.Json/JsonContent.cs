using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

public sealed class JsonContent : HttpContent
{
	private readonly JsonSerializerOptions _jsonSerializerOptions;

	public Type ObjectType { get; }

	public object? Value { get; }

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private JsonContent(object inputValue, Type inputType, MediaTypeHeaderValue mediaType, JsonSerializerOptions options)
	{
		if (inputType == null)
		{
			throw new ArgumentNullException("inputType");
		}
		if (inputValue != null && !inputType.IsAssignableFrom(inputValue.GetType()))
		{
			throw new ArgumentException(System.SR.Format(System.SR.SerializeWrongType, inputType, inputValue.GetType()));
		}
		Value = inputValue;
		ObjectType = inputType;
		base.Headers.ContentType = mediaType ?? JsonHelpers.GetDefaultMediaType();
		_jsonSerializerOptions = options ?? JsonHelpers.s_defaultSerializerOptions;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
	{
		return Create(inputValue, typeof(T), mediaType, options);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
	{
		return new JsonContent(inputValue, inputType, mediaType, options);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		return SerializeToStreamAsyncCore(stream, async: true, CancellationToken.None);
	}

	protected override bool TryComputeLength(out long length)
	{
		length = 0L;
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is annotated with RequiresUnreferencedCode.")]
	private async Task SerializeToStreamAsyncCore(Stream targetStream, bool async, CancellationToken cancellationToken)
	{
		Encoding encoding = JsonHelpers.GetEncoding(base.Headers.ContentType?.CharSet);
		if (encoding != null && encoding != Encoding.UTF8)
		{
			Stream transcodingStream = Encoding.CreateTranscodingStream(targetStream, encoding, Encoding.UTF8, leaveOpen: true);
			try
			{
				if (async)
				{
					await SerializeAsyncHelper(transcodingStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					SerializeSyncHelper(transcodingStream, Value, ObjectType, _jsonSerializerOptions);
				}
			}
			finally
			{
				if (async)
				{
					await transcodingStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					transcodingStream.Dispose();
				}
			}
		}
		else if (async)
		{
			await SerializeAsyncHelper(targetStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			SerializeSyncHelper(targetStream, Value, ObjectType, _jsonSerializerOptions);
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
		static Task SerializeAsyncHelper(Stream utf8Json, object value, Type inputType, JsonSerializerOptions options, CancellationToken cancellationToken)
		{
			return JsonSerializer.SerializeAsync(utf8Json, value, inputType, options, cancellationToken);
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
		static void SerializeSyncHelper(Stream utf8Json, object value, Type inputType, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize(utf8Json, value, inputType, options);
		}
	}

	protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		SerializeToStreamAsyncCore(stream, async: false, cancellationToken).GetAwaiter().GetResult();
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		return SerializeToStreamAsyncCore(stream, async: true, cancellationToken);
	}
}
internal sealed class JsonContent<TValue> : HttpContent
{
	private readonly JsonTypeInfo<TValue> _typeInfo;

	private readonly TValue _typedValue;

	public JsonContent(TValue inputValue, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		_typeInfo = jsonTypeInfo ?? throw new ArgumentNullException("jsonTypeInfo");
		_typedValue = inputValue;
		base.Headers.ContentType = JsonHelpers.GetDefaultMediaType();
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
	{
		return SerializeToStreamAsyncCore(stream, async: true, CancellationToken.None);
	}

	protected override bool TryComputeLength(out long length)
	{
		length = 0L;
		return false;
	}

	private async Task SerializeToStreamAsyncCore(Stream targetStream, bool async, CancellationToken cancellationToken)
	{
		Encoding encoding = JsonHelpers.GetEncoding(base.Headers.ContentType?.CharSet);
		if (encoding != null && encoding != Encoding.UTF8)
		{
			Stream transcodingStream = Encoding.CreateTranscodingStream(targetStream, encoding, Encoding.UTF8, leaveOpen: true);
			try
			{
				if (async)
				{
					await JsonSerializer.SerializeAsync(transcodingStream, _typedValue, _typeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					JsonSerializer.Serialize(transcodingStream, _typedValue, _typeInfo);
				}
			}
			finally
			{
				if (async)
				{
					await transcodingStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					transcodingStream.Dispose();
				}
			}
		}
		else if (async)
		{
			await JsonSerializer.SerializeAsync(targetStream, _typedValue, _typeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			JsonSerializer.Serialize(targetStream, _typedValue, _typeInfo);
		}
	}

	protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		SerializeToStreamAsyncCore(stream, async: false, cancellationToken).GetAwaiter().GetResult();
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		return SerializeToStreamAsyncCore(stream, async: true, cancellationToken);
	}
}
