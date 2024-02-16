using System.Diagnostics;

namespace System.Text.Json;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct JsonProperty
{
	public JsonElement Value { get; }

	private string? _name { get; }

	public string Name => _name ?? Value.GetPropertyName();

	private string DebuggerDisplay
	{
		get
		{
			if (Value.ValueKind != 0)
			{
				return "\"" + ToString() + "\"";
			}
			return "<Undefined>";
		}
	}

	internal JsonProperty(JsonElement value, string name = null)
	{
		Value = value;
		_name = name;
	}

	public bool NameEquals(string? text)
	{
		return NameEquals(text.AsSpan());
	}

	public bool NameEquals(ReadOnlySpan<byte> utf8Text)
	{
		return Value.TextEqualsHelper(utf8Text, isPropertyName: true, shouldUnescape: true);
	}

	public bool NameEquals(ReadOnlySpan<char> text)
	{
		return Value.TextEqualsHelper(text, isPropertyName: true);
	}

	internal bool EscapedNameEquals(ReadOnlySpan<byte> utf8Text)
	{
		return Value.TextEqualsHelper(utf8Text, isPropertyName: true, shouldUnescape: false);
	}

	public void WriteTo(Utf8JsonWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.WritePropertyName(Name);
		Value.WriteTo(writer);
	}

	public override string ToString()
	{
		return Value.GetPropertyRawText();
	}
}
