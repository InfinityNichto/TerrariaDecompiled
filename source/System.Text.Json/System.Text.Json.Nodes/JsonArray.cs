using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Nodes;

[DebuggerDisplay("JsonArray[{List.Count}]")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class JsonArray : JsonNode, IList<JsonNode?>, ICollection<JsonNode?>, IEnumerable<JsonNode?>, IEnumerable
{
	[ExcludeFromCodeCoverage]
	private class DebugView
	{
		[DebuggerDisplay("{Display,nq}")]
		private struct DebugViewItem
		{
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonNode Value;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public string Display
			{
				get
				{
					if (Value == null)
					{
						return "null";
					}
					if (Value is JsonValue)
					{
						return Value.ToJsonString();
					}
					if (Value is JsonObject jsonObject)
					{
						return $"JsonObject[{jsonObject.Count}]";
					}
					JsonArray jsonArray = (JsonArray)Value;
					return $"JsonArray[{jsonArray.List.Count}]";
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private JsonArray _node;

		public string Json => _node.ToJsonString();

		public string Path => _node.GetPath();

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private DebugViewItem[] Items
		{
			get
			{
				DebugViewItem[] array = new DebugViewItem[_node.List.Count];
				for (int i = 0; i < _node.List.Count; i++)
				{
					array[i].Value = _node.List[i];
				}
				return array;
			}
		}

		public DebugView(JsonArray node)
		{
			_node = node;
		}
	}

	private JsonElement? _jsonElement;

	private List<JsonNode> _list;

	internal List<JsonNode?> List
	{
		get
		{
			CreateNodes();
			return _list;
		}
	}

	public int Count => List.Count;

	bool ICollection<JsonNode>.IsReadOnly => false;

	public JsonArray(JsonNodeOptions? options = null)
		: base(options)
	{
	}

	public JsonArray(JsonNodeOptions options, params JsonNode?[] items)
		: base(options)
	{
		InitializeFromArray(items);
	}

	public JsonArray(params JsonNode?[] items)
	{
		InitializeFromArray(items);
	}

	private void InitializeFromArray(JsonNode[] items)
	{
		List<JsonNode> list = new List<JsonNode>(items);
		for (int i = 0; i < items.Length; i++)
		{
			items[i]?.AssignParent(this);
		}
		_list = list;
	}

	public static JsonArray? Create(JsonElement element, JsonNodeOptions? options = null)
	{
		if (element.ValueKind == JsonValueKind.Null)
		{
			return null;
		}
		if (element.ValueKind == JsonValueKind.Array)
		{
			return new JsonArray(element, options);
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeElementWrongType, "Array"));
	}

	internal JsonArray(JsonElement element, JsonNodeOptions? options = null)
		: base(options)
	{
		_jsonElement = element;
	}

	[RequiresUnreferencedCode("Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.")]
	public void Add<T>(T? value)
	{
		if (value == null)
		{
			Add(null);
			return;
		}
		JsonNode jsonNode = value as JsonNode;
		if (jsonNode == null)
		{
			jsonNode = new JsonValueNotTrimmable<T>(value);
		}
		Add(jsonNode);
	}

	internal JsonNode GetItem(int index)
	{
		return List[index];
	}

	internal void SetItem(int index, JsonNode value)
	{
		value?.AssignParent(this);
		DetachParent(List[index]);
		List[index] = value;
	}

	internal override void GetPath(List<string> path, JsonNode child)
	{
		if (child != null)
		{
			int value = List.IndexOf(child);
			path.Add($"[{value}]");
		}
		base.Parent?.GetPath(path, this);
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
		CreateNodes();
		if (options == null)
		{
			options = JsonSerializerOptions.s_defaultOptions;
		}
		writer.WriteStartArray();
		for (int i = 0; i < _list.Count; i++)
		{
			JsonNodeConverter.Instance.Write(writer, _list[i], options);
		}
		writer.WriteEndArray();
	}

	private void CreateNodes()
	{
		if (_list != null)
		{
			return;
		}
		List<JsonNode> list;
		if (!_jsonElement.HasValue)
		{
			list = new List<JsonNode>();
		}
		else
		{
			JsonElement value = _jsonElement.Value;
			list = new List<JsonNode>(value.GetArrayLength());
			foreach (JsonElement item in value.EnumerateArray())
			{
				JsonNode jsonNode = JsonNodeConverter.Create(item, base.Options);
				jsonNode?.AssignParent(this);
				list.Add(jsonNode);
			}
			_jsonElement = null;
		}
		_list = list;
	}

	public void Add(JsonNode? item)
	{
		item?.AssignParent(this);
		List.Add(item);
	}

	public void Clear()
	{
		for (int i = 0; i < List.Count; i++)
		{
			DetachParent(List[i]);
		}
		List.Clear();
	}

	public bool Contains(JsonNode? item)
	{
		return List.Contains(item);
	}

	public int IndexOf(JsonNode? item)
	{
		return List.IndexOf(item);
	}

	public void Insert(int index, JsonNode? item)
	{
		item?.AssignParent(this);
		List.Insert(index, item);
	}

	public bool Remove(JsonNode? item)
	{
		if (List.Remove(item))
		{
			DetachParent(item);
			return true;
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		JsonNode item = List[index];
		List.RemoveAt(index);
		DetachParent(item);
	}

	void ICollection<JsonNode>.CopyTo(JsonNode[] array, int index)
	{
		List.CopyTo(array, index);
	}

	public IEnumerator<JsonNode?> GetEnumerator()
	{
		return List.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)List).GetEnumerator();
	}

	private void DetachParent(JsonNode item)
	{
		if (item != null)
		{
			item.Parent = null;
		}
	}
}
