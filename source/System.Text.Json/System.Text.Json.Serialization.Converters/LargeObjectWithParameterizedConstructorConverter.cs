using System.Buffers;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal class LargeObjectWithParameterizedConstructorConverter<T> : ObjectWithParameterizedConstructorConverter<T>
{
	protected sealed override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo)
	{
		object value;
		bool flag = jsonParameterInfo.ConverterBase.TryReadAsObject(ref reader, jsonParameterInfo.Options, ref state, out value);
		if (flag && (value != null || !jsonParameterInfo.IgnoreDefaultValuesOnRead))
		{
			((object[])state.Current.CtorArgumentState.Arguments)[jsonParameterInfo.ClrInfo.Position] = value;
		}
		return flag;
	}

	protected sealed override object CreateObject(ref ReadStackFrame frame)
	{
		object[] array = (object[])frame.CtorArgumentState.Arguments;
		frame.CtorArgumentState.Arguments = null;
		Func<object[], T> func = (Func<object[], T>)frame.JsonTypeInfo.CreateObjectWithArgs;
		if (func == null)
		{
			ThrowHelper.ThrowNotSupportedException_ConstructorMaxOf64Parameters(TypeToConvert);
		}
		object result = func(array);
		ArrayPool<object>.Shared.Return(array, clearArray: true);
		return result;
	}

	protected sealed override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (jsonTypeInfo.ParameterCache == null)
		{
			jsonTypeInfo.InitializePropCache();
		}
		List<KeyValuePair<string, JsonParameterInfo>> list = jsonTypeInfo.ParameterCache.List;
		object[] array = ArrayPool<object>.Shared.Rent(list.Count);
		for (int i = 0; i < jsonTypeInfo.ParameterCount; i++)
		{
			JsonParameterInfo value = list[i].Value;
			array[value.ClrInfo.Position] = value.DefaultValue;
		}
		state.Current.CtorArgumentState.Arguments = array;
	}
}
