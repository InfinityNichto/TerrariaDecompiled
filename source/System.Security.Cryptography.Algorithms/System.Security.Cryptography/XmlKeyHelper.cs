using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Text;

namespace System.Security.Cryptography;

internal static class XmlKeyHelper
{
	internal struct ParseState
	{
		private static class Functions
		{
			private static readonly Func<string, object> s_xDocumentCreate;

			private static readonly PropertyInfo s_docRootProperty;

			private static readonly MethodInfo s_getElementsMethod;

			private static readonly PropertyInfo s_elementNameProperty;

			private static readonly PropertyInfo s_elementValueProperty;

			private static readonly PropertyInfo s_nameNameProperty;

			static Functions()
			{
				Type type = Type.GetType("System.Xml.Linq.XDocument, System.Private.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
				s_xDocumentCreate = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[1] { typeof(string) }).CreateDelegate<Func<string, object>>();
				s_docRootProperty = type.GetProperty("Root");
				Type type2 = Type.GetType("System.Xml.Linq.XElement, System.Private.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
				s_getElementsMethod = type2.GetMethod("Elements", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
				s_elementNameProperty = type2.GetProperty("Name");
				s_elementValueProperty = type2.GetProperty("Value");
				Type type3 = Type.GetType("System.Xml.Linq.XName, System.Private.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
				s_nameNameProperty = type3.GetProperty("LocalName");
			}

			internal static object ParseDocument(string xmlString)
			{
				return s_docRootProperty.GetValue(s_xDocumentCreate(xmlString));
			}

			internal static IEnumerable GetElements(object element)
			{
				return (IEnumerable)s_getElementsMethod.Invoke(element, Array.Empty<object>());
			}

			internal static string GetLocalName(object element)
			{
				return (string)s_nameNameProperty.GetValue(s_elementNameProperty.GetValue(element));
			}

			internal static string GetValue(object element)
			{
				return (string)s_elementValueProperty.GetValue(element);
			}
		}

		private IEnumerable _enumerable;

		private IEnumerator _enumerator;

		private int _index;

		internal static ParseState ParseDocument(string xmlString)
		{
			object element = Functions.ParseDocument(xmlString);
			ParseState result = default(ParseState);
			result._enumerable = Functions.GetElements(element);
			result._enumerator = null;
			result._index = -1;
			return result;
		}

		internal bool HasElement(string localName)
		{
			string value = GetValue(localName);
			bool flag = value != null;
			if (flag)
			{
				_index--;
			}
			return flag;
		}

		internal string GetValue(string localName)
		{
			if (_enumerable == null)
			{
				return null;
			}
			if (_enumerator == null)
			{
				_enumerator = _enumerable.GetEnumerator();
			}
			int index = _index;
			int num = index;
			if (!_enumerator.MoveNext())
			{
				num = -1;
				_enumerator = _enumerable.GetEnumerator();
				if (!_enumerator.MoveNext())
				{
					_enumerable = null;
					return null;
				}
			}
			for (num++; num != index; num++)
			{
				string localName2 = Functions.GetLocalName(_enumerator.Current);
				if (localName == localName2)
				{
					_index = num;
					return Functions.GetValue(_enumerator.Current);
				}
				if (!_enumerator.MoveNext())
				{
					num = -1;
					if (index < 0)
					{
						_enumerator = null;
						return null;
					}
					_enumerator = _enumerable.GetEnumerator();
					if (!_enumerator.MoveNext())
					{
						_enumerable = null;
						return null;
					}
				}
			}
			return null;
		}
	}

	internal static ParseState ParseDocument(string xmlString)
	{
		if (xmlString == null)
		{
			throw new ArgumentNullException("xmlString");
		}
		try
		{
			return ParseState.ParseDocument(xmlString);
		}
		catch (Exception inner)
		{
			throw new CryptographicException(System.SR.Cryptography_FromXmlParseError, inner);
		}
	}

	internal static bool HasElement(ref ParseState state, string name)
	{
		return state.HasElement(name);
	}

	internal static byte[] ReadCryptoBinary(ref ParseState state, string name, int sizeHint = -1)
	{
		string value = state.GetValue(name);
		if (value == null)
		{
			return null;
		}
		if (value.Length == 0)
		{
			return Array.Empty<byte>();
		}
		if (sizeHint < 0)
		{
			return Convert.FromBase64String(value);
		}
		byte[] array = new byte[sizeHint];
		if (Convert.TryFromBase64Chars(value.AsSpan(), array, out var bytesWritten))
		{
			if (bytesWritten == sizeHint)
			{
				return array;
			}
			int num = sizeHint - bytesWritten;
			Buffer.BlockCopy(array, 0, array, num, bytesWritten);
			array.AsSpan(0, num).Clear();
			return array;
		}
		return Convert.FromBase64String(value);
	}

	internal static int ReadCryptoBinaryInt32(byte[] buf)
	{
		int num = 0;
		for (int i = Math.Max(0, buf.Length - 4); i < buf.Length; i++)
		{
			num <<= 8;
			num |= buf[i];
		}
		return num;
	}

	internal static void WriteCryptoBinary(string name, int value, StringBuilder builder)
	{
		if (value == 0)
		{
			Span<byte> span = stackalloc byte[1];
			span[0] = 0;
			WriteCryptoBinary(name, span, builder);
			return;
		}
		Span<byte> destination = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(destination, value);
		int i;
		for (i = 0; destination[i] == 0; i++)
		{
		}
		WriteCryptoBinary(name, destination.Slice(i, destination.Length - i), builder);
	}

	internal static void WriteCryptoBinary(string name, ReadOnlySpan<byte> value, StringBuilder builder)
	{
		builder.Append('<');
		builder.Append(name);
		builder.Append('>');
		int num = 0;
		int num2 = value.Length;
		Span<char> chars = stackalloc char[256];
		while (num2 > 0)
		{
			int num3 = Math.Min(192, num2);
			if (!Convert.TryToBase64Chars(value.Slice(num, num3), chars, out var charsWritten))
			{
				throw new CryptographicException();
			}
			builder.Append(chars.Slice(0, charsWritten));
			num2 -= num3;
			num += num3;
		}
		builder.Append('<');
		builder.Append('/');
		builder.Append(name);
		builder.Append('>');
	}
}
