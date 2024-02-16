using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Security;

public sealed class SecurityElement
{
	internal string _tag;

	internal string _text;

	private ArrayList _children;

	internal ArrayList _attributes;

	private static readonly char[] s_tagIllegalCharacters = new char[3] { ' ', '<', '>' };

	private static readonly char[] s_textIllegalCharacters = new char[2] { '<', '>' };

	private static readonly char[] s_valueIllegalCharacters = new char[3] { '<', '>', '"' };

	private static readonly char[] s_escapeChars = new char[5] { '<', '>', '"', '\'', '&' };

	private static readonly string[] s_escapeStringPairs = new string[10] { "<", "&lt;", ">", "&gt;", "\"", "&quot;", "'", "&apos;", "&", "&amp;" };

	public string Tag
	{
		get
		{
			return _tag;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("Tag");
			}
			if (!IsValidTag(value))
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidElementTag, value));
			}
			_tag = value;
		}
	}

	public Hashtable? Attributes
	{
		get
		{
			if (_attributes == null || _attributes.Count == 0)
			{
				return null;
			}
			Hashtable hashtable = new Hashtable(_attributes.Count / 2);
			int count = _attributes.Count;
			for (int i = 0; i < count; i += 2)
			{
				hashtable.Add(_attributes[i], _attributes[i + 1]);
			}
			return hashtable;
		}
		set
		{
			if (value == null || value.Count == 0)
			{
				_attributes = null;
				return;
			}
			ArrayList arrayList = new ArrayList(value.Count);
			IDictionaryEnumerator enumerator = value.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string text = (string)enumerator.Key;
				string text2 = (string)enumerator.Value;
				if (!IsValidAttributeName(text))
				{
					throw new ArgumentException(SR.Format(SR.Argument_InvalidElementName, text));
				}
				if (!IsValidAttributeValue(text2))
				{
					throw new ArgumentException(SR.Format(SR.Argument_InvalidElementValue, text2));
				}
				arrayList.Add(text);
				arrayList.Add(text2);
			}
			_attributes = arrayList;
		}
	}

	public string? Text
	{
		get
		{
			return Unescape(_text);
		}
		set
		{
			if (value == null)
			{
				_text = null;
				return;
			}
			if (!IsValidText(value))
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidElementTag, value));
			}
			_text = value;
		}
	}

	public ArrayList? Children
	{
		get
		{
			return _children;
		}
		set
		{
			if (value != null && value.Contains(null))
			{
				throw new ArgumentException(SR.ArgumentNull_Child);
			}
			_children = value;
		}
	}

	public SecurityElement(string tag)
	{
		if (tag == null)
		{
			throw new ArgumentNullException("tag");
		}
		if (!IsValidTag(tag))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidElementTag, tag));
		}
		_tag = tag;
	}

	public SecurityElement(string tag, string? text)
	{
		if (tag == null)
		{
			throw new ArgumentNullException("tag");
		}
		if (!IsValidTag(tag))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidElementTag, tag));
		}
		if (text != null && !IsValidText(text))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidElementText, text));
		}
		_tag = tag;
		_text = text;
	}

	internal void AddAttributeSafe(string name, string value)
	{
		if (_attributes == null)
		{
			_attributes = new ArrayList(8);
		}
		else
		{
			int count = _attributes.Count;
			for (int i = 0; i < count; i += 2)
			{
				string a = (string)_attributes[i];
				if (string.Equals(a, name))
				{
					throw new ArgumentException(SR.Argument_AttributeNamesMustBeUnique);
				}
			}
		}
		_attributes.Add(name);
		_attributes.Add(value);
	}

	public void AddAttribute(string name, string value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (!IsValidAttributeName(name))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidElementName, name));
		}
		if (!IsValidAttributeValue(value))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidElementValue, value));
		}
		AddAttributeSafe(name, value);
	}

	public void AddChild(SecurityElement child)
	{
		if (child == null)
		{
			throw new ArgumentNullException("child");
		}
		if (_children == null)
		{
			_children = new ArrayList(1);
		}
		_children.Add(child);
	}

	public bool Equal([NotNullWhen(true)] SecurityElement? other)
	{
		if (other == null)
		{
			return false;
		}
		if (!string.Equals(_tag, other._tag))
		{
			return false;
		}
		if (!string.Equals(_text, other._text))
		{
			return false;
		}
		if (_attributes == null || other._attributes == null)
		{
			if (_attributes != other._attributes)
			{
				return false;
			}
		}
		else
		{
			int count = _attributes.Count;
			if (count != other._attributes.Count)
			{
				return false;
			}
			for (int i = 0; i < count; i++)
			{
				string a = (string)_attributes[i];
				string b = (string)other._attributes[i];
				if (!string.Equals(a, b))
				{
					return false;
				}
			}
		}
		if (_children == null || other._children == null)
		{
			if (_children != other._children)
			{
				return false;
			}
		}
		else
		{
			if (_children.Count != other._children.Count)
			{
				return false;
			}
			IEnumerator enumerator = _children.GetEnumerator();
			IEnumerator enumerator2 = other._children.GetEnumerator();
			while (enumerator.MoveNext())
			{
				enumerator2.MoveNext();
				SecurityElement securityElement = (SecurityElement)enumerator.Current;
				SecurityElement other2 = (SecurityElement)enumerator2.Current;
				if (securityElement == null || !securityElement.Equal(other2))
				{
					return false;
				}
			}
		}
		return true;
	}

	public SecurityElement Copy()
	{
		SecurityElement securityElement = new SecurityElement(_tag, _text);
		securityElement._children = ((_children == null) ? null : new ArrayList(_children));
		securityElement._attributes = ((_attributes == null) ? null : new ArrayList(_attributes));
		return securityElement;
	}

	public static bool IsValidTag([NotNullWhen(true)] string? tag)
	{
		if (tag == null)
		{
			return false;
		}
		return tag.IndexOfAny(s_tagIllegalCharacters) == -1;
	}

	public static bool IsValidText([NotNullWhen(true)] string? text)
	{
		if (text == null)
		{
			return false;
		}
		return text.IndexOfAny(s_textIllegalCharacters) == -1;
	}

	public static bool IsValidAttributeName([NotNullWhen(true)] string? name)
	{
		return IsValidTag(name);
	}

	public static bool IsValidAttributeValue([NotNullWhen(true)] string? value)
	{
		if (value == null)
		{
			return false;
		}
		return value.IndexOfAny(s_valueIllegalCharacters) == -1;
	}

	private static string GetEscapeSequence(char c)
	{
		int num = s_escapeStringPairs.Length;
		for (int i = 0; i < num; i += 2)
		{
			string text = s_escapeStringPairs[i];
			string result = s_escapeStringPairs[i + 1];
			if (text[0] == c)
			{
				return result;
			}
		}
		return c.ToString();
	}

	[return: NotNullIfNotNull("str")]
	public static string? Escape(string? str)
	{
		if (str == null)
		{
			return null;
		}
		StringBuilder stringBuilder = null;
		int length = str.Length;
		int num = 0;
		while (true)
		{
			int num2 = str.IndexOfAny(s_escapeChars, num);
			if (num2 == -1)
			{
				break;
			}
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
			}
			stringBuilder.Append(str, num, num2 - num);
			stringBuilder.Append(GetEscapeSequence(str[num2]));
			num = num2 + 1;
		}
		if (stringBuilder == null)
		{
			return str;
		}
		stringBuilder.Append(str, num, length - num);
		return stringBuilder.ToString();
	}

	private static string GetUnescapeSequence(string str, int index, out int newIndex)
	{
		int num = str.Length - index;
		int num2 = s_escapeStringPairs.Length;
		for (int i = 0; i < num2; i += 2)
		{
			string result = s_escapeStringPairs[i];
			string text = s_escapeStringPairs[i + 1];
			int length = text.Length;
			if (length <= num && string.Compare(text, 0, str, index, length, StringComparison.Ordinal) == 0)
			{
				newIndex = index + text.Length;
				return result;
			}
		}
		newIndex = index + 1;
		return str[index].ToString();
	}

	private static string Unescape(string str)
	{
		if (str == null)
		{
			return null;
		}
		StringBuilder stringBuilder = null;
		int length = str.Length;
		int newIndex = 0;
		while (true)
		{
			int num = str.IndexOf('&', newIndex);
			if (num == -1)
			{
				break;
			}
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
			}
			stringBuilder.Append(str, newIndex, num - newIndex);
			stringBuilder.Append(GetUnescapeSequence(str, num, out newIndex));
		}
		if (stringBuilder == null)
		{
			return str;
		}
		stringBuilder.Append(str, newIndex, length - newIndex);
		return stringBuilder.ToString();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		ToString(stringBuilder, delegate(object obj, string str)
		{
			((StringBuilder)obj).Append(str);
		});
		return stringBuilder.ToString();
	}

	private void ToString(object obj, Action<object, string> write)
	{
		write(obj, "<");
		write(obj, _tag);
		if (_attributes != null && _attributes.Count > 0)
		{
			write(obj, " ");
			int count = _attributes.Count;
			for (int i = 0; i < count; i += 2)
			{
				string arg = (string)_attributes[i];
				string arg2 = (string)_attributes[i + 1];
				write(obj, arg);
				write(obj, "=\"");
				write(obj, arg2);
				write(obj, "\"");
				if (i != _attributes.Count - 2)
				{
					write(obj, "\r\n");
				}
			}
		}
		if (_text == null && (_children == null || _children.Count == 0))
		{
			write(obj, "/>");
			write(obj, "\r\n");
			return;
		}
		write(obj, ">");
		write(obj, _text);
		if (_children != null)
		{
			write(obj, "\r\n");
			for (int j = 0; j < _children.Count; j++)
			{
				((SecurityElement)_children[j]).ToString(obj, write);
			}
		}
		write(obj, "</");
		write(obj, _tag);
		write(obj, ">");
		write(obj, "\r\n");
	}

	public string? Attribute(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_attributes == null)
		{
			return null;
		}
		int count = _attributes.Count;
		for (int i = 0; i < count; i += 2)
		{
			string a = (string)_attributes[i];
			if (string.Equals(a, name))
			{
				string str = (string)_attributes[i + 1];
				return Unescape(str);
			}
		}
		return null;
	}

	public SecurityElement? SearchForChildByTag(string tag)
	{
		if (tag == null)
		{
			throw new ArgumentNullException("tag");
		}
		if (_children == null)
		{
			return null;
		}
		foreach (SecurityElement child in _children)
		{
			if (child != null && string.Equals(child.Tag, tag))
			{
				return child;
			}
		}
		return null;
	}

	public string? SearchForTextOfTag(string tag)
	{
		if (tag == null)
		{
			throw new ArgumentNullException("tag");
		}
		if (string.Equals(_tag, tag))
		{
			return Unescape(_text);
		}
		if (_children == null)
		{
			return null;
		}
		foreach (SecurityElement child in Children)
		{
			string text = child?.SearchForTextOfTag(tag);
			if (text != null)
			{
				return text;
			}
		}
		return null;
	}

	public static SecurityElement? FromString(string xml)
	{
		if (xml == null)
		{
			throw new ArgumentNullException("xml");
		}
		return null;
	}
}
