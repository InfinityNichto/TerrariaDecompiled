using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.Qil;

internal class QilReference : QilNode
{
	private string _debugName;

	public string DebugName
	{
		get
		{
			return _debugName;
		}
		[param: DisallowNull]
		set
		{
			if (value.Length > 1000)
			{
				value = value.Substring(0, 1000);
			}
			_debugName = value;
		}
	}

	public QilReference(QilNodeType nodeType)
		: base(nodeType)
	{
	}
}
