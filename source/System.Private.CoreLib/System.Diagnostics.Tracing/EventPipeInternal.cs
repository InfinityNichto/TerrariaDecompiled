using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal static class EventPipeInternal
{
	private struct EventPipeProviderConfigurationNative
	{
		private unsafe char* m_pProviderName;

		private ulong m_keywords;

		private uint m_loggingLevel;

		private unsafe char* m_pFilterData;

		internal unsafe static void MarshalToNative(EventPipeProviderConfiguration managed, ref EventPipeProviderConfigurationNative native)
		{
			native.m_pProviderName = (char*)(void*)Marshal.StringToCoTaskMemUni(managed.ProviderName);
			native.m_keywords = managed.Keywords;
			native.m_loggingLevel = managed.LoggingLevel;
			native.m_pFilterData = (char*)(void*)Marshal.StringToCoTaskMemUni(managed.FilterData);
		}

		internal unsafe void Release()
		{
			if (m_pProviderName != null)
			{
				Marshal.FreeCoTaskMem((IntPtr)m_pProviderName);
			}
			if (m_pFilterData != null)
			{
				Marshal.FreeCoTaskMem((IntPtr)m_pFilterData);
			}
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern ulong Enable(char* outputFile, EventPipeSerializationFormat format, uint circularBufferSizeInMB, EventPipeProviderConfigurationNative* providers, uint numProviders);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void Disable(ulong sessionID);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr CreateProvider(string providerName, Interop.Advapi32.EtwEnableCallback callbackFunc);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal unsafe static extern IntPtr DefineEvent(IntPtr provHandle, uint eventID, long keywords, uint eventVersion, uint level, void* pMetadata, uint metadataLength);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr GetProvider(string providerName);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void DeleteProvider(IntPtr provHandle);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int EventActivityIdControl(uint controlCode, ref Guid activityId);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal unsafe static extern void WriteEventData(IntPtr eventHandle, EventProvider.EventData* pEventData, uint dataCount, Guid* activityId, Guid* relatedActivityId);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal unsafe static extern bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal unsafe static extern bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr GetWaitHandle(ulong sessionID);

	internal unsafe static ulong Enable(string outputFile, EventPipeSerializationFormat format, uint circularBufferSizeInMB, EventPipeProviderConfiguration[] providers)
	{
		Span<EventPipeProviderConfigurationNative> span = new Span<EventPipeProviderConfigurationNative>((void*)Marshal.AllocCoTaskMem(sizeof(EventPipeProviderConfigurationNative) * providers.Length), providers.Length);
		span.Clear();
		try
		{
			for (int i = 0; i < providers.Length; i++)
			{
				EventPipeProviderConfigurationNative.MarshalToNative(providers[i], ref span[i]);
			}
			fixed (char* outputFile2 = outputFile)
			{
				fixed (EventPipeProviderConfigurationNative* providers2 = span)
				{
					return Enable(outputFile2, format, circularBufferSizeInMB, providers2, (uint)span.Length);
				}
			}
		}
		finally
		{
			for (int j = 0; j < providers.Length; j++)
			{
				span[j].Release();
			}
			fixed (EventPipeProviderConfigurationNative* ptr = span)
			{
				Marshal.FreeCoTaskMem((IntPtr)ptr);
			}
		}
	}
}
