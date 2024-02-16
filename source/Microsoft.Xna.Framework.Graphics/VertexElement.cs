using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

public struct VertexElement
{
	internal int _offset;

	internal VertexElementFormat _format;

	internal VertexElementUsage _usage;

	internal int _usageIndex;

	public int Offset
	{
		get
		{
			return _offset;
		}
		set
		{
			_offset = value;
		}
	}

	public VertexElementFormat VertexElementFormat
	{
		get
		{
			return _format;
		}
		set
		{
			_format = value;
		}
	}

	public VertexElementUsage VertexElementUsage
	{
		get
		{
			return _usage;
		}
		set
		{
			_usage = value;
		}
	}

	public int UsageIndex
	{
		get
		{
			return _usageIndex;
		}
		set
		{
			_usageIndex = value;
		}
	}

	public VertexElement(int offset, VertexElementFormat elementFormat, VertexElementUsage elementUsage, int usageIndex)
	{
		_offset = offset;
		_usageIndex = usageIndex;
		_format = elementFormat;
		_usage = elementUsage;
	}

	public override int GetHashCode()
	{
		return Helpers.SmartGetHashCode(this);
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{Offset:{0} Format:{1} Usage:{2} UsageIndex:{3}}}", Offset, VertexElementFormat, VertexElementUsage, UsageIndex);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return this == (VertexElement)obj;
	}

	public static bool operator ==(VertexElement left, VertexElement right)
	{
		if (left._offset == right._offset && left._usageIndex == right._usageIndex && left._usage == right._usage)
		{
			return left._format == right._format;
		}
		return false;
	}

	public static bool operator !=(VertexElement left, VertexElement right)
	{
		return !(left == right);
	}
}
