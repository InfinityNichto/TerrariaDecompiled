using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net;

[EventSource(Name = "Private.InternalDiagnostics.System.Net.Sockets", LocalizationResources = "FxResources.System.Net.Sockets.SR")]
internal sealed class NetEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Default = (EventKeywords)1L;

		public const EventKeywords Debug = (EventKeywords)2L;
	}

	public static readonly System.Net.NetEventSource Log = new System.Net.NetEventSource();

	[NonEvent]
	public static void Accepted(Socket socket, object remoteEp, object localEp)
	{
		if (Log.IsEnabled())
		{
			Log.Accepted(IdOf(remoteEp), IdOf(localEp), GetHashCode(socket));
		}
	}

	[Event(17, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void Accepted(string remoteEp, string localEp, int socketHash)
	{
		WriteEvent(17, remoteEp, localEp, socketHash);
	}

	[NonEvent]
	public static void Connected(Socket socket, object localEp, object remoteEp)
	{
		if (Log.IsEnabled())
		{
			Log.Connected(IdOf(localEp), IdOf(remoteEp), GetHashCode(socket));
		}
	}

	[Event(18, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void Connected(string localEp, string remoteEp, int socketHash)
	{
		WriteEvent(18, localEp, remoteEp, socketHash);
	}

	[NonEvent]
	public static void ConnectedAsyncDns(Socket socket)
	{
		if (Log.IsEnabled())
		{
			Log.ConnectedAsyncDns(GetHashCode(socket));
		}
	}

	[Event(19, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void ConnectedAsyncDns(int socketHash)
	{
		WriteEvent(19, socketHash);
	}

	[NonEvent]
	public static void DumpBuffer(object thisOrContextObject, Memory<byte> buffer, int offset, int count, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled() && offset >= 0 && offset <= buffer.Length - count)
		{
			buffer = buffer.Slice(offset, Math.Min(count, 1024));
			ArraySegment<byte> segment;
			byte[] buffer2 = ((MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out segment) && segment.Offset == 0 && segment.Count == buffer.Length) ? segment.Array : buffer.ToArray());
			Log.DumpBuffer(IdOf(thisOrContextObject), memberName, buffer2);
		}
	}

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
	public static void DumpBuffer(object thisOrContextObject, byte[] buffer, int offset, int count, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled() && offset >= 0 && offset <= buffer.Length - count)
		{
			count = Math.Min(count, 1024);
			byte[] array = buffer;
			if (offset != 0 || count != buffer.Length)
			{
				array = new byte[count];
				Buffer.BlockCopy(buffer, offset, array, 0, count);
			}
			Log.DumpBuffer(IdOf(thisOrContextObject), memberName, array);
		}
	}

	[NonEvent]
	public unsafe static void DumpBuffer(object thisOrContextObject, IntPtr bufferPtr, int count, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			byte[] array = new byte[Math.Min(count, 1024)];
			fixed (byte* destination = array)
			{
				Buffer.MemoryCopy((void*)bufferPtr, destination, array.Length, array.Length);
			}
			Log.DumpBuffer(IdOf(thisOrContextObject), memberName, array);
		}
	}

	[Event(7, Level = EventLevel.Verbose, Keywords = (EventKeywords)2L)]
	private void DumpBuffer(string thisOrContextObject, string memberName, byte[] buffer)
	{
		WriteEvent(7, thisOrContextObject, memberName ?? "(?)", buffer);
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
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, byte[] arg3)
	{
		//The blocks IL_004f, IL_0053, IL_0055, IL_006e, IL_0155 are reachable both inside and outside the pinned region starting at IL_004c. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
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
			arg3 = Array.Empty<byte>();
		}
		fixed (char* ptr5 = arg1)
		{
			char* intPtr;
			byte[] array;
			int size;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				array = arg3;
				fixed (byte* ptr2 = array)
				{
					byte* ptr3 = ptr2;
					size = arg3.Length;
					EventData* ptr4 = stackalloc EventData[4];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)(&size),
						Size = 4
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = size
					};
					WriteEventCore(eventId, 4, ptr4);
				}
				return;
			}
			fixed (char* ptr6 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr6);
				array = arg3;
				fixed (byte* ptr2 = array)
				{
					byte* ptr3 = ptr2;
					size = arg3.Length;
					EventData* ptr4 = stackalloc EventData[4];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)(&size),
						Size = 4
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = size
					};
					WriteEventCore(eventId, 4, ptr4);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3)
	{
		//The blocks IL_0044 are reachable both inside and outside the pinned region starting at IL_0041. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
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
		fixed (char* ptr3 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				EventData* ptr2 = stackalloc EventData[3];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				WriteEventCore(eventId, 3, ptr2);
				return;
			}
			fixed (char* ptr4 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr4);
				EventData* ptr2 = stackalloc EventData[3];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				WriteEventCore(eventId, 3, ptr2);
			}
		}
	}
}
