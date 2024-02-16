using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class PatternBag
{
	public Dictionary<QilName, List<Pattern>> FixedNamePatterns = new Dictionary<QilName, List<Pattern>>();

	public List<QilName> FixedNamePatternsNames = new List<QilName>();

	public List<Pattern> NonFixedNamePatterns = new List<Pattern>();

	public void Clear()
	{
		FixedNamePatterns.Clear();
		FixedNamePatternsNames.Clear();
		NonFixedNamePatterns.Clear();
	}

	public void Add(Pattern pattern)
	{
		QilName qName = pattern.Match.QName;
		List<Pattern> value;
		if (qName == null)
		{
			value = NonFixedNamePatterns;
		}
		else if (!FixedNamePatterns.TryGetValue(qName, out value))
		{
			FixedNamePatternsNames.Add(qName);
			List<Pattern> list2 = (FixedNamePatterns[qName] = new List<Pattern>());
			value = list2;
		}
		value.Add(pattern);
	}
}
