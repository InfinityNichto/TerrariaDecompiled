using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Win32;

namespace System.Diagnostics.Tracing;

internal class EventProvider : IDisposable
{
	public struct EventData
	{
		internal ulong Ptr;

		internal uint Size;

		internal uint Reserved;
	}

	public struct SessionInfo
	{
		internal int sessionIdBit;

		internal int etwSessionId;

		internal SessionInfo(int sessionIdBit_, int etwSessionId_)
		{
			sessionIdBit = sessionIdBit_;
			etwSessionId = etwSessionId_;
		}
	}

	public enum WriteEventErrorCode
	{
		NoError,
		NoFreeBuffers,
		EventTooBig,
		NullInput,
		TooManyArgs,
		Other
	}

	private delegate void SessionInfoCallback(int etwSessionId, long matchAllKeywords, ref List<SessionInfo> sessionList);

	private struct EightObjects
	{
		internal object _arg0;

		private object _arg1;

		private object _arg2;

		private object _arg3;

		private object _arg4;

		private object _arg5;

		private object _arg6;

		private object _arg7;
	}

	internal IEventProvider m_eventProvider;

	private Interop.Advapi32.EtwEnableCallback m_etwCallback;

	private long m_regHandle;

	private byte m_level;

	private long m_anyKeywordMask;

	private long m_allKeywordMask;

	private List<SessionInfo> m_liveSessions;

	private bool m_enabled;

	private string m_providerName;

	private Guid m_providerId;

	internal bool m_disposed;

	[ThreadStatic]
	private static WriteEventErrorCode s_returnCode;

	private const int BasicTypeAllocationBufferSize = 16;

	private const int EtwMaxNumberArguments = 128;

	private const int EtwAPIMaxRefObjCount = 8;

	private const int TraceEventMaximumSize = 65482;

	private static bool m_setInformationMissing;

	protected EventLevel Level
	{
		get
		{
			return (EventLevel)m_level;
		}
		set
		{
			m_level = (byte)value;
		}
	}

	protected EventKeywords MatchAnyKeyword
	{
		get
		{
			return (EventKeywords)m_anyKeywordMask;
		}
		set
		{
			m_anyKeywordMask = (long)value;
		}
	}

	protected EventKeywords MatchAllKeyword
	{
		get
		{
			return (EventKeywords)m_allKeywordMask;
		}
		set
		{
			m_allKeywordMask = (long)value;
		}
	}

	internal EventProvider(EventProviderType providerType)
	{
		m_eventProvider = providerType switch
		{
			EventProviderType.ETW => new EtwEventProvider(), 
			EventProviderType.EventPipe => new EventPipeEventProvider(), 
			_ => new NoOpEventProvider(), 
		};
	}

