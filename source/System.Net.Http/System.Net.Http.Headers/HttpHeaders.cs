using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Http.Headers;

public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>, IEnumerable
{
	internal sealed class HeaderStoreItemInfo
	{
		internal object RawValue;

		internal object InvalidValue;

		internal object ParsedValue;

		internal bool IsEmpty
		{
			get
			{
				if (RawValue == null && InvalidValue == null)
				{
					return ParsedValue == null;
				}
				return false;
			}
		}

		internal HeaderStoreItemInfo()
		{
		}

		internal bool CanAddParsedValue(HttpHeaderParser parser)
		{
			if (!parser.SupportsMultipleValues)
			{
				if (InvalidValue == null)
				{
					return ParsedValue == null;
				}
				return false;
			}
			return true;
		}
	}

	private Dictionary<HeaderDescriptor, object> _headerStore;

	private readonly HttpHeaderType _allowedHeaderTypes;

	private readonly HttpHeaderType _treatAsCustomHeaderTypes;

	internal Dictionary<HeaderDescriptor, object>? HeaderStore => _headerStore;

	public HttpHeadersNonValidated NonValidated => new HttpHeadersNonValidated(this);

	protected HttpHeaders()
		: this(HttpHeaderType.All, HttpHeaderType.None)
	{
	}

	internal HttpHeaders(HttpHeaderType allowedHeaderTypes, HttpHeaderType treatAsCustomHeaderTypes)
	{
		_allowedHeaderTypes = allowedHeaderTypes & ~HttpHeaderType.NonTrailing;
		_treatAsCustomHeaderTypes = treatAsCustomHeaderTypes & ~HttpHeaderType.NonTrailing;
	}

	public void Add(string name, string? value)
	{
		Add(GetHeaderDescriptor(name), value);
	}

	internal void Add(HeaderDescriptor descriptor, string value)
	{
		PrepareHeaderInfoForAdd(descriptor, out var info, out var addToStore);
		ParseAndAddValue(descriptor, info, value);
		if (addToStore && info.ParsedValue != null)
		{
			AddHeaderToStore(descriptor, info);
		}
	}

	public void Add(string name, IEnumerable<string?> values)
	{
		Add(GetHeaderDescriptor(name), values);
	}

	internal void Add(HeaderDescriptor descriptor, IEnumerable<string> values)
	{
		if (values == null)
		{
			throw new ArgumentNullException("values");
		}
		PrepareHeaderInfoForAdd(descriptor, out var info, out var addToStore);
		try
		{
			foreach (string value in values)
			{
				ParseAndAddValue(descriptor, info, value);
			}
		}
		finally
		{
			if (addToStore && info.ParsedValue != null)
			{
				AddHeaderToStore(descriptor, info);
			}
		}
	}

	public bool TryAddWithoutValidation(string name, string? value)
	{
		if (TryGetHeaderDescriptor(name, out var descriptor))
		{
			return TryAddWithoutValidation(descriptor, value);
		}
		return false;
	}

	internal bool TryAddWithoutValidation(HeaderDescriptor descriptor, string value)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		if (_headerStore == null)
		{
			_headerStore = new Dictionary<HeaderDescriptor, object>();
		}
		if (_headerStore.TryGetValue(descriptor, out var value2))
		{
			if (value2 is HeaderStoreItemInfo info)
			{
				AddRawValue(info, value);
			}
			else
			{
				Dictionary<HeaderDescriptor, object> headerStore = _headerStore;
				HeaderStoreItemInfo obj = new HeaderStoreItemInfo
				{
					RawValue = value2
				};
				HeaderStoreItemInfo info2 = obj;
				headerStore[descriptor] = obj;
				AddRawValue(info2, value);
			}
		}
		else
		{
			_headerStore.Add(descriptor, value);
		}
		return true;
	}

	public bool TryAddWithoutValidation(string name, IEnumerable<string?> values)
	{
		if (TryGetHeaderDescriptor(name, out var descriptor))
		{
			return TryAddWithoutValidation(descriptor, values);
		}
		return false;
	}

	internal bool TryAddWithoutValidation(HeaderDescriptor descriptor, IEnumerable<string> values)
	{
		if (values == null)
		{
			throw new ArgumentNullException("values");
		}
		using (IEnumerator<string> enumerator = values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				TryAddWithoutValidation(descriptor, enumerator.Current);
				if (enumerator.MoveNext())
				{
					HeaderStoreItemInfo orCreateHeaderInfo = GetOrCreateHeaderInfo(descriptor, parseRawValues: false);
					do
					{
						AddRawValue(orCreateHeaderInfo, enumerator.Current ?? string.Empty);
					}
					while (enumerator.MoveNext());
				}
			}
		}
		return true;
	}

	public void Clear()
	{
		_headerStore?.Clear();
	}

	public IEnumerable<string> GetValues(string name)
	{
		return GetValues(GetHeaderDescriptor(name));
	}

	internal IEnumerable<string> GetValues(HeaderDescriptor descriptor)
	{
		if (TryGetValues(descriptor, out var values))
		{
			return values;
		}
		throw new InvalidOperationException(System.SR.net_http_headers_not_found);
	}

	public bool TryGetValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
	{
		if (TryGetHeaderDescriptor(name, out var descriptor))
		{
			return TryGetValues(descriptor, out values);
		}
		values = null;
		return false;
	}

	internal bool TryGetValues(HeaderDescriptor descriptor, [NotNullWhen(true)] out IEnumerable<string> values)
	{
		if (_headerStore != null && TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			values = GetStoreValuesAsStringArray(descriptor, info);
			return true;
		}
		values = null;
		return false;
	}

	public bool Contains(string name)
	{
		return Contains(GetHeaderDescriptor(name));
	}

	internal bool Contains(HeaderDescriptor descriptor)
	{
		HeaderStoreItemInfo info;
		if (_headerStore != null)
		{
			return TryGetAndParseHeaderInfo(descriptor, out info);
		}
		return false;
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[512];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		Dictionary<HeaderDescriptor, object> headerStore = _headerStore;
		if (headerStore != null)
		{
			foreach (KeyValuePair<HeaderDescriptor, object> item in headerStore)
			{
				valueStringBuilder.Append(item.Key.Name);
				valueStringBuilder.Append(": ");
				GetStoreValuesAsStringOrStringArray(item.Key, item.Value, out var singleValue, out var multiValue);
				if (singleValue != null)
				{
					valueStringBuilder.Append(singleValue);
				}
				else
				{
					HttpHeaderParser parser = item.Key.Parser;
					string s = ((parser != null && parser.SupportsMultipleValues) ? parser.Separator : ", ");
					for (int i = 0; i < multiValue.Length; i++)
					{
						if (i != 0)
						{
							valueStringBuilder.Append(s);
						}
						valueStringBuilder.Append(multiValue[i]);
					}
				}
				valueStringBuilder.Append(Environment.NewLine);
			}
		}
		return valueStringBuilder.ToString();
	}

	internal string GetHeaderString(HeaderDescriptor descriptor)
	{
		if (TryGetHeaderValue(descriptor, out var value))
		{
			GetStoreValuesAsStringOrStringArray(descriptor, value, out var singleValue, out var multiValue);
			if (singleValue != null)
			{
				return singleValue;
			}
			string separator = ((descriptor.Parser != null && descriptor.Parser.SupportsMultipleValues) ? descriptor.Parser.Separator : ", ");
			return string.Join(separator, multiValue);
		}
		return string.Empty;
	}

	public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
	{
		if (_headerStore == null || _headerStore.Count <= 0)
		{
			return ((IEnumerable<KeyValuePair<string, IEnumerable<string>>>)Array.Empty<KeyValuePair<string, IEnumerable<string>>>()).GetEnumerator();
		}
		return GetEnumeratorCore();
	}

	private IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumeratorCore()
	{
		foreach (KeyValuePair<HeaderDescriptor, object> item in _headerStore)
		{
			HeaderDescriptor key = item.Key;
			object value = item.Value;
			HeaderStoreItemInfo headerStoreItemInfo = value as HeaderStoreItemInfo;
			if (headerStoreItemInfo == null)
			{
				Dictionary<HeaderDescriptor, object> headerStore = _headerStore;
				HeaderDescriptor key2 = key;
				HeaderStoreItemInfo obj = new HeaderStoreItemInfo
				{
					RawValue = value
				};
				headerStoreItemInfo = obj;
				headerStore[key2] = obj;
			}
			if (!ParseRawHeaderValues(key, headerStoreItemInfo, removeEmptyHeader: false))
			{
				_headerStore.Remove(key);
				continue;
			}
			string[] storeValuesAsStringArray = GetStoreValuesAsStringArray(key, headerStoreItemInfo);
			yield return new KeyValuePair<string, IEnumerable<string>>(key.Name, storeValuesAsStringArray);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	internal void AddParsedValue(HeaderDescriptor descriptor, object value)
	{
		HeaderStoreItemInfo orCreateHeaderInfo = GetOrCreateHeaderInfo(descriptor, parseRawValues: true);
		AddParsedValue(orCreateHeaderInfo, value);
	}

	internal void SetParsedValue(HeaderDescriptor descriptor, object value)
	{
		HeaderStoreItemInfo orCreateHeaderInfo = GetOrCreateHeaderInfo(descriptor, parseRawValues: true);
		orCreateHeaderInfo.InvalidValue = null;
		orCreateHeaderInfo.ParsedValue = null;
		orCreateHeaderInfo.RawValue = null;
		AddParsedValue(orCreateHeaderInfo, value);
	}

	internal void SetOrRemoveParsedValue(HeaderDescriptor descriptor, object value)
	{
		if (value == null)
		{
			Remove(descriptor);
		}
		else
		{
			SetParsedValue(descriptor, value);
		}
	}

	public bool Remove(string name)
	{
		return Remove(GetHeaderDescriptor(name));
	}

	internal bool Remove(HeaderDescriptor descriptor)
	{
		if (_headerStore != null)
		{
			return _headerStore.Remove(descriptor);
		}
		return false;
	}

	internal bool RemoveParsedValue(HeaderDescriptor descriptor, object value)
	{
		if (_headerStore == null)
		{
			return false;
		}
		if (TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			if (info.ParsedValue == null)
			{
				return false;
			}
			bool result = false;
			IEqualityComparer comparer = descriptor.Parser.Comparer;
			if (!(info.ParsedValue is List<object> list))
			{
				if (AreEqual(value, info.ParsedValue, comparer))
				{
					info.ParsedValue = null;
					result = true;
				}
			}
			else
			{
				foreach (object item in list)
				{
					if (AreEqual(value, item, comparer))
					{
						result = list.Remove(item);
						break;
					}
				}
				if (list.Count == 0)
				{
					info.ParsedValue = null;
				}
			}
			if (info.IsEmpty)
			{
				bool flag = Remove(descriptor);
			}
			return result;
		}
		return false;
	}

	internal bool ContainsParsedValue(HeaderDescriptor descriptor, object value)
	{
		if (_headerStore == null)
		{
			return false;
		}
		if (TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			if (info.ParsedValue == null)
			{
				return false;
			}
			List<object> list = info.ParsedValue as List<object>;
			IEqualityComparer comparer = descriptor.Parser.Comparer;
			if (list == null)
			{
				return AreEqual(value, info.ParsedValue, comparer);
			}
			foreach (object item in list)
			{
				if (AreEqual(value, item, comparer))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	internal virtual void AddHeaders(HttpHeaders sourceHeaders)
	{
		Dictionary<HeaderDescriptor, object> headerStore = sourceHeaders._headerStore;
		if (headerStore == null || headerStore.Count == 0)
		{
			return;
		}
		if (_headerStore == null)
		{
			_headerStore = new Dictionary<HeaderDescriptor, object>();
		}
		foreach (KeyValuePair<HeaderDescriptor, object> item in headerStore)
		{
			if (!_headerStore.ContainsKey(item.Key))
			{
				object value = item.Value;
				if (value is HeaderStoreItemInfo sourceInfo)
				{
					AddHeaderInfo(item.Key, sourceInfo);
				}
				else
				{
					_headerStore.Add(item.Key, value);
				}
			}
		}
	}

	private void AddHeaderInfo(HeaderDescriptor descriptor, HeaderStoreItemInfo sourceInfo)
	{
		HeaderStoreItemInfo headerStoreItemInfo = CreateAndAddHeaderToStore(descriptor);
		headerStoreItemInfo.RawValue = CloneStringHeaderInfoValues(sourceInfo.RawValue);
		if (descriptor.Parser == null)
		{
			headerStoreItemInfo.ParsedValue = CloneStringHeaderInfoValues(sourceInfo.ParsedValue);
			return;
		}
		headerStoreItemInfo.InvalidValue = CloneStringHeaderInfoValues(sourceInfo.InvalidValue);
		if (sourceInfo.ParsedValue == null)
		{
			return;
		}
		if (!(sourceInfo.ParsedValue is List<object> list))
		{
			CloneAndAddValue(headerStoreItemInfo, sourceInfo.ParsedValue);
			return;
		}
		foreach (object item in list)
		{
			CloneAndAddValue(headerStoreItemInfo, item);
		}
	}

	private static void CloneAndAddValue(HeaderStoreItemInfo destinationInfo, object source)
	{
		if (source is ICloneable cloneable)
		{
			AddParsedValue(destinationInfo, cloneable.Clone());
		}
		else
		{
			AddParsedValue(destinationInfo, source);
		}
	}

	[return: NotNullIfNotNull("source")]
	private static object CloneStringHeaderInfoValues(object source)
	{
		if (source == null)
		{
			return null;
		}
		if (!(source is List<object> collection))
		{
			return source;
		}
		return new List<object>(collection);
	}

	private HeaderStoreItemInfo GetOrCreateHeaderInfo(HeaderDescriptor descriptor, bool parseRawValues)
	{
		HeaderStoreItemInfo info = null;
		bool flag;
		if (parseRawValues)
		{
			flag = TryGetAndParseHeaderInfo(descriptor, out info);
		}
		else
		{
			flag = TryGetHeaderValue(descriptor, out var value);
			if (flag)
			{
				if (value is HeaderStoreItemInfo headerStoreItemInfo)
				{
					info = headerStoreItemInfo;
				}
				else
				{
					Dictionary<HeaderDescriptor, object> headerStore = _headerStore;
					HeaderStoreItemInfo obj = new HeaderStoreItemInfo
					{
						RawValue = value
					};
					info = obj;
					headerStore[descriptor] = obj;
				}
			}
		}
		if (!flag)
		{
			info = CreateAndAddHeaderToStore(descriptor);
		}
		return info;
	}

	private HeaderStoreItemInfo CreateAndAddHeaderToStore(HeaderDescriptor descriptor)
	{
		HeaderStoreItemInfo headerStoreItemInfo = new HeaderStoreItemInfo();
		AddHeaderToStore(descriptor, headerStoreItemInfo);
		return headerStoreItemInfo;
	}

	private void AddHeaderToStore(HeaderDescriptor descriptor, object value)
	{
		(_headerStore ?? (_headerStore = new Dictionary<HeaderDescriptor, object>())).Add(descriptor, value);
	}

	internal bool TryGetHeaderValue(HeaderDescriptor descriptor, [NotNullWhen(true)] out object value)
	{
		if (_headerStore == null)
		{
			value = null;
			return false;
		}
		return _headerStore.TryGetValue(descriptor, out value);
	}

	private bool TryGetAndParseHeaderInfo(HeaderDescriptor key, [NotNullWhen(true)] out HeaderStoreItemInfo info)
	{
		if (TryGetHeaderValue(key, out var value))
		{
			if (value is HeaderStoreItemInfo headerStoreItemInfo)
			{
				info = headerStoreItemInfo;
			}
			else
			{
				Dictionary<HeaderDescriptor, object> headerStore = _headerStore;
				HeaderStoreItemInfo obj = new HeaderStoreItemInfo
				{
					RawValue = value
				};
				HeaderStoreItemInfo value2 = obj;
				info = obj;
				headerStore[key] = value2;
			}
			return ParseRawHeaderValues(key, info, removeEmptyHeader: true);
		}
		info = null;
		return false;
	}

	private bool ParseRawHeaderValues(HeaderDescriptor descriptor, HeaderStoreItemInfo info, bool removeEmptyHeader)
	{
		if (info.RawValue != null)
		{
			if (!(info.RawValue is List<string> rawValues))
			{
				ParseSingleRawHeaderValue(descriptor, info);
			}
			else
			{
				ParseMultipleRawHeaderValues(descriptor, info, rawValues);
			}
			info.RawValue = null;
			if (info.InvalidValue == null && info.ParsedValue == null)
			{
				if (removeEmptyHeader)
				{
					_headerStore.Remove(descriptor);
				}
				return false;
			}
		}
		return true;
	}

	private static void ParseMultipleRawHeaderValues(HeaderDescriptor descriptor, HeaderStoreItemInfo info, List<string> rawValues)
	{
		if (descriptor.Parser == null)
		{
			foreach (string rawValue in rawValues)
			{
				if (!ContainsNewLine(rawValue, descriptor.Name))
				{
					AddParsedValue(info, rawValue);
				}
			}
			return;
		}
		foreach (string rawValue2 in rawValues)
		{
			if (!TryParseAndAddRawHeaderValue(descriptor, info, rawValue2, addWhenInvalid: true) && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Log.HeadersInvalidValue(descriptor.Name, rawValue2);
			}
		}
	}

	private static void ParseSingleRawHeaderValue(HeaderDescriptor descriptor, HeaderStoreItemInfo info)
	{
		string text = info.RawValue as string;
		if (descriptor.Parser == null)
		{
			if (!ContainsNewLine(text, descriptor.Name))
			{
				AddParsedValue(info, text);
			}
		}
		else if (!TryParseAndAddRawHeaderValue(descriptor, info, text, addWhenInvalid: true) && System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.HeadersInvalidValue(descriptor.Name, text);
		}
	}

	internal bool TryParseAndAddValue(HeaderDescriptor descriptor, string value)
	{
		PrepareHeaderInfoForAdd(descriptor, out var info, out var addToStore);
		bool flag = TryParseAndAddRawHeaderValue(descriptor, info, value, addWhenInvalid: false);
		if (flag && addToStore && info.ParsedValue != null)
		{
			AddHeaderToStore(descriptor, info);
		}
		return flag;
	}

	private static bool TryParseAndAddRawHeaderValue(HeaderDescriptor descriptor, HeaderStoreItemInfo info, string value, bool addWhenInvalid)
	{
		if (!info.CanAddParsedValue(descriptor.Parser))
		{
			if (addWhenInvalid)
			{
				AddInvalidValue(info, value ?? string.Empty);
			}
			return false;
		}
		int index = 0;
		if (descriptor.Parser.TryParseValue(value, info.ParsedValue, ref index, out var parsedValue))
		{
			if (value == null || index == value.Length)
			{
				if (parsedValue != null)
				{
					AddParsedValue(info, parsedValue);
				}
				return true;
			}
			List<object> list = new List<object>();
			if (parsedValue != null)
			{
				list.Add(parsedValue);
			}
			while (index < value.Length)
			{
				if (descriptor.Parser.TryParseValue(value, info.ParsedValue, ref index, out parsedValue))
				{
					if (parsedValue != null)
					{
						list.Add(parsedValue);
					}
					continue;
				}
				if (!ContainsNewLine(value, descriptor.Name) && addWhenInvalid)
				{
					AddInvalidValue(info, value);
				}
				return false;
			}
			foreach (object item in list)
			{
				AddParsedValue(info, item);
			}
			return true;
		}
		if (!ContainsNewLine(value, descriptor.Name) && addWhenInvalid)
		{
			AddInvalidValue(info, value ?? string.Empty);
		}
		return false;
	}

	private static void AddParsedValue(HeaderStoreItemInfo info, object value)
	{
		AddValueToStoreValue(value, ref info.ParsedValue);
	}

	private static void AddInvalidValue(HeaderStoreItemInfo info, string value)
	{
		AddValueToStoreValue(value, ref info.InvalidValue);
	}

	private static void AddRawValue(HeaderStoreItemInfo info, string value)
	{
		AddValueToStoreValue(value, ref info.RawValue);
	}

	private static void AddValueToStoreValue<T>(T value, ref object currentStoreValue) where T : class
	{
		if (currentStoreValue == null)
		{
			currentStoreValue = value;
			return;
		}
		List<T> list = currentStoreValue as List<T>;
		if (list == null)
		{
			list = new List<T>(2);
			list.Add((T)currentStoreValue);
			currentStoreValue = list;
		}
		list.Add(value);
	}

	internal object GetParsedValues(HeaderDescriptor descriptor)
	{
		if (!TryGetAndParseHeaderInfo(descriptor, out var info))
		{
			return null;
		}
		return info.ParsedValue;
	}

	internal virtual bool IsAllowedHeaderName(HeaderDescriptor descriptor)
	{
		return true;
	}

	private void PrepareHeaderInfoForAdd(HeaderDescriptor descriptor, out HeaderStoreItemInfo info, out bool addToStore)
	{
		if (!IsAllowedHeaderName(descriptor))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_http_headers_not_allowed_header_name, descriptor.Name));
		}
		addToStore = false;
		if (!TryGetAndParseHeaderInfo(descriptor, out info))
		{
			info = new HeaderStoreItemInfo();
			addToStore = true;
		}
	}

	private void ParseAndAddValue(HeaderDescriptor descriptor, HeaderStoreItemInfo info, string value)
	{
		if (descriptor.Parser == null)
		{
			CheckContainsNewLine(value);
			AddParsedValue(info, value ?? string.Empty);
			return;
		}
		if (!info.CanAddParsedValue(descriptor.Parser))
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_single_value_header, descriptor.Name));
		}
		int index = 0;
		object obj = descriptor.Parser.ParseValue(value, info.ParsedValue, ref index);
		if (value == null || index == value.Length)
		{
			if (obj != null)
			{
				AddParsedValue(info, obj);
			}
			return;
		}
		List<object> list = new List<object>();
		if (obj != null)
		{
			list.Add(obj);
		}
		while (index < value.Length)
		{
			obj = descriptor.Parser.ParseValue(value, info.ParsedValue, ref index);
			if (obj != null)
			{
				list.Add(obj);
			}
		}
		foreach (object item in list)
		{
			AddParsedValue(info, item);
		}
	}

	private HeaderDescriptor GetHeaderDescriptor(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "name");
		}
		if (!HeaderDescriptor.TryGet(name, out var descriptor))
		{
			throw new FormatException(System.SR.net_http_headers_invalid_header_name);
		}
		if ((descriptor.HeaderType & _allowedHeaderTypes) != 0)
		{
			return descriptor;
		}
		if ((descriptor.HeaderType & _treatAsCustomHeaderTypes) != 0)
		{
			return descriptor.AsCustomHeader();
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.net_http_headers_not_allowed_header_name, name));
	}

	private bool TryGetHeaderDescriptor(string name, out HeaderDescriptor descriptor)
	{
		if (string.IsNullOrEmpty(name))
		{
			descriptor = default(HeaderDescriptor);
			return false;
		}
		if (HeaderDescriptor.TryGet(name, out descriptor))
		{
			if ((descriptor.HeaderType & _allowedHeaderTypes) != 0)
			{
				return true;
			}
			if ((descriptor.HeaderType & _treatAsCustomHeaderTypes) != 0)
			{
				descriptor = descriptor.AsCustomHeader();
				return true;
			}
		}
		return false;
	}

	internal static void CheckContainsNewLine(string value)
	{
		if (value == null || !HttpRuleParser.ContainsNewLine(value))
		{
			return;
		}
		throw new FormatException(System.SR.net_http_headers_no_newlines);
	}

	private static bool ContainsNewLine(string value, string name)
	{
		if (HttpRuleParser.ContainsNewLine(value))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_http_log_headers_no_newlines, name, value), "ContainsNewLine");
			}
			return true;
		}
		return false;
	}

	internal static string[] GetStoreValuesAsStringArray(HeaderDescriptor descriptor, HeaderStoreItemInfo info)
	{
		GetStoreValuesAsStringOrStringArray(descriptor, info, out var singleValue, out var multiValue);
		return multiValue ?? new string[1] { singleValue };
	}

	internal static void GetStoreValuesAsStringOrStringArray(HeaderDescriptor descriptor, object sourceValues, out string singleValue, out string[] multiValue)
	{
		if (!(sourceValues is HeaderStoreItemInfo headerStoreItemInfo))
		{
			singleValue = (string)sourceValues;
			multiValue = null;
			return;
		}
		int valueCount = GetValueCount(headerStoreItemInfo);
		singleValue = null;
		Span<string> values;
		if (valueCount == 1)
		{
			multiValue = null;
			values = MemoryMarshal.CreateSpan(ref singleValue, 1);
		}
		else
		{
			values = (multiValue = ((valueCount != 0) ? new string[valueCount] : Array.Empty<string>()));
		}
		int currentIndex = 0;
		ReadStoreValues<string>(values, headerStoreItemInfo.RawValue, null, ref currentIndex);
		ReadStoreValues<object>(values, headerStoreItemInfo.ParsedValue, descriptor.Parser, ref currentIndex);
		ReadStoreValues<string>(values, headerStoreItemInfo.InvalidValue, null, ref currentIndex);
	}

	internal static int GetStoreValuesIntoStringArray(HeaderDescriptor descriptor, object sourceValues, [NotNull] ref string[] values)
	{
		if (values == null)
		{
			values = Array.Empty<string>();
		}
		if (!(sourceValues is HeaderStoreItemInfo headerStoreItemInfo))
		{
			if (values.Length == 0)
			{
				values = new string[1];
			}
			values[0] = (string)sourceValues;
			return 1;
		}
		int valueCount = GetValueCount(headerStoreItemInfo);
		if (valueCount > 0)
		{
			if (values.Length < valueCount)
			{
				values = new string[valueCount];
			}
			int currentIndex = 0;
			ReadStoreValues<string>(values, headerStoreItemInfo.RawValue, null, ref currentIndex);
			ReadStoreValues<object>(values, headerStoreItemInfo.ParsedValue, descriptor.Parser, ref currentIndex);
			ReadStoreValues<string>(values, headerStoreItemInfo.InvalidValue, null, ref currentIndex);
		}
		return valueCount;
	}

	private static int GetValueCount(HeaderStoreItemInfo info)
	{
		int num = Count<string>(info.RawValue);
		num += Count<string>(info.InvalidValue);
		return num + Count<object>(info.ParsedValue);
		static int Count<T>(object valueStore)
		{
			if (valueStore != null)
			{
				if (!(valueStore is List<T> list))
				{
					return 1;
				}
				return list.Count;
			}
			return 0;
		}
	}

	private static void ReadStoreValues<T>(Span<string> values, object storeValue, HttpHeaderParser parser, ref int currentIndex)
	{
		if (storeValue == null)
		{
			return;
		}
		if (!(storeValue is List<T> list))
		{
			values[currentIndex] = ((parser == null) ? storeValue.ToString() : parser.ToString(storeValue));
			currentIndex++;
			return;
		}
		foreach (T item in list)
		{
			object obj = item;
			values[currentIndex] = ((parser == null) ? obj.ToString() : parser.ToString(obj));
			currentIndex++;
		}
	}

	private bool AreEqual(object value, object storeValue, IEqualityComparer comparer)
	{
		return comparer?.Equals(value, storeValue) ?? value.Equals(storeValue);
	}
}
