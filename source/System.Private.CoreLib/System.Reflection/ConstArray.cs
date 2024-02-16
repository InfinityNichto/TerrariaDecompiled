namespace System.Reflection;

internal readonly struct ConstArray
{
	internal readonly int m_length;

	internal readonly IntPtr m_constArray;

	public IntPtr Signature => m_constArray;

	public int Length => m_length;

	public unsafe byte this[int index]
	{
		get
		{
			if (index < 0 || index >= m_length)
			{
				throw new IndexOutOfRangeException();
			}
			return ((byte*)m_constArray.ToPointer())[index];
		}
	}
}
