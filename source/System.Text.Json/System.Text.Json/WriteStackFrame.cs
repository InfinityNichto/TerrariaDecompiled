using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

[DebuggerDisplay("ConverterStrategy.{JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {JsonTypeInfo.Type.Name}")]
internal struct WriteStackFrame
{
	public IEnumerator CollectionEnumerator;

	public IAsyncDisposable AsyncDisposable;

	public bool AsyncEnumeratorIsPendingCompletion;

	public JsonPropertyInfo DeclaredJsonPropertyInfo;

	public bool IsWritingExtensionDataProperty;

	public JsonTypeInfo JsonTypeInfo;

	public int OriginalDepth;

	public bool ProcessedStartToken;

	public bool ProcessedEndToken;

	public StackFramePropertyState PropertyState;

	public int EnumeratorIndex;

	public string JsonPropertyNameAsString;

	public MetadataPropertyName MetadataPropertyName;

	private JsonPropertyInfo PolymorphicJsonPropertyInfo;

	public JsonNumberHandling? NumberHandling;

	public void EndDictionaryElement()
	{
		PropertyState = StackFramePropertyState.None;
	}

	public void EndProperty()
	{
		DeclaredJsonPropertyInfo = null;
		JsonPropertyNameAsString = null;
		PolymorphicJsonPropertyInfo = null;
		PropertyState = StackFramePropertyState.None;
	}

	public JsonPropertyInfo GetPolymorphicJsonPropertyInfo()
	{
		return PolymorphicJsonPropertyInfo ?? DeclaredJsonPropertyInfo;
	}

	public JsonConverter InitializeReEntry(Type type, JsonSerializerOptions options)
	{
		if (PolymorphicJsonPropertyInfo?.RuntimePropertyType != type)
		{
			JsonTypeInfo orAddClass = options.GetOrAddClass(type);
			PolymorphicJsonPropertyInfo = orAddClass.PropertyInfoForTypeInfo;
		}
		return PolymorphicJsonPropertyInfo.ConverterBase;
	}
}
