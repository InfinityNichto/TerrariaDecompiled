using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal class SmallObjectWithParameterizedConstructorConverter<T, TArg0, TArg1, TArg2, TArg3> : ObjectWithParameterizedConstructorConverter<T>
{
	protected override object CreateObject(ref ReadStackFrame frame)
	{
		JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3> parameterizedConstructorDelegate = (JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>)frame.JsonTypeInfo.CreateObjectWithArgs;
		Arguments<TArg0, TArg1, TArg2, TArg3> arguments = (Arguments<TArg0, TArg1, TArg2, TArg3>)frame.CtorArgumentState.Arguments;
		return parameterizedConstructorDelegate(arguments.Arg0, arguments.Arg1, arguments.Arg2, arguments.Arg3);
	}

	protected override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo)
	{
		Arguments<TArg0, TArg1, TArg2, TArg3> arguments = (Arguments<TArg0, TArg1, TArg2, TArg3>)state.Current.CtorArgumentState.Arguments;
		return jsonParameterInfo.ClrInfo.Position switch
		{
			0 => TryRead<TArg0>(ref state, ref reader, jsonParameterInfo, out arguments.Arg0), 
			1 => TryRead<TArg1>(ref state, ref reader, jsonParameterInfo, out arguments.Arg1), 
			2 => TryRead<TArg2>(ref state, ref reader, jsonParameterInfo, out arguments.Arg2), 
			3 => TryRead<TArg3>(ref state, ref reader, jsonParameterInfo, out arguments.Arg3), 
			_ => throw new InvalidOperationException(), 
		};
	}

	private bool TryRead<TArg>(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, out TArg arg)
	{
		JsonParameterInfo<TArg> jsonParameterInfo2 = (JsonParameterInfo<TArg>)jsonParameterInfo;
		JsonConverter<TArg> jsonConverter = (JsonConverter<TArg>)jsonParameterInfo.ConverterBase;
		TArg value;
		bool result = jsonConverter.TryRead(ref reader, jsonParameterInfo2.RuntimePropertyType, jsonParameterInfo2.Options, ref state, out value);
		arg = ((value == null && jsonParameterInfo.IgnoreDefaultValuesOnRead) ? ((TArg)jsonParameterInfo2.DefaultValue) : value);
		return result;
	}

	protected override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (jsonTypeInfo.CreateObjectWithArgs == null)
		{
			jsonTypeInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(base.ConstructorInfo);
		}
		Arguments<TArg0, TArg1, TArg2, TArg3> arguments = new Arguments<TArg0, TArg1, TArg2, TArg3>();
		List<KeyValuePair<string, JsonParameterInfo>> list = jsonTypeInfo.ParameterCache.List;
		for (int i = 0; i < jsonTypeInfo.ParameterCount; i++)
		{
			JsonParameterInfo value = list[i].Value;
			if (value.ShouldDeserialize)
			{
				switch (value.ClrInfo.Position)
				{
				case 0:
					arguments.Arg0 = ((JsonParameterInfo<TArg0>)value).TypedDefaultValue;
					break;
				case 1:
					arguments.Arg1 = ((JsonParameterInfo<TArg1>)value).TypedDefaultValue;
					break;
				case 2:
					arguments.Arg2 = ((JsonParameterInfo<TArg2>)value).TypedDefaultValue;
					break;
				case 3:
					arguments.Arg3 = ((JsonParameterInfo<TArg3>)value).TypedDefaultValue;
					break;
				default:
					throw new InvalidOperationException();
				}
			}
		}
		state.Current.CtorArgumentState.Arguments = arguments;
	}
}
