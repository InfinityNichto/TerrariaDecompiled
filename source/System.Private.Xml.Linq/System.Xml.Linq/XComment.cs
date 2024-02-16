using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XComment : XNode
{
	internal string value;

	public override XmlNodeType NodeType => XmlNodeType.Comment;

	public string Value
	{
		get
		{
			return value;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			this.value = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public XComment(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		this.value = value;
	}

	public XComment(XComment other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		value = other.value;
	}

	internal XComment(XmlReader r)
	{
		value = r.Value;
		r.Read();
	}

	public override void WriteTo(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.WriteComment(value);
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return writer.WriteCommentAsync(value);
	}

	internal override XNode CloneNode()
	{
		return new XComment(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node is XComment xComment)
		{
			return value == xComment.value;
		}
		return false;
	}

	internal override int GetDeepHashCode()
	{
		return value.GetHashCode();
	}
}
