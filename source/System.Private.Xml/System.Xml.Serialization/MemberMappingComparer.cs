using System.Collections.Generic;

namespace System.Xml.Serialization;

internal sealed class MemberMappingComparer : IComparer<MemberMapping>
{
	public int Compare(MemberMapping m1, MemberMapping m2)
	{
		if (m1.IsText)
		{
			if (m2.IsText)
			{
				return 0;
			}
			return 1;
		}
		if (m2.IsText)
		{
			return -1;
		}
		if (m1.SequenceId < 0 && m2.SequenceId < 0)
		{
			return 0;
		}
		if (m1.SequenceId < 0)
		{
			return 1;
		}
		if (m2.SequenceId < 0)
		{
			return -1;
		}
		if (m1.SequenceId < m2.SequenceId)
		{
			return -1;
		}
		if (m1.SequenceId > m2.SequenceId)
		{
			return 1;
		}
		return 0;
	}
}
