namespace System.Xml.Xsl.Qil;

internal class QilIterator : QilReference
{
	private QilNode _binding;

	public override int Count => 1;

	public override QilNode this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _binding;
		}
		set
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			_binding = value;
		}
	}

	public QilNode Binding
	{
		get
		{
			return _binding;
		}
		set
		{
			_binding = value;
		}
	}

	public QilIterator(QilNodeType nodeType, QilNode binding)
		: base(nodeType)
	{
		Binding = binding;
	}
}
