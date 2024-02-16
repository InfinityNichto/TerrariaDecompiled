using System.Collections;

namespace System.Xml.Xsl.Qil;

internal sealed class SubstitutionList
{
	private readonly ArrayList _s;

	public SubstitutionList()
	{
		_s = new ArrayList(4);
	}

	public void AddSubstitutionPair(QilNode find, QilNode replace)
	{
		_s.Add(find);
		_s.Add(replace);
	}

	public void RemoveLastSubstitutionPair()
	{
		_s.RemoveRange(_s.Count - 2, 2);
	}

	public QilNode FindReplacement(QilNode n)
	{
		for (int num = _s.Count - 2; num >= 0; num -= 2)
		{
			if (_s[num] == n)
			{
				return (QilNode)_s[num + 1];
			}
		}
		return null;
	}
}
