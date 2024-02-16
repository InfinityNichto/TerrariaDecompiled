using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class Comparer : IComparer, ISerializable
{
	private readonly CompareInfo _compareInfo;

	public static readonly Comparer Default = new Comparer(CultureInfo.CurrentCulture);

	public static readonly Comparer DefaultInvariant = new Comparer(CultureInfo.InvariantCulture);

	public Comparer(CultureInfo culture)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		_compareInfo = culture.CompareInfo;
	}

	private Comparer(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		_compareInfo = (CompareInfo)info.GetValue("CompareInfo", typeof(CompareInfo));
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("CompareInfo", _compareInfo);
	}

	public int Compare(object? a, object? b)
	{
		if (a == b)
		{
			return 0;
		}
		if (a == null)
		{
			return -1;
		}
		if (b == null)
		{
			return 1;
		}
		if (a is string @string && b is string string2)
		{
			return _compareInfo.Compare(@string, string2);
		}
		if (a is IComparable comparable)
		{
			return comparable.CompareTo(b);
		}
		if (b is IComparable comparable2)
		{
			return -comparable2.CompareTo(a);
		}
		throw new ArgumentException(SR.Argument_ImplementIComparable);
	}
}
