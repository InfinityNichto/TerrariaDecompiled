using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net;

[EventSource(Name = "Private.InternalDiagnostics.System.Net.Requests")]
internal sealed class NetEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Default = (EventKeywords)1L;

		public const EventKeywords Debug = (EventKeywords)2L;
	}

	public static readonly System.Net.NetEventSource Log = new System.Net.NetEventSource();

	[NonEvent]
	public static void Info(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.Info(IdOf(thisOrContextObject), memberName, (formattableString != null) ? Format(formattableString) : "");
		}
	}

	[NonEvent]
	public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.Info(IdOf(thisOrContextObject), memberName, Format(message).ToString());
		}
	}

	[Event(4, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void Info(string thisOrContextObject, string memberName, string message)
	{
		WriteEvent(4, thisOrContextObject, memberName ?? "(?)", message);
	}

	[NonEvent]
	public static void Error(object thisOrContextObject, FormattableString formattableString, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(formattableString));
		}
	}

	[NonEvent]
	public static void Error(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(message).ToString());
		}
	}

	[Event(5, Level = EventLevel.Error, Keywords = (EventKeywords)1L)]
	private void ErrorMessage(string thisOrContextObject, string memberName, string message)
	{
		WriteEvent(5, thisOrContextObject, memberName ?? "(?)", message);
	}

	[NonEvent]
	public static void Associate(object first, object second, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.Associate(IdOf(first), memberName, IdOf(first), IdOf(second));
		}
	}

	[Event(3, Level = EventLevel.Informational, Keywords = (EventKeywords)1L, Message = "[{2}]<-->[{3}]")]
	private void Associate(string thisOrContextObject, string memberName, string first, string second)
	{
		WriteEvent(3, thisOrContextObject, memberName ?? "(?)", first, second);
	}

	[NonEvent]
	public static string IdOf(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		return value.GetType().Name + "#" + GetHashCode(value);
	}

	[NonEvent]
	public static int GetHashCode(object value)
	{
		return value?.GetHashCode() ?? 0;
	}

	[NonEvent]
	public static object Format(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		string text = null;
		if (text != null)
		{
			return text;
		}
		if (value is Array array)
		{
			return $"{array.GetType().GetElementType()}[{((Array)value).Length}]";
		}
		if (value is ICollection collection)
		{
			return $"{collection.GetType().Name}({collection.Count})";
		}
		if (value is SafeHandle safeHandle)
		{
			return $"{safeHandle.GetType().Name}:{safeHandle.GetHashCode()}(0x{safeHandle.DangerousGetHandle():X})";
		}
		if (value is IntPtr)
		{
			return $"0x{value:X}";
		}
		string text2 = value.ToString();
		if (text2 == null || text2 == value.GetType().FullName)
		{
			return IdOf(value);
		}
		return value;
	}

	[NonEvent]
	private static string Format(FormattableString s)
	{
		switch (s.ArgumentCount)
		{
		case 0:
			return s.Format;
		case 1:
			return string.Format(s.Format, Format(s.GetArgument(0)));
		case 2:
			return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)));
		case 3:
			return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)), Format(s.GetArgument(2)));
		default:
		{
			object[] arguments = s.GetArguments();
			object[] array = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				array[i] = Format(arguments[i]);
			}
			return string.Format(s.Format, array);
		}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3, string arg4)
	{
		//The blocks IL_005a, IL_005d, IL_006f, IL_0075, IL_0079, IL_0084, IL_0085, IL_017b, IL_017f are reachable both inside and outside the pinned region starting at IL_0057. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0085 are reachable both inside and outside the pinned region starting at IL_0080. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0085 are reachable both inside and outside the pinned region starting at IL_0080. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!Log.IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg3 == null)
		{
			arg3 = "";
		}
		if (arg4 == null)
		{
			arg4 = "";
		}
		fixed (char* ptr6 = arg1)
		{
			char* intPtr;
			char* intPtr2;
			EventData* intPtr3;
			nint num;
			nint num2;
			nint num3;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				fixed (char* ptr2 = arg3)
				{
					char* ptr3 = ptr2;
					if (arg4 == null)
					{
						char* ptr4;
						intPtr2 = (ptr4 = null);
						EventData* ptr5 = stackalloc EventData[4];
						intPtr3 = ptr5;
						*intPtr3 = new EventData
						{
							DataPointer = (IntPtr)ptr6,
							Size = (arg1.Length + 1) * 2
						};
						num = (nint)(ptr5 + 1);
						*(EventData*)num = new EventData
						{
							DataPointer = (IntPtr)ptr,
							Size = (arg2.Length + 1) * 2
						};
						num2 = (nint)(ptr5 + 2);
						*(EventData*)num2 = new EventData
						{
							DataPointer = (IntPtr)ptr3,
							Size = (arg3.Length + 1) * 2
						};
						num3 = (nint)(ptr5 + 3);
						*(EventData*)num3 = new EventData
						{
							DataPointer = (IntPtr)ptr4,
							Size = (arg4.Length + 1) * 2
						};
						WriteEventCore(eventId, 4, ptr5);
						return;
					}
					fixed (char* ptr7 = &arg4.GetPinnableReference())
					{
						char* ptr4;
						intPtr2 = (ptr4 = ptr7);
						EventData* ptr5 = stackalloc EventData[4];
						intPtr3 = ptr5;
						*intPtr3 = new EventData
						{
							DataPointer = (IntPtr)ptr6,
							Size = (arg1.Length + 1) * 2
						};
						num = (nint)(ptr5 + 1);
						*(EventData*)num = new EventData
						{
							DataPointer = (IntPtr)ptr,
							Size = (arg2.Length + 1) * 2
						};
						num2 = (nint)(ptr5 + 2);
						*(EventData*)num2 = new EventData
						{
							DataPointer = (IntPtr)ptr3,
							Size = (arg3.Length + 1) * 2
						};
						num3 = (nint)(ptr5 + 3);
						*(EventData*)num3 = new EventData
						{
							DataPointer = (IntPtr)ptr4,
							Size = (arg4.Length + 1) * 2
						};
						WriteEventCore(eventId, 4, ptr5);
					}
				}
				return;
			}
			fixed (char* ptr8 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr8);
				fixed (char* ptr2 = arg3)
				{
					char* ptr3 = ptr2;
					if (arg4 == null)
					{
						char* ptr4;
						intPtr2 = (ptr4 = null);
						EventData* ptr5 = stackalloc EventData[4];
						intPtr3 = ptr5;
						*intPtr3 = new EventData
						{
							DataPointer = (IntPtr)ptr6,
							Size = (arg1.Length + 1) * 2
						};
						num = (nint)(ptr5 + 1);
						*(EventData*)num = new EventData
						{
							DataPointer = (IntPtr)ptr,
							Size = (arg2.Length + 1) * 2
						};
						num2 = (nint)(ptr5 + 2);
						*(EventData*)num2 = new EventData
						{
							DataPointer = (IntPtr)ptr3,
							Size = (arg3.Length + 1) * 2
						};
						num3 = (nint)(ptr5 + 3);
						*(EventData*)num3 = new EventData
						{
							DataPointer = (IntPtr)ptr4,
							Size = (arg4.Length + 1) * 2
						};
						WriteEventCore(eventId, 4, ptr5);
						return;
					}
					fixed (char* ptr7 = &arg4.GetPinnableReference())
					{
						char* ptr4;
						intPtr2 = (ptr4 = ptr7);
						EventData* ptr5 = stackalloc EventData[4];
						intPtr3 = ptr5;
						*intPtr3 = new EventData
						{
							DataPointer = (IntPtr)ptr6,
							Size = (arg1.Length + 1) * 2
						};
						num = (nint)(ptr5 + 1);
						*(EventData*)num = new EventData
						{
							DataPointer = (IntPtr)ptr,
							Size = (arg2.Length + 1) * 2
						};
						num2 = (nint)(ptr5 + 2);
						*(EventData*)num2 = new EventData
						{
							DataPointer = (IntPtr)ptr3,
							Size = (arg3.Length + 1) * 2
						};
						num3 = (nint)(ptr5 + 3);
						*(EventData*)num3 = new EventData
						{
							DataPointer = (IntPtr)ptr4,
							Size = (arg4.Length + 1) * 2
						};
						WriteEventCore(eventId, 4, ptr5);
					}
				}
			}
		}
	}
}
