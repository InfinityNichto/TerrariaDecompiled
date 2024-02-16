using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Nodes;

[DebuggerDisplay("JsonObject[{Count}]")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class JsonObject : JsonNode, IDictionary<string, JsonNode?>, ICollection<KeyValuePair<string, JsonNode?>>, IEnumerable<KeyValuePair<string, JsonNode?>>, IEnumerable
{
	[ExcludeFromCodeCoverage]
	private class DebugView
	{
		[DebuggerDisplay("{Display,nq}")]
		private struct DebugViewProperty
		{
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonNode Value;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string PropertyName;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string Display
			{
				get
				{
					if (Value == null)
					{
						return PropertyName + " = null";
					}
					if (Value is JsonValue)
					{
						return PropertyName + " = " + Value.ToJsonString();
					}
					if (Value is JsonObject jsonObject)
					{
						return $"{PropertyName} = JsonObject[{jsonObject.Count}]";
					}
					JsonArray jsonArray = (JsonArray)Value;
					return $"{PropertyName} = JsonArray[{jsonArray.Count}]";
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private JsonObject _node;

		public string Json => _node.ToJsonString();

		public string Path => _node.GetPath();

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private DebugViewProperty[] Items
		{
			get
			{
				DebugViewProperty[] array = new DebugViewProperty[_node.Count];
				int num = 0;
				foreach (KeyValuePair<string, JsonNode> item in _node)
				{
					array[num].PropertyName = item.Key;
					array[num].Value = item.Value;
					num++;
				}
				return array;
			}
		}

		public DebugView(JsonObject node)
		{
			_node = node;
		}
	}

	private JsonElement? _jsonElement;

	private JsonPropertyDictionary<JsonNode> _dictionary;

	public int Count
	{
		get
		{
			InitializeIfRequired();
			return _dictionary.Count;
		}
	}

	ICollection<string> IDictionary<string, JsonNode>.Keys
	{
		get
		{
			InitializeIfRequired();
			return _dictionary.Keys;
		}
	}

	ICollection<JsonNode?> IDictionary<string, JsonNode>.Values
	{
		get
		{
			InitializeIfRequired();
			return _dictionary.Values;
		}
	}

	bool ICollection<KeyValuePair<string, JsonNode>>.IsReadOnly => false;

	public JsonObject(JsonNodeOptions? options = null)
		: base(options)
	{
	}

	public JsonObject(IEnumerable<KeyValuePair<string, JsonNode?>> properties, JsonNodeOptions? options = null)
	{
		foreach (KeyValuePair<string, JsonNode> property in properties)
		{
			Add(property.Key, property.Value);
		}
	}

	public static JsonObject? Create(JsonElement element, JsonNodeOptions? options = null)
	{
		if (element.ValueKind == JsonValueKind.Null)
		{
			return null;
		}
		if (element.ValueKind == JsonValueKind.Object)
		{
			return new JsonObject(element, options);
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeElementWrongType, "Object"));
	}

	internal JsonObject(JsonElement element, JsonNodeOptions? options = null)
		: base(options)
	{
		_jsonElement = element;
	}

	public bool TryGetPropertyValue(string propertyName, out JsonNode? jsonNode)
	{
		return ((IDictionary<string, JsonNode>)this).TryGetValue(propertyName, out jsonNode);
	}

	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (_jsonElement.HasValue)
		{
			_jsonElement.Value.WriteTo(writer);
			return;
		}
		if (options == null)
		{
			options = JsonSerializerOptions.s_defaultOptions;
		}
		writer.WriteStartObject();
		using (IEnumerator<KeyValuePair<string, JsonNode>> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, JsonNode> current = enumerator.Current;
				writer.WritePropertyName(current.Key);
				JsonNodeConverter.Instance.Write(writer, current.Value, options);
			}
		}
		writer.WriteEndObject();
	}

	internal JsonNode GetItem(string propertyName)
	{
		if (TryGetPropertyValue(propertyName, out JsonNode jsonNode))
		{
			return jsonNode;
		}
		return null;
	}

	internal override void GetPath(List<string> path, JsonNode child)
	{
		if (child != null)
		{
			InitializeIfRequired();
			string key = _dictionary.FindValue(child).Value.Key;
			if (key.IndexOfAny(ReadStack.SpecialCharacters) != -1)
			{
				path.Add("['" + key + "']");
			}
			else
			{
				path.Add("." + key);
			}
		}
		if (base.Parent != null)
		{
			base.Parent.GetPath(path, this);
		}
	}

	internal void SetItem(string propertyName, JsonNode value)
	{
		InitializeIfRequired();
		JsonNode item = _dictionary.SetValue(propertyName, value, delegate
		{
			value?.AssignParent(this);
		});
		DetachParent(item);
	}

	private void DetachParent(JsonNode item)
	{
		InitializeIfRequired();
		if (item != null)
		{
			item.Parent = null;
		}
	}

	public void Add(string propertyName, JsonNode? value)
	{
		InitializeIfRequired();
		_dictionary.Add(propertyName, value);
		value?.AssignParent(this);
	}

	public void Add(KeyValuePair<string, JsonNode?> property)
	{
		Add(property.Key, property.Value);
	}

	public void Clear()
	{
		if (_jsonElement.HasValue)
		{
			_jsonElement = null;
		}
		else
		{
			if (_dictionary == null)
			{
				return;
			}
			foreach (JsonNode item in _dictionary.GetValueCollection())
			{
				DetachParent(item);
			}
			_dictionary.Clear();
		}
	}

	public bool ContainsKey(string propertyName)
	{
		InitializeIfRequired();
		return _dictionary.ContainsKey(propertyName);
	}

	public bool Remove(string propertyName)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		InitializeIfRequired();
		JsonNode existing;
		bool flag = _dictionary.TryRemoveProperty(propertyName, out existing);
		if (flag)
		{
			DetachParent(existing);
		}
		return flag;
	}

	bool ICollection<KeyValuePair<string, JsonNode>>.Contains(KeyValuePair<string, JsonNode> item)
	{
		InitializeIfRequired();
		return _dictionary.Contains(item);
	}

	void ICollection<KeyValuePair<string, JsonNode>>.CopyTo(KeyValuePair<string, JsonNode>[] array, int index)
	{
		InitializeIfRequired();
		_dictionary.CopyTo(array, index);
	}

	public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator()
	{
		InitializeIfRequired();
		return _dictionary.GetEnumerator();
	}

	bool ICollection<KeyValuePair<string, JsonNode>>.Remove(KeyValuePair<string, JsonNode> item)
	{
		return Remove(item.Key);
	}

	bool IDictionary<string, JsonNode>.TryGetValue(string propertyName, out JsonNode jsonNode)
	{
		InitializeIfRequired();
		return _dictionary.TryGetValue(propertyName, out jsonNode);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		InitializeIfRequired();
		return _dictionary.GetEnumerator();
	}

	private void InitializeIfRequired()
	{
		if (_dictionary != null)
		{
			return;
		}
		bool caseInsensitive = base.Options.HasValue && base.Options.Value.PropertyNameCaseInsensitive;
		JsonPropertyDictionary<JsonNode> jsonPropertyDictionary = new JsonPropertyDictionary<JsonNode>(caseInsensitive);
		if (_jsonElement.HasValue)
		{
			foreach (JsonProperty item in _jsonElement.Value.EnumerateObject())
			{
				JsonNode jsonNode = JsonNodeConverter.Create(item.Value, base.Options);
				if (jsonNode != null)
				{
					jsonNode.Parent = this;
				}
				jsonPropertyDictionary.Add(new KeyValuePair<string, JsonNode>(item.Name, jsonNode));
			}
			_jsonElement = null;
		}
		_dictionary = jsonPropertyDictionary;
	}
}