	internal unsafe void Register(EventSource eventSource)
	{
		m_etwCallback = EtwEnableCallBack;
		uint num = EventRegister(eventSource, m_etwCallback);
		if (num != 0)
		{
			throw new ArgumentException(Interop.Kernel32.GetMessage((int)num));
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (m_disposed)
		{
			return;
		}
		m_enabled = false;
		long num = 0L;
		lock (EventListener.EventListenersLock)
		{
			if (m_disposed)
			{
				return;
			}
			num = m_regHandle;
			m_regHandle = 0L;
			m_disposed = true;
		}
		if (num != 0L)
		{
			EventUnregister(num);
		}
	}

	public virtual void Close()
	{
		Dispose();
	}

	~EventProvider()
	{
		Dispose(disposing: false);
	}

	private unsafe void EtwEnableCallBack(in Guid sourceId, int controlCode, byte setLevel, long anyKeyword, long allKeyword, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext)
	{
		try
		{
			ControllerCommand command = ControllerCommand.Update;
			IDictionary<string, string> dictionary = null;
			bool flag = false;
			switch (controlCode)
			{
			case 1:
			{
				m_enabled = true;
				m_level = setLevel;
				m_anyKeywordMask = anyKeyword;
				m_allKeywordMask = allKeyword;
				List<KeyValuePair<SessionInfo, bool>> sessions = GetSessions();
				if (sessions.Count == 0)
				{
					sessions.Add(new KeyValuePair<SessionInfo, bool>(new SessionInfo(0, 0), value: true));
				}
				foreach (KeyValuePair<SessionInfo, bool> item in sessions)
				{
					int sessionIdBit = item.Key.sessionIdBit;
					int etwSessionId = item.Key.etwSessionId;
					bool value = item.Value;
					flag = true;
					dictionary = null;
					if (sessions.Count > 1)
					{
						filterData = null;
					}
					if (value && GetDataFromController(etwSessionId, filterData, out command, out var data, out var dataStart))
					{
						dictionary = new Dictionary<string, string>(4);
						if (data != null)
						{
							while (dataStart < data.Length)
							{
								int num = FindNull(data, dataStart);
								int num2 = num + 1;
								int num3 = FindNull(data, num2);
								if (num3 < data.Length)
								{
									string @string = Encoding.UTF8.GetString(data, dataStart, num - dataStart);
									string string2 = Encoding.UTF8.GetString(data, num2, num3 - num2);
									dictionary[@string] = string2;
								}
								dataStart = num3 + 1;
							}
						}
					}
					OnControllerCommand(command, dictionary, value ? sessionIdBit : (-sessionIdBit), etwSessionId);
				}
				break;
			}
			case 0:
				m_enabled = false;
				m_level = 0;
				m_anyKeywordMask = 0L;
				m_allKeywordMask = 0L;
				m_liveSessions = null;
				break;
			case 2:
				command = ControllerCommand.SendManifest;
				break;
			default:
				return;
			}
			if (!flag)
			{
				OnControllerCommand(command, dictionary, 0, 0);
			}
		}
		catch
		{
		}
	}

	protected virtual void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments, int sessionId, int etwSessionId)
	{
	}

	private static int FindNull(byte[] buffer, int idx)
	{
		while (idx < buffer.Length && buffer[idx] != 0)
		{
			idx++;
		}
		return idx;
	}

	private List<KeyValuePair<SessionInfo, bool>> GetSessions()
	{
		List<SessionInfo> sessionList2 = null;
		GetSessionInfo(delegate(int etwSessionId, long matchAllKeywords, ref List<SessionInfo> sessionList)
		{
			GetSessionInfoCallback(etwSessionId, matchAllKeywords, ref sessionList);
		}, ref sessionList2);
		List<KeyValuePair<SessionInfo, bool>> list = new List<KeyValuePair<SessionInfo, bool>>();
		if (m_liveSessions != null)
		{
			foreach (SessionInfo liveSession in m_liveSessions)
			{
				int index;
				if ((index = IndexOfSessionInList(sessionList2, liveSession.etwSessionId)) < 0 || sessionList2[index].sessionIdBit != liveSession.sessionIdBit)
				{
					list.Add(new KeyValuePair<SessionInfo, bool>(liveSession, value: false));
				}
			}
		}
		if (sessionList2 != null)
		{
			foreach (SessionInfo item in sessionList2)
			{
				int index2;
				if ((index2 = IndexOfSessionInList(m_liveSessions, item.etwSessionId)) < 0 || m_liveSessions[index2].sessionIdBit != item.sessionIdBit)
				{
					list.Add(new KeyValuePair<SessionInfo, bool>(item, value: true));
				}
			}
		}
		m_liveSessions = sessionList2;
		return list;
	}

	private static void GetSessionInfoCallback(int etwSessionId, long matchAllKeywords, ref List<SessionInfo> sessionList)
	{
		uint value = (uint)SessionMask.FromEventKeywords((ulong)matchAllKeywords);
		int num = BitOperations.PopCount(value);
		if (num <= 1)
		{
			if (sessionList == null)
			{
				sessionList = new List<SessionInfo>(8);
			}
			num = ((num != 1) ? BitOperations.PopCount((uint)SessionMask.All) : BitOperations.TrailingZeroCount(value));
			sessionList.Add(new SessionInfo(num + 1, etwSessionId));
		}
	}

