using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal struct EventPipeProviderConfiguration
{
	[MarshalAs(UnmanagedType.LPWStr)]
	private readonly string m_providerName;

	private readonly ulong m_keywords;

	private readonly uint m_loggingLevel;

	[MarshalAs(UnmanagedType.LPWStr)]
	private readonly string m_filterData;

	internal string ProviderName => m_providerName;

	internal ulong Keywords => m_keywords;

	internal uint LoggingLevel => m_loggingLevel;

	internal string FilterData => m_filterData;

	internal EventPipeProviderConfiguration(string providerName, ulong keywords, uint loggingLevel, string filterData)
	{
		if (string.IsNullOrEmpty(providerName))
		{
			throw new ArgumentNullException("providerName");
		}
		if (loggingLevel > 5)
		{
			throw new ArgumentOutOfRangeException("loggingLevel");
		}
		m_providerName = providerName;
		m_keywords = keywords;
		m_loggingLevel = loggingLevel;
		m_filterData = filterData;
	}
}
