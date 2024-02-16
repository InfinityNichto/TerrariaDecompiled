namespace System.Reflection;

internal sealed class MetadataException : Exception
{
	private int m_hr;

	internal MetadataException(int hr)
	{
		m_hr = hr;
	}

	public override string ToString()
	{
		return $"MetadataException HResult = {m_hr:x}.";
	}
}
