using System.Collections;

namespace System.Xml.Xsl.Xslt;

internal sealed class CompilerErrorCollection : CollectionBase
{
	public int Add(CompilerError value)
	{
		return base.List.Add(value);
	}

	public void AddRange(CompilerError[] value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		for (int i = 0; i < value.Length; i++)
		{
			Add(value[i]);
		}
	}

	public void CopyTo(CompilerError[] array, int index)
	{
		base.List.CopyTo(array, index);
	}
}
