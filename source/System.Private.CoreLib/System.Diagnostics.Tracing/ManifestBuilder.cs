using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;

namespace System.Diagnostics.Tracing;

internal sealed class ManifestBuilder
{
	private sealed class ChannelInfo
	{
		public string Name;

		public ulong Keywords;

		public EventChannelAttribute Attribs;
	}

	private static readonly string[] s_escapes = new string[8] { "&amp;", "&lt;", "&gt;", "&apos;", "&quot;", "%r", "%n", "%t" };

	private readonly Dictionary<int, string> opcodeTab;

	private Dictionary<int, string> taskTab;

	private Dictionary<int, ChannelInfo> channelTab;

	private Dictionary<ulong, string> keywordTab;

	private Dictionary<string, Type> mapsTab;

	private readonly Dictionary<string, string> stringTab;

	private ulong nextChannelKeywordBit = 9223372036854775808uL;

	private readonly StringBuilder sb;

	private readonly StringBuilder events;

	private readonly StringBuilder templates;

	private readonly string providerName;

	private readonly ResourceManager resources;

	private readonly EventManifestOptions flags;

	private readonly IList<string> errors;

	private readonly Dictionary<string, List<int>> perEventByteArrayArgIndices;

	private string eventName;

	private int numParams;

	private List<int> byteArrArgIndices;

	public IList<string> Errors => errors;

	public bool HasResources => resources != null;

