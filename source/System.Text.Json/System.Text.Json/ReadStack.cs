using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

[DebuggerDisplay("Path:{JsonPath()} Current: ConverterStrategy.{Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {Current.JsonTypeInfo.Type.Name}")]
internal struct ReadStack
{
	internal static readonly char[] SpecialCharacters = new char[18]
	{
		'.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t',
		'\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029'
	};

	public ReadStackFrame Current;

	private ReadStackFrame[] _stack;

	private int _count;

	private int _continuationCount;

	private List<ArgumentState> _ctorArgStateCache;

	public long BytesConsumed;

	public bool ReadAhead;

	public ReferenceResolver ReferenceResolver;

	public bool SupportContinuation;

	public bool UseFastPath;

	public bool IsContinuation => _continuationCount != 0;

	private void EnsurePushCapacity()
	{
		if (_stack == null)
		{
			_stack = new ReadStackFrame[4];
		}
		else if (_count - 1 == _stack.Length)
		{
			Array.Resize(ref _stack, 2 * _stack.Length);
		}
	}

	public void Initialize(Type type, JsonSerializerOptions options, bool supportContinuation)
	{
		JsonTypeInfo orAddClassForRootType = options.GetOrAddClassForRootType(type);
		Initialize(orAddClassForRootType, supportContinuation);
	}

	internal void Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation = false)
	{
		Current.JsonTypeInfo = jsonTypeInfo;
		Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
		Current.NumberHandling = Current.JsonPropertyInfo.NumberHandling;
		JsonSerializerOptions options = jsonTypeInfo.Options;
		bool flag = options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve;
		if (flag)
		{
			ReferenceResolver = options.ReferenceHandler.CreateResolver(writing: false);
		}
		SupportContinuation = supportContinuation;
		UseFastPath = !supportContinuation && !flag;
	}

	public void Push()
	{
		if (_continuationCount == 0)
		{
			if (_count == 0)
			{
				_count = 1;
			}
			else
			{
				JsonNumberHandling? numberHandling = Current.NumberHandling;
				JsonTypeInfo jsonTypeInfo = Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy switch
				{
					ConverterStrategy.Object => (Current.JsonPropertyInfo == null) ? Current.CtorArgumentState.JsonParameterInfo.RuntimeTypeInfo : Current.JsonPropertyInfo.RuntimeTypeInfo, 
					ConverterStrategy.Value => Current.JsonPropertyInfo.RuntimeTypeInfo, 
					_ => Current.JsonTypeInfo.ElementTypeInfo, 
				};
				EnsurePushCapacity();
				_stack[_count - 1] = Current;
				Current = default(ReadStackFrame);
				_count++;
				Current.JsonTypeInfo = jsonTypeInfo;
				Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
				Current.NumberHandling = numberHandling ?? Current.JsonPropertyInfo.NumberHandling;
			}
		}
		else
		{
			if (_count++ > 0)
			{
				Current = _stack[_count - 1];
			}
			if (_continuationCount == _count)
			{
				_continuationCount = 0;
			}
		}
		SetConstructorArgumentState();
	}

	public void Pop(bool success)
	{
		if (!success)
		{
			if (_continuationCount == 0)
			{
				if (_count == 1)
				{
					_continuationCount = 1;
					_count = 0;
					return;
				}
				EnsurePushCapacity();
				_continuationCount = _count--;
			}
			else if (--_count == 0)
			{
				return;
			}
			_stack[_count] = Current;
			Current = _stack[_count - 1];
		}
		else if (--_count > 0)
		{
			Current = _stack[_count - 1];
		}
		SetConstructorArgumentState();
	}

	public string JsonPath()
	{
		StringBuilder stringBuilder = new StringBuilder("$");
		int num = Math.Max(_count, _continuationCount);
		for (int i = 0; i < num - 1; i++)
		{
			AppendStackFrame(stringBuilder, ref _stack[i]);
		}
		if (_continuationCount == 0)
		{
			AppendStackFrame(stringBuilder, ref Current);
		}
		return stringBuilder.ToString();
		static void AppendPropertyName(StringBuilder sb, string propertyName)
		{
			if (propertyName != null)
			{
				if (propertyName.IndexOfAny(SpecialCharacters) != -1)
				{
					sb.Append("['");
					sb.Append(propertyName);
					sb.Append("']");
				}
				else
				{
					sb.Append('.');
					sb.Append(propertyName);
				}
			}
		}
		static void AppendStackFrame(StringBuilder sb, ref ReadStackFrame frame)
		{
			string propertyName2 = GetPropertyName(ref frame);
			AppendPropertyName(sb, propertyName2);
			if (frame.JsonTypeInfo != null && frame.IsProcessingEnumerable() && frame.ReturnValue is IEnumerable enumerable2 && (frame.ObjectState == StackFrameObjectState.None || frame.ObjectState == StackFrameObjectState.CreatedObject || frame.ObjectState == StackFrameObjectState.ReadElements))
			{
				sb.Append('[');
				sb.Append(GetCount(enumerable2));
				sb.Append(']');
			}
		}
		static int GetCount(IEnumerable enumerable)
		{
			if (enumerable is ICollection collection)
			{
				return collection.Count;
			}
			int num2 = 0;
			IEnumerator enumerator = enumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				num2++;
			}
			return num2;
		}
		static string GetPropertyName(ref ReadStackFrame frame)
		{
			string result = null;
			byte[] array = frame.JsonPropertyName;
			if (array == null)
			{
				if (frame.JsonPropertyNameAsString != null)
				{
					result = frame.JsonPropertyNameAsString;
				}
				else
				{
					array = frame.JsonPropertyInfo?.NameAsUtf8Bytes ?? frame.CtorArgumentState?.JsonParameterInfo?.NameAsUtf8Bytes;
				}
			}
			if (array != null)
			{
				result = JsonHelpers.Utf8GetString(array);
			}
			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetConstructorArgumentState()
	{
		if (!Current.JsonTypeInfo.IsObjectWithParameterizedCtor)
		{
			return;
		}
		if (Current.CtorArgumentStateIndex == 0)
		{
			if (_ctorArgStateCache == null)
			{
				_ctorArgStateCache = new List<ArgumentState>();
			}
			ArgumentState argumentState = new ArgumentState();
			_ctorArgStateCache.Add(argumentState);
			ref int ctorArgumentStateIndex = ref Current.CtorArgumentStateIndex;
			ref ArgumentState ctorArgumentState = ref Current.CtorArgumentState;
			int count = _ctorArgStateCache.Count;
			ArgumentState argumentState2 = argumentState;
			ctorArgumentStateIndex = count;
			ctorArgumentState = argumentState2;
		}
		else
		{
			Current.CtorArgumentState = _ctorArgStateCache[Current.CtorArgumentStateIndex - 1];
		}
	}
}
