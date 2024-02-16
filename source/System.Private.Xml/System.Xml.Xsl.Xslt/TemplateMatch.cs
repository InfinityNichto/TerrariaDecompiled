using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class TemplateMatch
{
	internal sealed class TemplateMatchComparer : IComparer<TemplateMatch>
	{
		public int Compare(TemplateMatch x, TemplateMatch y)
		{
			if (!(x._priority > y._priority))
			{
				if (!(x._priority < y._priority))
				{
					return x._template.OrderNumber - y._template.OrderNumber;
				}
				return -1;
			}
			return 1;
		}
	}

	public static readonly TemplateMatchComparer Comparer = new TemplateMatchComparer();

	private readonly Template _template;

	private readonly double _priority;

	private XmlNodeKindFlags _nodeKind;

	private QilName _qname;

	private readonly QilIterator _iterator;

	private QilNode _condition;

	public XmlNodeKindFlags NodeKind => _nodeKind;

	public QilName QName => _qname;

	public QilIterator Iterator => _iterator;

	public QilNode Condition => _condition;

	public QilFunction TemplateFunction => _template.Function;

	public TemplateMatch(Template template, QilLoop filter)
	{
		_template = template;
		_priority = (double.IsNaN(template.Priority) ? XPathPatternBuilder.GetPriority(filter) : template.Priority);
		_iterator = filter.Variable;
		_condition = filter.Body;
		XPathPatternBuilder.CleanAnnotation(filter);
		NipOffTypeNameCheck();
	}

	private void NipOffTypeNameCheck()
	{
		QilBinary[] array = new QilBinary[4];
		int num = -1;
		QilNode qilNode = _condition;
		_nodeKind = XmlNodeKindFlags.None;
		_qname = null;
		while (qilNode.NodeType == QilNodeType.And)
		{
			qilNode = (array[++num & 3] = (QilBinary)qilNode).Left;
		}
		if (qilNode.NodeType != QilNodeType.IsType)
		{
			return;
		}
		QilBinary qilBinary = (QilBinary)qilNode;
		if (qilBinary.Left != _iterator || qilBinary.Right.NodeType != QilNodeType.LiteralType)
		{
			return;
		}
		XmlNodeKindFlags nodeKinds = qilBinary.Right.XmlType.NodeKinds;
		if (!Bits.ExactlyOne((uint)nodeKinds))
		{
			return;
		}
		QilNode qilNode2 = qilBinary;
		_nodeKind = nodeKinds;
		QilBinary qilBinary2 = array[num & 3];
		if (qilBinary2 != null && qilBinary2.Right.NodeType == QilNodeType.Eq)
		{
			QilBinary qilBinary3 = (QilBinary)qilBinary2.Right;
			if (qilBinary3.Left.NodeType == QilNodeType.NameOf && ((QilUnary)qilBinary3.Left).Child == _iterator && qilBinary3.Right.NodeType == QilNodeType.LiteralQName)
			{
				qilNode2 = qilBinary2;
				_qname = (QilName)((QilLiteral)qilBinary3.Right).Value;
				num--;
			}
		}
		QilBinary qilBinary4 = array[num & 3];
		QilBinary qilBinary5 = array[--num & 3];
		if (qilBinary5 != null)
		{
			qilBinary5.Left = qilBinary4.Right;
		}
		else if (qilBinary4 != null)
		{
			_condition = qilBinary4.Right;
		}
		else
		{
			_condition = null;
		}
	}
}
