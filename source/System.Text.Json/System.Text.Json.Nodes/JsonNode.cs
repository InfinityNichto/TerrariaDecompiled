using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Nodes;

public abstract class JsonNode
{
	private JsonNode _parent;

	private JsonNodeOptions? _options;

	public JsonNodeOptions? Options
	{
		get
		{
			if (!_options.HasValue && Parent != null)
			{
				_options = Parent.Options;
			}
			return _options;
		}
	}

	public JsonNode? Parent
	{
		get
		{
			return _parent;
		}
		internal set
		{
			_parent = value;
		}
	}

	public JsonNode Root
	{
		get
		{
			JsonNode parent = Parent;
			if (parent == null)
			{
				return this;
			}
			while (parent.Parent != null)
			{
				parent = parent.Parent;
			}
			return parent;
		}
	}

	public JsonNode? this[int index]
	{
		get
		{
			return AsArray().GetItem(index);
		}
		set
		{
			AsArray().SetItem(index, value);
		}
	}

	public JsonNode? this[string propertyName]
	{
		get
		{
			return AsObject().GetItem(propertyName);
		}
		set
		{
			AsObject().SetItem(propertyName, value);
		}
	}

	internal JsonNode(JsonNodeOptions? options = null)
	{
		_options = options;
	}

	public JsonArray AsArray()
	{
		if (this is JsonArray result)
		{
			return result;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeWrongType, "JsonArray"));
	}

	public JsonObject AsObject()
	{
		if (this is JsonObject result)
		{
			return result;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeWrongType, "JsonObject"));
	}

	public JsonValue AsValue()
	{
		if (this is JsonValue result)
		{
			return result;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeWrongType, "JsonValue"));
	}

	public string GetPath()
	{
		if (Parent == null)
		{
			return "$";
		}
		List<string> list = new List<string>();
		GetPath(list, null);
		StringBuilder stringBuilder = new StringBuilder("$");
		for (int num = list.Count - 1; num >= 0; num--)
		{
			stringBuilder.Append(list[num]);
		}
		return stringBuilder.ToString();
	}

	internal abstract void GetPath(List<string> path, JsonNode child);

	public virtual T GetValue<T>()
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeWrongType, "JsonValue"));
	}

	internal void AssignParent(JsonNode parent)
	{
		if (Parent != null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeAlreadyHasParent();
		}
		for (JsonNode jsonNode = parent; jsonNode != null; jsonNode = jsonNode.Parent)
		{
			if (jsonNode == this)
			{
				ThrowHelper.ThrowInvalidOperationException_NodeCycleDetected();
			}
		}
		Parent = parent;
	}

	public static implicit operator JsonNode(bool value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(bool? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(byte value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(byte? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(char value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(char? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(DateTime value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(DateTime? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(DateTimeOffset value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(DateTimeOffset? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(decimal value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(decimal? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(double value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(double? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(Guid value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(Guid? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(short value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(short? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(int value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(int? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(long value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(long? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(sbyte value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(sbyte? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(float value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(float? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(string? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(ushort value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(ushort? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(uint value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(uint? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(ulong value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(ulong? value)
	{
		return JsonValue.Create(value);
	}

	public static explicit operator bool(JsonNode value)
	{
		return value.GetValue<bool>();
	}

	public static explicit operator bool?(JsonNode? value)
	{
		return value?.GetValue<bool>();
	}

	public static explicit operator byte(JsonNode value)
	{
		return value.GetValue<byte>();
	}

	public static explicit operator byte?(JsonNode? value)
	{
		return value?.GetValue<byte>();
	}

	public static explicit operator char(JsonNode value)
	{
		return value.GetValue<char>();
	}

	public static explicit operator char?(JsonNode? value)
	{
		return value?.GetValue<char>();
	}

	public static explicit operator DateTime(JsonNode value)
	{
		return value.GetValue<DateTime>();
	}

	public static explicit operator DateTime?(JsonNode? value)
	{
		return value?.GetValue<DateTime>();
	}

	public static explicit operator DateTimeOffset(JsonNode value)
	{
		return value.GetValue<DateTimeOffset>();
	}

	public static explicit operator DateTimeOffset?(JsonNode? value)
	{
		return value?.GetValue<DateTimeOffset>();
	}

	public static explicit operator decimal(JsonNode value)
	{
		return value.GetValue<decimal>();
	}

	public static explicit operator decimal?(JsonNode? value)
	{
		return value?.GetValue<decimal>();
	}

	public static explicit operator double(JsonNode value)
	{
		return value.GetValue<double>();
	}

	public static explicit operator double?(JsonNode? value)
	{
		return value?.GetValue<double>();
	}

	public static explicit operator Guid(JsonNode value)
	{
		return value.GetValue<Guid>();
	}

	public static explicit operator Guid?(JsonNode? value)
	{
		return value?.GetValue<Guid>();
	}

	public static explicit operator short(JsonNode value)
	{
		return value.GetValue<short>();
	}

	public static explicit operator short?(JsonNode? value)
	{
		return value?.GetValue<short>();
	}

	public static explicit operator int(JsonNode value)
	{
		return value.GetValue<int>();
	}

	public static explicit operator int?(JsonNode? value)
	{
		return value?.GetValue<int>();
	}

	public static explicit operator long(JsonNode value)
	{
		return value.GetValue<long>();
	}

	public static explicit operator long?(JsonNode? value)
	{
		return value?.GetValue<long>();
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(JsonNode value)
	{
		return value.GetValue<sbyte>();
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte?(JsonNode? value)
	{
		return value?.GetValue<sbyte>();
	}

	public static explicit operator float(JsonNode value)
	{
		return value.GetValue<float>();
	}

	public static explicit operator float?(JsonNode? value)
	{
		return value?.GetValue<float>();
	}

	public static explicit operator string?(JsonNode? value)
	{
		return value?.GetValue<string>();
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(JsonNode value)
	{
		return value.GetValue<ushort>();
	}

	[CLSCompliant(false)]
	public static explicit operator ushort?(JsonNode? value)
	{
		return value?.GetValue<ushort>();
	}

	[CLSCompliant(false)]
	public static explicit operator uint(JsonNode value)
	{
		return value.GetValue<uint>();
	}

	[CLSCompliant(false)]
	public static explicit operator uint?(JsonNode? value)
	{
		return value?.GetValue<uint>();
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(JsonNode value)
	{
		return value.GetValue<ulong>();
	}

	[CLSCompliant(false)]
	public static explicit operator ulong?(JsonNode? value)
	{
		return value?.GetValue<ulong>();
	}

	public static JsonNode? Parse(ref Utf8JsonReader reader, JsonNodeOptions? nodeOptions = null)
	{
		JsonElement element = JsonElement.ParseValue(ref reader);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static JsonNode? Parse(string json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
	{
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		JsonElement element = JsonElement.ParseValue(json, documentOptions);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static JsonNode? Parse(ReadOnlySpan<byte> utf8Json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
	{
		JsonElement element = JsonElement.ParseValue(utf8Json, documentOptions);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static JsonNode? Parse(Stream utf8Json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		JsonElement element = JsonElement.ParseValue(utf8Json, documentOptions);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public string ToJsonString(JsonSerializerOptions? options = null)
	{
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(16384);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options?.GetWriterOptions() ?? default(JsonWriterOptions)))
		{
			WriteTo(writer, options);
		}
		return JsonHelpers.Utf8GetString(pooledByteBufferWriter.WrittenMemory.ToArray());
	}

	public override string ToString()
	{
		if (this is JsonValue)
		{
			if (this is JsonValue<string> jsonValue)
			{
				return jsonValue.Value;
			}
			if (this is JsonValue<JsonElement> { Value: { ValueKind: JsonValueKind.String }, Value: var value2 })
			{
				return value2.GetString();
			}
		}
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(16384);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, new JsonWriterOptions
		{
			Indented = true
		}))
		{
			WriteTo(writer);
		}
		return JsonHelpers.Utf8GetString(pooledByteBufferWriter.WrittenMemory.ToArray());
	}

	public abstract void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null);
}