	public ManifestBuilder(string providerName, Guid providerGuid, string dllName, ResourceManager resources, EventManifestOptions flags)
	{
		this.providerName = providerName;
		this.flags = flags;
		this.resources = resources;
		sb = new StringBuilder();
		events = new StringBuilder();
		templates = new StringBuilder();
		opcodeTab = new Dictionary<int, string>();
		stringTab = new Dictionary<string, string>();
		errors = new List<string>();
		perEventByteArrayArgIndices = new Dictionary<string, List<int>>();
		sb.AppendLine("<instrumentationManifest xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
		sb.AppendLine(" <instrumentation xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:win=\"http://manifests.microsoft.com/win/2004/08/windows/events\">");
		sb.AppendLine("  <events xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(27, 2, stringBuilder);
		handler.AppendLiteral("<provider name=\"");
		handler.AppendFormatted(providerName);
		handler.AppendLiteral("\" guid=\"{");
		handler.AppendFormatted(providerGuid);
		handler.AppendLiteral("}\"");
		stringBuilder2.Append(ref handler);
		if (dllName != null)
		{
			stringBuilder = sb;
			StringBuilder stringBuilder3 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(39, 2, stringBuilder);
			handler2.AppendLiteral(" resourceFileName=\"");
			handler2.AppendFormatted(dllName);
			handler2.AppendLiteral("\" messageFileName=\"");
			handler2.AppendFormatted(dllName);
			handler2.AppendLiteral("\"");
			stringBuilder3.Append(ref handler2);
		}
		string value = providerName.Replace("-", "").Replace('.', '_');
		stringBuilder = sb;
		StringBuilder stringBuilder4 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder);
		handler3.AppendLiteral(" symbol=\"");
		handler3.AppendFormatted(value);
		handler3.AppendLiteral("\">");
		stringBuilder4.AppendLine(ref handler3);
	}

	public void AddOpcode(string name, int value)
	{
		if ((flags & EventManifestOptions.Strict) != 0)
		{
			if (value <= 10 || value >= 239)
			{
				ManifestError(SR.Format(SR.EventSource_IllegalOpcodeValue, name, value));
			}
			if (opcodeTab.TryGetValue(value, out var value2) && !name.Equals(value2, StringComparison.Ordinal))
			{
				ManifestError(SR.Format(SR.EventSource_OpcodeCollision, name, value2, value));
			}
		}
		opcodeTab[value] = name;
	}

	public void AddTask(string name, int value)
	{
		if ((flags & EventManifestOptions.Strict) != 0)
		{
			if (value <= 0 || value >= 65535)
			{
				ManifestError(SR.Format(SR.EventSource_IllegalTaskValue, name, value));
			}
			if (taskTab != null && taskTab.TryGetValue(value, out var value2) && !name.Equals(value2, StringComparison.Ordinal))
			{
				ManifestError(SR.Format(SR.EventSource_TaskCollision, name, value2, value));
			}
		}
		if (taskTab == null)
		{
			taskTab = new Dictionary<int, string>();
		}
		taskTab[value] = name;
	}

	public void AddKeyword(string name, ulong value)
	{
		if ((value & (value - 1)) != 0L)
		{
			ManifestError(SR.Format(SR.EventSource_KeywordNeedPowerOfTwo, "0x" + value.ToString("x", CultureInfo.CurrentCulture), name), runtimeCritical: true);
		}
		if ((flags & EventManifestOptions.Strict) != 0)
		{
			if (value >= 17592186044416L && !name.StartsWith("Session", StringComparison.Ordinal))
			{
				ManifestError(SR.Format(SR.EventSource_IllegalKeywordsValue, name, "0x" + value.ToString("x", CultureInfo.CurrentCulture)));
			}
			if (keywordTab != null && keywordTab.TryGetValue(value, out var value2) && !name.Equals(value2, StringComparison.Ordinal))
			{
				ManifestError(SR.Format(SR.EventSource_KeywordCollision, name, value2, "0x" + value.ToString("x", CultureInfo.CurrentCulture)));
			}
		}
		if (keywordTab == null)
		{
			keywordTab = new Dictionary<ulong, string>();
		}
		keywordTab[value] = name;
	}

	public void AddChannel(string name, int value, EventChannelAttribute channelAttribute)
	{
		EventChannel eventChannel = (EventChannel)value;
		if (value < 16 || value > 255)
		{
			ManifestError(SR.Format(SR.EventSource_EventChannelOutOfRange, name, value));
		}
		else if ((int)eventChannel >= 16 && (int)eventChannel <= 19 && channelAttribute != null && EventChannelToChannelType(eventChannel) != channelAttribute.EventChannelType)
		{
			ManifestError(SR.Format(SR.EventSource_ChannelTypeDoesNotMatchEventChannelValue, name, ((EventChannel)value).ToString()));
		}
		ulong channelKeyword = GetChannelKeyword(eventChannel, 0uL);
		if (channelTab == null)
		{
			channelTab = new Dictionary<int, ChannelInfo>(4);
		}
		channelTab[value] = new ChannelInfo
		{
			Name = name,
			Keywords = channelKeyword,
			Attribs = channelAttribute
		};
	}

	private static EventChannelType EventChannelToChannelType(EventChannel channel)
	{
		return (EventChannelType)(channel - 16 + 1);
	}

	private static EventChannelAttribute GetDefaultChannelAttribute(EventChannel channel)
	{
		EventChannelAttribute eventChannelAttribute = new EventChannelAttribute();
		eventChannelAttribute.EventChannelType = EventChannelToChannelType(channel);
		if (eventChannelAttribute.EventChannelType <= EventChannelType.Operational)
		{
			eventChannelAttribute.Enabled = true;
		}
		return eventChannelAttribute;
	}

	public ulong[] GetChannelData()
	{
		if (channelTab == null)
		{
			return Array.Empty<ulong>();
		}
		int num = -1;
		foreach (int key in channelTab.Keys)
		{
			if (key > num)
			{
				num = key;
			}
		}
		ulong[] array = new ulong[num + 1];
		foreach (KeyValuePair<int, ChannelInfo> item in channelTab)
		{
			array[item.Key] = item.Value.Keywords;
		}
		return array;
	}

	public void StartEvent(string eventName, EventAttribute eventAttribute)
	{
		this.eventName = eventName;
		numParams = 0;
		byteArrArgIndices = null;
		events.Append("  <event value=\"").Append(eventAttribute.EventId).Append("\" version=\"")
			.Append(eventAttribute.Version)
			.Append("\" level=\"");
		AppendLevelName(events, eventAttribute.Level);
		events.Append("\" symbol=\"").Append(eventName).Append('"');
		WriteMessageAttrib(events, "event", eventName, eventAttribute.Message);
		if (eventAttribute.Keywords != EventKeywords.None)
		{
			events.Append(" keywords=\"");
			AppendKeywords(events, (ulong)eventAttribute.Keywords, eventName);
			events.Append('"');
		}
		if (eventAttribute.Opcode != 0)
		{
			events.Append(" opcode=\"").Append(GetOpcodeName(eventAttribute.Opcode, eventName)).Append('"');
		}
		if (eventAttribute.Task != 0)
		{
			events.Append(" task=\"").Append(GetTaskName(eventAttribute.Task, eventName)).Append('"');
		}
		if (eventAttribute.Channel != 0)
		{
			events.Append(" channel=\"").Append(GetChannelName(eventAttribute.Channel, eventName, eventAttribute.Message)).Append('"');
		}
	}

	public void AddEventParameter(Type type, string name)
	{
		if (numParams == 0)
		{
			templates.Append("  <template tid=\"").Append(eventName).AppendLine("Args\">");
		}
		if (type == typeof(byte[]))
		{
			if (byteArrArgIndices == null)
			{
				byteArrArgIndices = new List<int>(4);
			}
			byteArrArgIndices.Add(numParams);
			numParams++;
			templates.Append("   <data name=\"").Append(name).AppendLine("Size\" inType=\"win:UInt32\"/>");
		}
		numParams++;
		templates.Append("   <data name=\"").Append(name).Append("\" inType=\"")
			.Append(GetTypeName(type))
			.Append('"');
		if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
		{
			templates.Append(" length=\"").Append(name).Append("Size\"");
		}
		if (type.IsEnum && Enum.GetUnderlyingType(type) != typeof(ulong) && Enum.GetUnderlyingType(type) != typeof(long))
		{
			templates.Append(" map=\"").Append(type.Name).Append('"');
			if (mapsTab == null)
			{
				mapsTab = new Dictionary<string, Type>();
			}
			if (!mapsTab.ContainsKey(type.Name))
			{
				mapsTab.Add(type.Name, type);
			}
		}
		templates.AppendLine("/>");
	}

	public void EndEvent()
	{
		if (numParams > 0)
		{
			templates.AppendLine("  </template>");
			events.Append(" template=\"").Append(eventName).Append("Args\"");
		}
		events.AppendLine("/>");
		if (byteArrArgIndices != null)
		{
			perEventByteArrayArgIndices[eventName] = byteArrArgIndices;
		}
		string key = "event_" + eventName;
		if (stringTab.TryGetValue(key, out var value))
		{
			value = TranslateToManifestConvention(value, eventName);
			stringTab[key] = value;
		}
		eventName = null;
		numParams = 0;
		byteArrArgIndices = null;
	}

	public ulong GetChannelKeyword(EventChannel channel, ulong channelKeyword = 0uL)
	{
		channelKeyword &= 0xF000000000000000uL;
		if (channelTab == null)
		{
			channelTab = new Dictionary<int, ChannelInfo>(4);
		}
		if (channelTab.Count == 8)
		{
			ManifestError(SR.EventSource_MaxChannelExceeded);
		}
		if (!channelTab.TryGetValue((int)channel, out var value))
		{
			if (channelKeyword == 0L)
			{
				channelKeyword = nextChannelKeywordBit;
				nextChannelKeywordBit >>= 1;
			}
		}
		else
		{
			channelKeyword = value.Keywords;
		}
		return channelKeyword;
	}

	public byte[] CreateManifest()
	{
		string s = CreateManifestString();
		return Encoding.UTF8.GetBytes(s);
	}

	public void ManifestError(string msg, bool runtimeCritical = false)
	{
		if ((flags & EventManifestOptions.Strict) != 0)
		{
			errors.Add(msg);
		}
		else if (runtimeCritical)
		{
			throw new ArgumentException(msg);
		}
	}

	private string CreateManifestString()
	{
		Span<char> destination = stackalloc char[16];
		if (channelTab != null)
		{
			sb.AppendLine(" <channels>");
			List<KeyValuePair<int, ChannelInfo>> list = new List<KeyValuePair<int, ChannelInfo>>();
			foreach (KeyValuePair<int, ChannelInfo> item in channelTab)
			{
				list.Add(item);
			}
			list.Sort((KeyValuePair<int, ChannelInfo> p1, KeyValuePair<int, ChannelInfo> p2) => -Comparer<ulong>.Default.Compare(p1.Value.Keywords, p2.Value.Keywords));
			foreach (KeyValuePair<int, ChannelInfo> item2 in list)
			{
				int key = item2.Key;
				ChannelInfo value = item2.Value;
				string text = null;
				bool flag = false;
				string text2 = null;
				if (value.Attribs != null)
				{
					EventChannelAttribute attribs = value.Attribs;
					if (Enum.IsDefined(typeof(EventChannelType), attribs.EventChannelType))
					{
						text = attribs.EventChannelType.ToString();
					}
					flag = attribs.Enabled;
				}
				if (text2 == null)
				{
					text2 = providerName + "/" + value.Name;
				}
				sb.Append("  <channel chid=\"").Append(value.Name).Append("\" name=\"")
					.Append(text2)
					.Append('"');
				WriteMessageAttrib(sb, "channel", value.Name, null);
				sb.Append(" value=\"").Append(key).Append('"');
				if (text != null)
				{
					sb.Append(" type=\"").Append(text).Append('"');
				}
				sb.Append(" enabled=\"").Append(flag ? "true" : "false").Append('"');
				sb.AppendLine("/>");
			}
			sb.AppendLine(" </channels>");
		}
		if (taskTab != null)
		{
			sb.AppendLine(" <tasks>");
			List<int> list2 = new List<int>(taskTab.Keys);
			list2.Sort();
			foreach (int item3 in list2)
			{
				sb.Append("  <task");
				WriteNameAndMessageAttribs(sb, "task", taskTab[item3]);
				sb.Append(" value=\"").Append(item3).AppendLine("\"/>");
			}
			sb.AppendLine(" </tasks>");
		}
		if (mapsTab != null)
		{
			sb.AppendLine(" <maps>");
			foreach (Type value3 in mapsTab.Values)
			{
				bool flag2 = EventSource.IsCustomAttributeDefinedHelper(value3, typeof(FlagsAttribute), flags);
				string value2 = (flag2 ? "bitMap" : "valueMap");
				sb.Append("  <").Append(value2).Append(" name=\"")
					.Append(value3.Name)
					.AppendLine("\">");
				FieldInfo[] array = GetEnumFields(value3);
				bool flag3 = false;
				FieldInfo[] array2 = array;
				foreach (FieldInfo fieldInfo in array2)
				{
					object rawConstantValue = fieldInfo.GetRawConstantValue();
					if (rawConstantValue != null)
					{
						ulong num = ((!(rawConstantValue is ulong)) ? ((ulong)Convert.ToInt64(rawConstantValue)) : ((ulong)rawConstantValue));
						if (!flag2 || ((num & (num - 1)) == 0L && num != 0L))
						{
							num.TryFormat(destination, out var charsWritten, "x");
							Span<char> span = destination.Slice(0, charsWritten);
							sb.Append("   <map value=\"0x").Append(span).Append('"');
							WriteMessageAttrib(sb, "map", value3.Name + "." + fieldInfo.Name, fieldInfo.Name);
							sb.AppendLine("/>");
							flag3 = true;
						}
					}
				}
				if (!flag3)
				{
					sb.Append("   <map value=\"0x0\"");
					WriteMessageAttrib(sb, "map", value3.Name + ".None", "None");
					sb.AppendLine("/>");
				}
				sb.Append("  </").Append(value2).AppendLine(">");
			}
			sb.AppendLine(" </maps>");
		}
		sb.AppendLine(" <opcodes>");
		List<int> list3 = new List<int>(opcodeTab.Keys);
		list3.Sort();
		foreach (int item4 in list3)
		{
			sb.Append("  <opcode");
			WriteNameAndMessageAttribs(sb, "opcode", opcodeTab[item4]);
			sb.Append(" value=\"").Append(item4).AppendLine("\"/>");
		}
		sb.AppendLine(" </opcodes>");
		if (keywordTab != null)
		{
			sb.AppendLine(" <keywords>");
			List<ulong> list4 = new List<ulong>(keywordTab.Keys);
			list4.Sort();
			foreach (ulong item5 in list4)
			{
				sb.Append("  <keyword");
				WriteNameAndMessageAttribs(sb, "keyword", keywordTab[item5]);
				item5.TryFormat(destination, out var charsWritten2, "x");
				Span<char> span2 = destination.Slice(0, charsWritten2);
				sb.Append(" mask=\"0x").Append(span2).AppendLine("\"/>");
			}
			sb.AppendLine(" </keywords>");
		}
		sb.AppendLine(" <events>");
		sb.Append(events);
		sb.AppendLine(" </events>");
		sb.AppendLine(" <templates>");
		if (templates.Length > 0)
		{
			sb.Append(templates);
		}
		else
		{
			sb.AppendLine("    <template tid=\"_empty\"></template>");
		}
		sb.AppendLine(" </templates>");
		sb.AppendLine("</provider>");
		sb.AppendLine("</events>");
		sb.AppendLine("</instrumentation>");
		sb.AppendLine("<localization>");
		string[] array3 = new string[stringTab.Keys.Count];
		stringTab.Keys.CopyTo(array3, 0);
		Array.Sort(array3, 0, array3.Length);
		CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
		sb.Append(" <resources culture=\"").Append(currentUICulture.Name).AppendLine("\">");
		sb.AppendLine("  <stringTable>");
		string[] array4 = array3;
		foreach (string text3 in array4)
		{
			string localizedMessage = GetLocalizedMessage(text3, currentUICulture, etwFormat: true);
			sb.Append("   <string id=\"").Append(text3).Append("\" value=\"")
				.Append(localizedMessage)
				.AppendLine("\"/>");
		}
		sb.AppendLine("  </stringTable>");
		sb.AppendLine(" </resources>");
		sb.AppendLine("</localization>");
		sb.AppendLine("</instrumentationManifest>");
		return sb.ToString();
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "Trimmer does not trim enums")]
		static FieldInfo[] GetEnumFields(Type localEnumType)
		{
			return localEnumType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
		}
	}

	private void WriteNameAndMessageAttribs(StringBuilder stringBuilder, string elementName, string name)
	{
		stringBuilder.Append(" name=\"").Append(name).Append('"');
		WriteMessageAttrib(sb, elementName, name, name);
	}

	private void WriteMessageAttrib(StringBuilder stringBuilder, string elementName, string name, string value)
	{
		string text = null;
		if (resources != null)
		{
			text = elementName + "_" + name;
			string @string = resources.GetString(text, CultureInfo.InvariantCulture);
			if (@string != null)
			{
				value = @string;
			}
		}
		if (value != null)
		{
			if (text == null)
			{
				text = elementName + "_" + name;
			}
			stringBuilder.Append(" message=\"$(string.").Append(text).Append(")\"");
			if (stringTab.TryGetValue(text, out var value2) && !value2.Equals(value))
			{
				ManifestError(SR.Format(SR.EventSource_DuplicateStringKey, text), runtimeCritical: true);
			}
			else
			{
				stringTab[text] = value;
			}
		}
	}

	internal string GetLocalizedMessage(string key, CultureInfo ci, bool etwFormat)
	{
		string value = null;
		if (resources != null)
		{
			string @string = resources.GetString(key, ci);
			if (@string != null)
			{
				value = @string;
				if (etwFormat && key.StartsWith("event_", StringComparison.Ordinal))
				{
					string evtName = key.Substring("event_".Length);
					value = TranslateToManifestConvention(value, evtName);
				}
			}
		}
		if (etwFormat && value == null)
		{
			stringTab.TryGetValue(key, out value);
		}
		return value;
	}

	private static void AppendLevelName(StringBuilder sb, EventLevel level)
	{
		if (level < (EventLevel)16)
		{
			sb.Append("win:");
		}
		string value;
		switch (level)
		{
		case EventLevel.LogAlways:
			value = "LogAlways";
			break;
		case EventLevel.Critical:
			value = "Critical";
			break;
		case EventLevel.Error:
			value = "Error";
			break;
		case EventLevel.Warning:
			value = "Warning";
			break;
		case EventLevel.Informational:
			value = "Informational";
			break;
		case EventLevel.Verbose:
			value = "Verbose";
			break;
		default:
		{
			int num = (int)level;
			value = num.ToString();
			break;
		}
		}
		sb.Append(value);
	}

	private string GetChannelName(EventChannel channel, string eventName, string eventMessage)
	{
		if (channelTab == null || !channelTab.TryGetValue((int)channel, out var value))
		{
			if ((int)channel < 16)
			{
				ManifestError(SR.Format(SR.EventSource_UndefinedChannel, channel, eventName));
			}
			if (channelTab == null)
			{
				channelTab = new Dictionary<int, ChannelInfo>(4);
			}
			string text = channel.ToString();
			if (19 < (int)channel)
			{
				text = "Channel" + text;
			}
			AddChannel(text, (int)channel, GetDefaultChannelAttribute(channel));
			if (!channelTab.TryGetValue((int)channel, out value))
			{
				ManifestError(SR.Format(SR.EventSource_UndefinedChannel, channel, eventName));
			}
		}
		if (resources != null && eventMessage == null)
		{
			eventMessage = resources.GetString("event_" + eventName, CultureInfo.InvariantCulture);
		}
		if (value.Attribs.EventChannelType == EventChannelType.Admin && eventMessage == null)
		{
			ManifestError(SR.Format(SR.EventSource_EventWithAdminChannelMustHaveMessage, eventName, value.Name));
		}
		return value.Name;
	}

	private string GetTaskName(EventTask task, string eventName)
	{
		if (task == EventTask.None)
		{
			return "";
		}
		if (taskTab == null)
		{
			taskTab = new Dictionary<int, string>();
		}
		if (!taskTab.TryGetValue((int)task, out var value))
		{
			return taskTab[(int)task] = eventName;
		}
		return value;
	}

	private string GetOpcodeName(EventOpcode opcode, string eventName)
	{
		switch (opcode)
		{
		case EventOpcode.Info:
			return "win:Info";
		case EventOpcode.Start:
			return "win:Start";
		case EventOpcode.Stop:
			return "win:Stop";
		case EventOpcode.DataCollectionStart:
			return "win:DC_Start";
		case EventOpcode.DataCollectionStop:
			return "win:DC_Stop";
		case EventOpcode.Extension:
			return "win:Extension";
		case EventOpcode.Reply:
			return "win:Reply";
		case EventOpcode.Resume:
			return "win:Resume";
		case EventOpcode.Suspend:
			return "win:Suspend";
		case EventOpcode.Send:
			return "win:Send";
		case EventOpcode.Receive:
			return "win:Receive";
		default:
		{
			if (opcodeTab == null || !opcodeTab.TryGetValue((int)opcode, out var value))
			{
				ManifestError(SR.Format(SR.EventSource_UndefinedOpcode, opcode, eventName), runtimeCritical: true);
				return null;
			}
			return value;
		}
		}
	}

	private void AppendKeywords(StringBuilder sb, ulong keywords, string eventName)
	{
		keywords &= 0xFFFFFFFFFFFFFFFuL;
		bool flag = false;
		for (ulong num = 1uL; num != 0L; num <<= 1)
		{
			if ((keywords & num) != 0L)
			{
				string value = null;
				if ((keywordTab == null || !keywordTab.TryGetValue(num, out value)) && num >= 281474976710656L)
				{
					value = string.Empty;
				}
				if (value == null)
				{
					ManifestError(SR.Format(SR.EventSource_UndefinedKeyword, "0x" + num.ToString("x", CultureInfo.CurrentCulture), eventName), runtimeCritical: true);
					value = string.Empty;
				}
				if (value.Length != 0)
				{
					if (flag)
					{
						sb.Append(' ');
					}
					sb.Append(value);
					flag = true;
				}
			}
		}
	}

	private string GetTypeName(Type type)
	{
		if (type.IsEnum)
		{
			string typeName = GetTypeName(type.GetEnumUnderlyingType());
			return typeName.Replace("win:Int", "win:UInt");
		}
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Boolean:
			return "win:Boolean";
		case TypeCode.Byte:
			return "win:UInt8";
		case TypeCode.Char:
		case TypeCode.UInt16:
			return "win:UInt16";
		case TypeCode.UInt32:
			return "win:UInt32";
		case TypeCode.UInt64:
			return "win:UInt64";
		case TypeCode.SByte:
			return "win:Int8";
		case TypeCode.Int16:
			return "win:Int16";
		case TypeCode.Int32:
			return "win:Int32";
		case TypeCode.Int64:
			return "win:Int64";
		case TypeCode.String:
			return "win:UnicodeString";
		case TypeCode.Single:
			return "win:Float";
		case TypeCode.Double:
			return "win:Double";
		case TypeCode.DateTime:
			return "win:FILETIME";
		default:
			if (type == typeof(Guid))
			{
				return "win:GUID";
			}
			if (type == typeof(IntPtr))
			{
				return "win:Pointer";
			}
			if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
			{
				return "win:Binary";
			}
			ManifestError(SR.Format(SR.EventSource_UnsupportedEventTypeInManifest, type.Name), runtimeCritical: true);
			return string.Empty;
		}
	}

	private static void UpdateStringBuilder([NotNull] ref StringBuilder stringBuilder, string eventMessage, int startIndex, int count)
	{
		if (stringBuilder == null)
		{
			stringBuilder = new StringBuilder();
		}
		stringBuilder.Append(eventMessage, startIndex, count);
	}

	private string TranslateToManifestConvention(string eventMessage, string evtName)
	{
		StringBuilder stringBuilder = null;
		int num = 0;
		int i = 0;
		while (i < eventMessage.Length)
		{
			int num4;
			if (eventMessage[i] == '%')
			{
				UpdateStringBuilder(ref stringBuilder, eventMessage, num, i - num);
				stringBuilder.Append("%%");
				i++;
				num = i;
			}
			else if (i < eventMessage.Length - 1 && ((eventMessage[i] == '{' && eventMessage[i + 1] == '{') || (eventMessage[i] == '}' && eventMessage[i + 1] == '}')))
			{
				UpdateStringBuilder(ref stringBuilder, eventMessage, num, i - num);
				stringBuilder.Append(eventMessage[i]);
				i++;
				i++;
				num = i;
			}
			else if (eventMessage[i] == '{')
			{
				int num2 = i;
				i++;
				int num3 = 0;
				for (; i < eventMessage.Length && char.IsDigit(eventMessage[i]); i++)
				{
					num3 = num3 * 10 + eventMessage[i] - 48;
				}
				if (i < eventMessage.Length && eventMessage[i] == '}')
				{
					i++;
					UpdateStringBuilder(ref stringBuilder, eventMessage, num, num2 - num);
					int value = TranslateIndexToManifestConvention(num3, evtName);
					stringBuilder.Append('%').Append(value);
					if (i < eventMessage.Length && eventMessage[i] == '!')
					{
						i++;
						stringBuilder.Append("%!");
					}
					num = i;
				}
				else
				{
					ManifestError(SR.Format(SR.EventSource_UnsupportedMessageProperty, evtName, eventMessage));
				}
			}
			else if ((num4 = "&<>'\"\r\n\t".IndexOf(eventMessage[i])) >= 0)
			{
				UpdateStringBuilder(ref stringBuilder, eventMessage, num, i - num);
				i++;
				stringBuilder.Append(s_escapes[num4]);
				num = i;
			}
			else
			{
				i++;
			}
		}
		if (stringBuilder == null)
		{
			return eventMessage;
		}
		UpdateStringBuilder(ref stringBuilder, eventMessage, num, i - num);
		return stringBuilder.ToString();
	}

	private int TranslateIndexToManifestConvention(int idx, string evtName)
	{
		if (perEventByteArrayArgIndices.TryGetValue(evtName, out var value))
		{
			foreach (int item in value)
			{
				if (idx >= item)
				{
					idx++;
					continue;
				}
				break;
			}
		}
		return idx + 1;
	}
}
