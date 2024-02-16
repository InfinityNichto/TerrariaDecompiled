using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class Keys : KeyedCollection<QilName, List<Key>>
{
	protected override QilName GetKeyForItem(List<Key> list)
	{
		return list[0].Name;
	}
}
