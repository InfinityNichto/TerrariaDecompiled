using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

[DebuggerDisplay("ConverterStrategy.{JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {JsonTypeInfo.Type.Name}")]
internal struct ReadStackFrame
{
	public JsonPropertyInfo JsonPropertyInfo;

	public StackFramePropertyState PropertyState;

	public bool UseExtensionProperty;

	public byte[] JsonPropertyName;

	public string JsonPropertyNameAsString;

	public object DictionaryKey;

	public int OriginalDepth;

	public JsonTokenType OriginalTokenType;

	public object ReturnValue;

	public JsonTypeInfo JsonTypeInfo;

	public StackFrameObjectState ObjectState;

	public bool ValidateEndTokenOnArray;

	public int PropertyIndex;

	public List<PropertyRef> PropertyRefCache;

	public int CtorArgumentStateIndex;

	public ArgumentState CtorArgumentState;

	public JsonNumberHandling? NumberHandling;

	public void EndConstructorParameter()
	{
		CtorArgumentState.JsonParameterInfo = null;
		JsonPropertyName = null;
		PropertyState = StackFramePropertyState.None;
	}

	public void EndProperty()
	{
		JsonPropertyInfo = null;
		JsonPropertyName = null;
		JsonPropertyNameAsString = null;
		PropertyState = StackFramePropertyState.None;
		ValidateEndTokenOnArray = false;
	}

	public void EndElement()
	{
		JsonPropertyNameAsString = null;
		PropertyState = StackFramePropertyState.None;
	}

	public bool IsProcessingDictionary()
	{
		return (JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy & ConverterStrategy.Dictionary) != 0;
	}

	public bool IsProcessingEnumerable()
	{
		return (JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy & ConverterStrategy.Enumerable) != 0;
	}
}