	private unsafe void GetSessionInfo(SessionInfoCallback action, ref List<SessionInfo> sessionList)
	{
		int ReturnLength = 256;
		byte* ptr = stackalloc byte[(int)(uint)ReturnLength];
		byte* ptr2 = ptr;
		try
		{
			while (true)
			{
				int num = 0;
				fixed (Guid* inBuffer = &m_providerId)
				{
					num = Interop.Advapi32.EnumerateTraceGuidsEx(Interop.Advapi32.TRACE_QUERY_INFO_CLASS.TraceGuidQueryInfo, inBuffer, sizeof(Guid), ptr2, ReturnLength, out ReturnLength);
				}
				switch (num)
				{
				default:
					return;
				case 122:
					if (ptr2 != ptr)
					{
						byte* ptr7 = ptr2;
						ptr2 = null;
						Marshal.FreeHGlobal((IntPtr)ptr7);
					}
					break;
				case 0:
				{
					Interop.Advapi32.TRACE_GUID_INFO* ptr3 = (Interop.Advapi32.TRACE_GUID_INFO*)ptr2;
					Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO* ptr4 = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)(ptr3 + 1);
					int currentProcessId = (int)Interop.Kernel32.GetCurrentProcessId();
					for (int i = 0; i < ptr3->InstanceCount; i++)
					{
						if (ptr4->Pid == currentProcessId)
						{
							Interop.Advapi32.TRACE_ENABLE_INFO* ptr5 = (Interop.Advapi32.TRACE_ENABLE_INFO*)(ptr4 + 1);
							for (int j = 0; j < ptr4->EnableCount; j++)
							{
								action(ptr5[j].LoggerId, ptr5[j].MatchAllKeyword, ref sessionList);
							}
						}
						if (ptr4->NextOffset == 0)
						{
							break;
						}
						byte* ptr6 = (byte*)ptr4;
						ptr4 = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)(ptr6 + ptr4->NextOffset);
					}
					return;
				}
				}
				ptr2 = (byte*)(void*)Marshal.AllocHGlobal(ReturnLength);
			}
		}
		finally
		{
			if (ptr2 != null && ptr2 != ptr)
			{
				Marshal.FreeHGlobal((IntPtr)ptr2);
			}
		}
	}

	private static int IndexOfSessionInList(List<SessionInfo> sessions, int etwSessionId)
	{
		if (sessions == null)
		{
			return -1;
		}
		for (int i = 0; i < sessions.Count; i++)
		{
			if (sessions[i].etwSessionId == etwSessionId)
			{
				return i;
			}
		}
		return -1;
	}

	private unsafe bool GetDataFromController(int etwSessionId, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, out ControllerCommand command, out byte[] data, out int dataStart)
	{
		data = null;
		dataStart = 0;
		if (filterData == null)
		{
			string text = "\\Microsoft\\Windows\\CurrentVersion\\Winevt\\Publishers\\{" + m_providerId.ToString() + "}";
			_ = IntPtr.Size;
			text = "Software\\Wow6432Node" + text;
			string name = "ControllerData_Session_" + etwSessionId.ToString(CultureInfo.InvariantCulture);
			using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(text))
			{
				data = registryKey?.GetValue(name, null) as byte[];
				if (data != null)
				{
					command = ControllerCommand.Update;
					return true;
				}
			}
			command = ControllerCommand.Update;
			return false;
		}
		if (filterData->Ptr != 0L && 0 < filterData->Size && filterData->Size <= 102400)
		{
			data = new byte[filterData->Size];
			Marshal.Copy((IntPtr)(void*)filterData->Ptr, data, 0, data.Length);
		}
		command = (ControllerCommand)filterData->Type;
		return true;
	}

	public bool IsEnabled()
	{
		return m_enabled;
	}

	public bool IsEnabled(byte level, long keywords)
	{
		if (!m_enabled)
		{
			return false;
		}
		if ((level <= m_level || m_level == 0) && (keywords == 0L || ((keywords & m_anyKeywordMask) != 0L && (keywords & m_allKeywordMask) == m_allKeywordMask)))
		{
			return true;
		}
		return false;
	}

	public static WriteEventErrorCode GetLastWriteEventError()
	{
		return s_returnCode;
	}

	private static void SetLastError(WriteEventErrorCode error)
	{
		s_returnCode = error;
	}

	private unsafe static object EncodeObject(ref object data, ref EventData* dataDescriptor, ref byte* dataBuffer, ref uint totalEventSize)
	{
		string text;
		byte[] array;
		while (true)
		{
			dataDescriptor->Reserved = 0u;
			text = data as string;
			array = null;
			if (text != null)
			{
				dataDescriptor->Size = (uint)((text.Length + 1) * 2);
				break;
			}
			if ((array = data as byte[]) != null)
			{
				*(int*)dataBuffer = array.Length;
				dataDescriptor->Ptr = (ulong)dataBuffer;
				dataDescriptor->Size = 4u;
				totalEventSize += dataDescriptor->Size;
				dataDescriptor++;
				dataBuffer += 16;
				dataDescriptor->Size = (uint)array.Length;
				break;
			}
			if (data is IntPtr)
			{
				dataDescriptor->Size = (uint)sizeof(IntPtr);
				IntPtr* ptr = (IntPtr*)dataBuffer;
				*ptr = (IntPtr)data;
				dataDescriptor->Ptr = (ulong)ptr;
				break;
			}
			if (data is int)
			{
				dataDescriptor->Size = 4u;
				int* ptr2 = (int*)dataBuffer;
				*ptr2 = (int)data;
				dataDescriptor->Ptr = (ulong)ptr2;
				break;
			}
			if (data is long)
			{
				dataDescriptor->Size = 8u;
				long* ptr3 = (long*)dataBuffer;
				*ptr3 = (long)data;
				dataDescriptor->Ptr = (ulong)ptr3;
				break;
			}
			if (data is uint)
			{
				dataDescriptor->Size = 4u;
				uint* ptr4 = (uint*)dataBuffer;
				*ptr4 = (uint)data;
				dataDescriptor->Ptr = (ulong)ptr4;
				break;
			}
			if (data is ulong)
			{
				dataDescriptor->Size = 8u;
				ulong* ptr5 = (ulong*)dataBuffer;
				*ptr5 = (ulong)data;
				dataDescriptor->Ptr = (ulong)ptr5;
				break;
			}
			if (data is char)
			{
				dataDescriptor->Size = 2u;
				char* ptr6 = (char*)dataBuffer;
				*ptr6 = (char)data;
				dataDescriptor->Ptr = (ulong)ptr6;
				break;
			}
			if (data is byte)
			{
				dataDescriptor->Size = 1u;
				byte* ptr7 = dataBuffer;
				*ptr7 = (byte)data;
				dataDescriptor->Ptr = (ulong)ptr7;
				break;
			}
			if (data is short)
			{
				dataDescriptor->Size = 2u;
				short* ptr8 = (short*)dataBuffer;
				*ptr8 = (short)data;
				dataDescriptor->Ptr = (ulong)ptr8;
				break;
			}
			if (data is sbyte)
			{
				dataDescriptor->Size = 1u;
				sbyte* ptr9 = (sbyte*)dataBuffer;
				*ptr9 = (sbyte)data;
				dataDescriptor->Ptr = (ulong)ptr9;
				break;
			}
			if (data is ushort)
			{
				dataDescriptor->Size = 2u;
				ushort* ptr10 = (ushort*)dataBuffer;
				*ptr10 = (ushort)data;
				dataDescriptor->Ptr = (ulong)ptr10;
				break;
			}
			if (data is float)
			{
				dataDescriptor->Size = 4u;
				float* ptr11 = (float*)dataBuffer;
				*ptr11 = (float)data;
				dataDescriptor->Ptr = (ulong)ptr11;
				break;
			}
			if (data is double)
			{
				dataDescriptor->Size = 8u;
				double* ptr12 = (double*)dataBuffer;
				*ptr12 = (double)data;
				dataDescriptor->Ptr = (ulong)ptr12;
				break;
			}
			if (data is bool)
			{
				dataDescriptor->Size = 4u;
				int* ptr13 = (int*)dataBuffer;
				if ((bool)data)
				{
					*ptr13 = 1;
				}
				else
				{
					*ptr13 = 0;
				}
				dataDescriptor->Ptr = (ulong)ptr13;
				break;
			}
			if (data is Guid)
			{
				dataDescriptor->Size = (uint)sizeof(Guid);
				Guid* ptr14 = (Guid*)dataBuffer;
				*ptr14 = (Guid)data;
				dataDescriptor->Ptr = (ulong)ptr14;
				break;
			}
			if (data is decimal)
			{
				dataDescriptor->Size = 16u;
				decimal* ptr15 = (decimal*)dataBuffer;
				*ptr15 = (decimal)data;
				dataDescriptor->Ptr = (ulong)ptr15;
				break;
			}
			if (data is DateTime)
			{
				long num = 0L;
				if (((DateTime)data).Ticks > 504911232000000000L)
				{
					num = ((DateTime)data).ToFileTimeUtc();
				}
				dataDescriptor->Size = 8u;
				long* ptr16 = (long*)dataBuffer;
				*ptr16 = num;
				dataDescriptor->Ptr = (ulong)ptr16;
				break;
			}
			if (data is Enum)
			{
				try
				{
					Type underlyingType = Enum.GetUnderlyingType(data.GetType());
					if (underlyingType == typeof(ulong))
					{
						data = (ulong)data;
					}
					else if (underlyingType == typeof(long))
					{
						data = (long)data;
					}
					else
					{
						data = (int)Convert.ToInt64(data);
					}
				}
				catch
				{
					goto IL_0411;
				}
				continue;
			}
			goto IL_0411;
			IL_0411:
			text = ((data != null) ? data.ToString() : "");
			dataDescriptor->Size = (uint)((text.Length + 1) * 2);
			break;
		}
		totalEventSize += dataDescriptor->Size;
		dataDescriptor++;
		dataBuffer += 16;
		return ((object)text) ?? ((object)array);
	}

	internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* childActivityID, object[] eventPayload)
	{
		//The blocks IL_0220, IL_0231, IL_0242, IL_0255, IL_025a, IL_0263, IL_0264, IL_0275, IL_0286, IL_0299, IL_029e, IL_02a7, IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_021b. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0264, IL_0275, IL_0286, IL_0299, IL_029e, IL_02a7, IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_025f. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0264, IL_0275, IL_0286, IL_0299, IL_029e, IL_02a7, IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_025f. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		WriteEventErrorCode writeEventErrorCode = WriteEventErrorCode.NoError;
		if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
		{
			int num = eventPayload.Length;
			if (num > 128)
			{
				s_returnCode = WriteEventErrorCode.TooManyArgs;
				return false;
			}
			uint totalEventSize = 0u;
			int i = 0;
			EightObjects eightObjects = default(EightObjects);
			Span<int> span = stackalloc int[8];
			Span<object> span2 = new Span<object>(ref eightObjects._arg0, 8);
			EventData* ptr = stackalloc EventData[2 * num];
			for (int j = 0; j < 2 * num; j++)
			{
				ptr[j] = default(EventData);
			}
			EventData* dataDescriptor = ptr;
			byte* ptr2 = stackalloc byte[(int)(uint)(32 * num)];
			byte* dataBuffer = ptr2;
			bool flag = false;
			for (int k = 0; k < eventPayload.Length; k++)
			{
				if (eventPayload[k] != null)
				{
					object obj = EncodeObject(ref eventPayload[k], ref dataDescriptor, ref dataBuffer, ref totalEventSize);
					if (obj == null)
					{
						continue;
					}
					int num2 = (int)(dataDescriptor - ptr - 1);
					if (!(obj is string))
					{
						if (eventPayload.Length + num2 + 1 - k > 128)
						{
							s_returnCode = WriteEventErrorCode.TooManyArgs;
							return false;
						}
						flag = true;
					}
					if (i >= span2.Length)
					{
						Span<object> span3 = new object[span2.Length * 2];
						span2.CopyTo(span3);
						span2 = span3;
						Span<int> span4 = new int[span.Length * 2];
						span.CopyTo(span4);
						span = span4;
					}
					span2[i] = obj;
					span[i] = num2;
					i++;
					continue;
				}
				s_returnCode = WriteEventErrorCode.NullInput;
				return false;
			}
			num = (int)(dataDescriptor - ptr);
			if (totalEventSize > 65482)
			{
				s_returnCode = WriteEventErrorCode.EventTooBig;
				return false;
			}
			if (!flag && i <= 8)
			{
				for (; i < 8; i++)
				{
					span2[i] = null;
					span[i] = -1;
				}
				fixed (char* ptr13 = (string)span2[0])
				{
					string obj2 = (string)span2[1];
					char* intPtr;
					object obj3;
					object obj4;
					char* intPtr2;
					object obj5;
					object obj6;
					char* intPtr3;
					object obj7;
					object obj8;
					char* intPtr4;
					if (obj2 == null)
					{
						char* ptr3;
						intPtr = (ptr3 = null);
						obj3 = (string)span2[2];
						fixed (char* ptr4 = (string)obj3)
						{
							char* ptr5 = ptr4;
							obj4 = (string)span2[3];
							if (obj4 == null)
							{
								char* ptr6;
								intPtr2 = (ptr6 = null);
								obj5 = (string)span2[4];
								fixed (char* ptr7 = (string)obj5)
								{
									char* ptr8 = ptr7;
									obj6 = (string)span2[5];
									if (obj6 == null)
									{
										char* ptr9;
										intPtr3 = (ptr9 = null);
										obj7 = (string)span2[6];
										fixed (char* ptr10 = (string)obj7)
										{
											char* ptr11 = ptr10;
											obj8 = (string)span2[7];
											if (obj8 == null)
											{
												char* ptr12;
												intPtr4 = (ptr12 = null);
												dataDescriptor = ptr;
												if (span2[0] != null)
												{
													dataDescriptor[span[0]].Ptr = (ulong)ptr13;
												}
												if (span2[1] != null)
												{
													dataDescriptor[span[1]].Ptr = (ulong)ptr3;
												}
												if (span2[2] != null)
												{
													dataDescriptor[span[2]].Ptr = (ulong)ptr5;
												}
												if (span2[3] != null)
												{
													dataDescriptor[span[3]].Ptr = (ulong)ptr6;
												}
												if (span2[4] != null)
												{
													dataDescriptor[span[4]].Ptr = (ulong)ptr8;
												}
												if (span2[5] != null)
												{
													dataDescriptor[span[5]].Ptr = (ulong)ptr9;
												}
												if (span2[6] != null)
												{
													dataDescriptor[span[6]].Ptr = (ulong)ptr11;
												}
												if (span2[7] != null)
												{
													dataDescriptor[span[7]].Ptr = (ulong)ptr12;
												}
												writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
											}
											else
											{
												fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
												{
													char* ptr12;
													intPtr4 = (ptr12 = ptr14);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
											}
										}
									}
									else
									{
										fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
										{
											char* ptr9;
											intPtr3 = (ptr9 = ptr15);
											obj7 = (string)span2[6];
											fixed (char* ptr10 = (string)obj7)
											{
												char* ptr11 = ptr10;
												obj8 = (string)span2[7];
												if (obj8 == null)
												{
													char* ptr12;
													intPtr4 = (ptr12 = null);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
												else
												{
													fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
													{
														char* ptr12;
														intPtr4 = (ptr12 = ptr14);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
												}
											}
										}
									}
								}
							}
							else
							{
								fixed (char* ptr16 = &((string)obj4).GetPinnableReference())
								{
									char* ptr6;
									intPtr2 = (ptr6 = ptr16);
									obj5 = (string)span2[4];
									fixed (char* ptr7 = (string)obj5)
									{
										char* ptr8 = ptr7;
										obj6 = (string)span2[5];
										if (obj6 == null)
										{
											char* ptr9;
											intPtr3 = (ptr9 = null);
											obj7 = (string)span2[6];
											fixed (char* ptr10 = (string)obj7)
											{
												char* ptr11 = ptr10;
												obj8 = (string)span2[7];
												if (obj8 == null)
												{
													char* ptr12;
													intPtr4 = (ptr12 = null);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
												else
												{
													fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
													{
														char* ptr12;
														intPtr4 = (ptr12 = ptr14);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
												}
											}
										}
										else
										{
											fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
											{
												char* ptr9;
												intPtr3 = (ptr9 = ptr15);
												obj7 = (string)span2[6];
												fixed (char* ptr10 = (string)obj7)
												{
													char* ptr11 = ptr10;
													obj8 = (string)span2[7];
													if (obj8 == null)
													{
														char* ptr12;
														intPtr4 = (ptr12 = null);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
													else
													{
														fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
														{
															char* ptr12;
															intPtr4 = (ptr12 = ptr14);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					else
					{
						fixed (char* ptr17 = &obj2.GetPinnableReference())
						{
							char* ptr3;
							intPtr = (ptr3 = ptr17);
							obj3 = (string)span2[2];
							fixed (char* ptr4 = (string)obj3)
							{
								char* ptr5 = ptr4;
								obj4 = (string)span2[3];
								if (obj4 == null)
								{
									char* ptr6;
									intPtr2 = (ptr6 = null);
									obj5 = (string)span2[4];
									fixed (char* ptr7 = (string)obj5)
									{
										char* ptr8 = ptr7;
										obj6 = (string)span2[5];
										if (obj6 == null)
										{
											char* ptr9;
											intPtr3 = (ptr9 = null);
											obj7 = (string)span2[6];
											fixed (char* ptr10 = (string)obj7)
											{
												char* ptr11 = ptr10;
												obj8 = (string)span2[7];
												if (obj8 == null)
												{
													char* ptr12;
													intPtr4 = (ptr12 = null);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
												else
												{
													fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
													{
														char* ptr12;
														intPtr4 = (ptr12 = ptr14);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
												}
											}
										}
										else
										{
											fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
											{
												char* ptr9;
												intPtr3 = (ptr9 = ptr15);
												obj7 = (string)span2[6];
												fixed (char* ptr10 = (string)obj7)
												{
													char* ptr11 = ptr10;
													obj8 = (string)span2[7];
													if (obj8 == null)
													{
														char* ptr12;
														intPtr4 = (ptr12 = null);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
													else
													{
														fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
														{
															char* ptr12;
															intPtr4 = (ptr12 = ptr14);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
													}
												}
											}
										}
									}
								}
								else
								{
									fixed (char* ptr16 = &((string)obj4).GetPinnableReference())
									{
										char* ptr6;
										intPtr2 = (ptr6 = ptr16);
										obj5 = (string)span2[4];
										fixed (char* ptr7 = (string)obj5)
										{
											char* ptr8 = ptr7;
											obj6 = (string)span2[5];
											if (obj6 == null)
											{
												char* ptr9;
												intPtr3 = (ptr9 = null);
												obj7 = (string)span2[6];
												fixed (char* ptr10 = (string)obj7)
												{
													char* ptr11 = ptr10;
													obj8 = (string)span2[7];
													if (obj8 == null)
													{
														char* ptr12;
														intPtr4 = (ptr12 = null);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
													else
													{
														fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
														{
															char* ptr12;
															intPtr4 = (ptr12 = ptr14);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
													}
												}
											}
											else
											{
												fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
												{
													char* ptr9;
													intPtr3 = (ptr9 = ptr15);
													obj7 = (string)span2[6];
													fixed (char* ptr10 = (string)obj7)
													{
														char* ptr11 = ptr10;
														obj8 = (string)span2[7];
														if (obj8 == null)
														{
															char* ptr12;
															intPtr4 = (ptr12 = null);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
														else
														{
															fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
															{
																char* ptr12;
																intPtr4 = (ptr12 = ptr14);
																dataDescriptor = ptr;
																if (span2[0] != null)
																{
																	dataDescriptor[span[0]].Ptr = (ulong)ptr13;
																}
																if (span2[1] != null)
																{
																	dataDescriptor[span[1]].Ptr = (ulong)ptr3;
																}
																if (span2[2] != null)
																{
																	dataDescriptor[span[2]].Ptr = (ulong)ptr5;
																}
																if (span2[3] != null)
																{
																	dataDescriptor[span[3]].Ptr = (ulong)ptr6;
																}
																if (span2[4] != null)
																{
																	dataDescriptor[span[4]].Ptr = (ulong)ptr8;
																}
																if (span2[5] != null)
																{
																	dataDescriptor[span[5]].Ptr = (ulong)ptr9;
																}
																if (span2[6] != null)
																{
																	dataDescriptor[span[6]].Ptr = (ulong)ptr11;
																}
																if (span2[7] != null)
																{
																	dataDescriptor[span[7]].Ptr = (ulong)ptr12;
																}
																writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				dataDescriptor = ptr;
				GCHandle[] array = new GCHandle[i];
				for (int l = 0; l < i; l++)
				{
					array[l] = GCHandle.Alloc(span2[l], GCHandleType.Pinned);
					if (span2[l] is string)
					{
						fixed (char* ptr18 = (string)span2[l])
						{
							dataDescriptor[span[l]].Ptr = (ulong)ptr18;
						}
					}
					else
					{
						fixed (byte* ptr19 = (byte[])span2[l])
						{
							dataDescriptor[span[l]].Ptr = (ulong)ptr19;
						}
					}
				}
				writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
				for (int m = 0; m < i; m++)
				{
					array[m].Free();
				}
			}
		}
		if (writeEventErrorCode != 0)
		{
			SetLastError(writeEventErrorCode);
			return false;
		}
		return true;
	}

	protected internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* childActivityID, int dataCount, IntPtr data)
	{
		_ = 0u;
		WriteEventErrorCode writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, childActivityID, dataCount, (EventData*)(void*)data);
		if (writeEventErrorCode != 0)
		{
			SetLastError(writeEventErrorCode);
			return false;
		}
		return true;
	}

	internal unsafe bool WriteEventRaw(ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* relatedActivityID, int dataCount, IntPtr data)
	{
		WriteEventErrorCode writeEventErrorCode = m_eventProvider.EventWriteTransfer(m_regHandle, in eventDescriptor, eventHandle, activityID, relatedActivityID, dataCount, (EventData*)(void*)data);
		if (writeEventErrorCode != 0)
		{
			SetLastError(writeEventErrorCode);
			return false;
		}
		return true;
	}

	private unsafe uint EventRegister(EventSource eventSource, Interop.Advapi32.EtwEnableCallback enableCallback)
	{
		m_providerName = eventSource.Name;
		m_providerId = eventSource.Guid;
		m_etwCallback = enableCallback;
		return m_eventProvider.EventRegister(eventSource, enableCallback, null, ref m_regHandle);
	}

	private void EventUnregister(long registrationHandle)
	{
		m_eventProvider.EventUnregister(registrationHandle);
	}

	internal unsafe int SetInformation(Interop.Advapi32.EVENT_INFO_CLASS eventInfoClass, void* data, uint dataSize)
	{
		int result = 50;
		if (!m_setInformationMissing)
		{
			try
			{
				result = Interop.Advapi32.EventSetInformation(m_regHandle, eventInfoClass, data, dataSize);
			}
			catch (TypeLoadException)
			{
				m_setInformationMissing = true;
			}
		}
		return result;
	}
}
