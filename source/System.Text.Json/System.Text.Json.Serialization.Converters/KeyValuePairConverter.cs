using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class KeyValuePairConverter<TKey, TValue> : SmallObjectWithParameterizedConstructorConverter<KeyValuePair<TKey, TValue>, TKey, TValue, object, object>
{
	private string _keyName;

	private string _valueName;

	private static readonly ConstructorInfo s_constructorInfo = typeof(KeyValuePair<TKey, TValue>).GetConstructor(new Type[2]
	{
		typeof(TKey),
		typeof(TValue)
	});

	internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo jsonTypeInfo = null)
	{
		JsonNamingPolicy propertyNamingPolicy = options.PropertyNamingPolicy;
		if (propertyNamingPolicy == null)
		{
			_keyName = "Key";
			_valueName = "Value";
		}
		else
		{
			_keyName = propertyNamingPolicy.ConvertName("Key");
			_valueName = propertyNamingPolicy.ConvertName("Value");
		}
		base.ConstructorInfo = s_constructorInfo;
	}

	protected override bool TryLookupConstructorParameter(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options, out JsonParameterInfo jsonParameterInfo)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		ArgumentState ctorArgumentState = state.Current.CtorArgumentState;
		bool propertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
		string @string = reader.GetString();
		state.Current.JsonPropertyNameAsString = @string;
		if (!ctorArgumentState.FoundKey && FoundKeyProperty(@string, propertyNameCaseInsensitive))
		{
			jsonParameterInfo = jsonTypeInfo.ParameterCache[_keyName];
			ctorArgumentState.FoundKey = true;
		}
		else
		{
			if (ctorArgumentState.FoundValue || !FoundValueProperty(@string, propertyNameCaseInsensitive))
			{
				ThrowHelper.ThrowJsonException();
				jsonParameterInfo = null;
				return false;
			}
			jsonParameterInfo = jsonTypeInfo.ParameterCache[_valueName];
			ctorArgumentState.FoundValue = true;
		}
		ctorArgumentState.ParameterIndex++;
		ctorArgumentState.JsonParameterInfo = jsonParameterInfo;
		state.Current.NumberHandling = jsonParameterInfo.NumberHandling;
		return true;
	}

	protected override void EndRead(ref ReadStack state)
	{
		if (state.Current.CtorArgumentState.ParameterIndex != 2)
		{
			ThrowHelper.ThrowJsonException();
		}
	}

	private bool FoundKeyProperty(string propertyName, bool caseInsensitiveMatch)
	{
		if (!(propertyName == _keyName) && (!caseInsensitiveMatch || !string.Equals(propertyName, _keyName, StringComparison.OrdinalIgnoreCase)))
		{
			return propertyName == "Key";
		}
		return true;
	}

	private bool FoundValueProperty(string propertyName, bool caseInsensitiveMatch)
	{
		if (!(propertyName == _valueName) && (!caseInsensitiveMatch || !string.Equals(propertyName, _valueName, StringComparison.OrdinalIgnoreCase)))
		{
			return propertyName == "Value";
		}
		return true;
	}
}
