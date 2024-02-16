using System.Xml.XPath;

namespace System.Xml;

internal static class ValidateNames
{
	internal enum Flags
	{
		NCNames = 1,
		CheckLocalName = 2,
		CheckPrefixMapping = 4,
		All = 7,
		AllExceptNCNames = 6,
		AllExceptPrefixMapping = 3
	}

	internal static int ParseNmtoken(string s, int offset)
	{
		int i;
		for (i = offset; i < s.Length && XmlCharType.IsNCNameSingleChar(s[i]); i++)
		{
		}
		return i - offset;
	}

	internal static int ParseNmtokenNoNamespaces(string s, int offset)
	{
		int i;
		for (i = offset; i < s.Length && (XmlCharType.IsNameSingleChar(s[i]) || s[i] == ':'); i++)
		{
		}
		return i - offset;
	}

	internal static bool IsNmtokenNoNamespaces(string s)
	{
		int num = ParseNmtokenNoNamespaces(s, 0);
		if (num > 0)
		{
			return num == s.Length;
		}
		return false;
	}

	internal static int ParseNameNoNamespaces(string s, int offset)
	{
		int i = offset;
		if (i < s.Length)
		{
			if (!XmlCharType.IsStartNCNameSingleChar(s[i]) && s[i] != ':')
			{
				return 0;
			}
			for (i++; i < s.Length && (XmlCharType.IsNCNameSingleChar(s[i]) || s[i] == ':'); i++)
			{
			}
		}
		return i - offset;
	}

	internal static bool IsNameNoNamespaces(string s)
	{
		int num = ParseNameNoNamespaces(s, 0);
		if (num > 0)
		{
			return num == s.Length;
		}
		return false;
	}

	internal static int ParseNCName(string s, int offset)
	{
		int i = offset;
		if (i < s.Length)
		{
			if (!XmlCharType.IsStartNCNameSingleChar(s[i]))
			{
				return 0;
			}
			for (i++; i < s.Length && XmlCharType.IsNCNameSingleChar(s[i]); i++)
			{
			}
		}
		return i - offset;
	}

	internal static int ParseNCName(string s)
	{
		return ParseNCName(s, 0);
	}

	internal static string ParseNCNameThrow(string s)
	{
		ParseNCNameInternal(s, throwOnError: true);
		return s;
	}

	private static bool ParseNCNameInternal(string s, bool throwOnError)
	{
		int num = ParseNCName(s, 0);
		if (num == 0 || num != s.Length)
		{
			if (throwOnError)
			{
				ThrowInvalidName(s, 0, num);
			}
			return false;
		}
		return true;
	}

	internal static int ParseQName(string s, int offset, out int colonOffset)
	{
		colonOffset = 0;
		int num = ParseNCName(s, offset);
		if (num != 0)
		{
			offset += num;
			if (offset < s.Length && s[offset] == ':')
			{
				int num2 = ParseNCName(s, offset + 1);
				if (num2 != 0)
				{
					colonOffset = offset;
					num += num2 + 1;
				}
			}
		}
		return num;
	}

	internal static int ParseQNameThrow(string s)
	{
		int colonOffset;
		int num = ParseQName(s, 0, out colonOffset);
		if (num == 0 || num != s.Length)
		{
			ThrowInvalidName(s, 0, num);
		}
		return colonOffset;
	}

	internal static void ParseQNameThrow(string s, out string prefix, out string localName)
	{
		int num = ParseQNameThrow(s);
		if (num != 0)
		{
			prefix = s.Substring(0, num);
			localName = s.Substring(num + 1);
		}
		else
		{
			prefix = "";
			localName = s;
		}
	}

	internal static void ParseNameTestThrow(string s, out string prefix, out string localName)
	{
		int num;
		if (s.Length != 0 && s[0] == '*')
		{
			prefix = (localName = null);
			num = 1;
		}
		else
		{
			num = ParseNCName(s, 0);
			if (num != 0)
			{
				localName = s.Substring(0, num);
				if (num < s.Length && s[num] == ':')
				{
					prefix = localName;
					int num2 = num + 1;
					if (num2 < s.Length && s[num2] == '*')
					{
						localName = null;
						num += 2;
					}
					else
					{
						int num3 = ParseNCName(s, num2);
						if (num3 != 0)
						{
							localName = s.Substring(num2, num3);
							num += num3 + 1;
						}
					}
				}
				else
				{
					prefix = string.Empty;
				}
			}
			else
			{
				prefix = (localName = null);
			}
		}
		if (num == 0 || num != s.Length)
		{
			ThrowInvalidName(s, 0, num);
		}
	}

	internal static void ThrowInvalidName(string s, int offsetStartChar, int offsetBadChar)
	{
		if (offsetStartChar >= s.Length)
		{
			throw new XmlException(System.SR.Format(System.SR.Xml_EmptyName, string.Empty));
		}
		if (XmlCharType.IsNCNameSingleChar(s[offsetBadChar]) && !XmlCharType.IsStartNCNameSingleChar(s[offsetBadChar]))
		{
			throw new XmlException(System.SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(s, offsetBadChar));
		}
		throw new XmlException(System.SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(s, offsetBadChar));
	}

	internal static Exception GetInvalidNameException(string s, int offsetStartChar, int offsetBadChar)
	{
		if (offsetStartChar >= s.Length)
		{
			return new XmlException(System.SR.Xml_EmptyName, string.Empty);
		}
		if (XmlCharType.IsNCNameSingleChar(s[offsetBadChar]) && !XmlCharType.IsStartNCNameSingleChar(s[offsetBadChar]))
		{
			return new XmlException(System.SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(s, offsetBadChar));
		}
		return new XmlException(System.SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(s, offsetBadChar));
	}

	internal static bool StartsWithXml(string s)
	{
		if (s.Length < 3)
		{
			return false;
		}
		if (s[0] != 'x' && s[0] != 'X')
		{
			return false;
		}
		if (s[1] != 'm' && s[1] != 'M')
		{
			return false;
		}
		if (s[2] != 'l' && s[2] != 'L')
		{
			return false;
		}
		return true;
	}

	internal static bool IsReservedNamespace(string s)
	{
		if (!s.Equals("http://www.w3.org/XML/1998/namespace"))
		{
			return s.Equals("http://www.w3.org/2000/xmlns/");
		}
		return true;
	}

	internal static void ValidateNameThrow(string prefix, string localName, string ns, XPathNodeType nodeKind, Flags flags)
	{
		ValidateNameInternal(prefix, localName, ns, nodeKind, flags, throwOnError: true);
	}

	internal static bool ValidateName(string prefix, string localName, string ns, XPathNodeType nodeKind, Flags flags)
	{
		return ValidateNameInternal(prefix, localName, ns, nodeKind, flags, throwOnError: false);
	}

	private static bool ValidateNameInternal(string prefix, string localName, string ns, XPathNodeType nodeKind, Flags flags, bool throwOnError)
	{
		if ((flags & Flags.NCNames) != 0)
		{
			if (prefix.Length != 0 && !ParseNCNameInternal(prefix, throwOnError))
			{
				return false;
			}
			if (localName.Length != 0 && !ParseNCNameInternal(localName, throwOnError))
			{
				return false;
			}
		}
		if ((flags & Flags.CheckLocalName) != 0)
		{
			if (nodeKind != XPathNodeType.Element)
			{
				if (nodeKind != XPathNodeType.Attribute)
				{
					if (nodeKind == XPathNodeType.ProcessingInstruction)
					{
						if (localName.Length == 0 || (localName.Length == 3 && StartsWithXml(localName)))
						{
							if (throwOnError)
							{
								throw new XmlException(System.SR.Xml_InvalidPIName, localName);
							}
							return false;
						}
					}
					else if (localName.Length != 0)
					{
						if (throwOnError)
						{
							throw new XmlException(System.SR.XmlNoNameAllowed, nodeKind.ToString());
						}
						return false;
					}
					goto IL_00fa;
				}
				if (ns.Length == 0 && localName.Equals("xmlns"))
				{
					if (throwOnError)
					{
						throw new XmlException(System.SR.XmlBadName, new string[2]
						{
							nodeKind.ToString(),
							localName
						});
					}
					return false;
				}
			}
			if (localName.Length == 0)
			{
				if (throwOnError)
				{
					throw new XmlException(System.SR.Xdom_Empty_LocalName, string.Empty);
				}
				return false;
			}
		}
		goto IL_00fa;
		IL_00fa:
		if ((flags & Flags.CheckPrefixMapping) != 0)
		{
			switch (nodeKind)
			{
			case XPathNodeType.Element:
			case XPathNodeType.Attribute:
			case XPathNodeType.Namespace:
				if (ns.Length == 0)
				{
					if (prefix.Length != 0)
					{
						if (throwOnError)
						{
							throw new XmlException(System.SR.Xml_PrefixForEmptyNs, string.Empty);
						}
						return false;
					}
					break;
				}
				if (prefix.Length == 0 && nodeKind == XPathNodeType.Attribute)
				{
					if (throwOnError)
					{
						throw new XmlException(System.SR.XmlBadName, new string[2]
						{
							nodeKind.ToString(),
							localName
						});
					}
					return false;
				}
				if (prefix.Equals("xml"))
				{
					if (!ns.Equals("http://www.w3.org/XML/1998/namespace"))
					{
						if (throwOnError)
						{
							throw new XmlException(System.SR.Xml_XmlPrefix, string.Empty);
						}
						return false;
					}
					break;
				}
				if (prefix.Equals("xmlns"))
				{
					if (throwOnError)
					{
						throw new XmlException(System.SR.Xml_XmlnsPrefix, string.Empty);
					}
					return false;
				}
				if (IsReservedNamespace(ns))
				{
					if (throwOnError)
					{
						throw new XmlException(System.SR.Xml_NamespaceDeclXmlXmlns, string.Empty);
					}
					return false;
				}
				break;
			case XPathNodeType.ProcessingInstruction:
				if (prefix.Length != 0 || ns.Length != 0)
				{
					if (throwOnError)
					{
						throw new XmlException(System.SR.Xml_InvalidPIName, CreateName(prefix, localName));
					}
					return false;
				}
				break;
			default:
				if (prefix.Length != 0 || ns.Length != 0)
				{
					if (throwOnError)
					{
						throw new XmlException(System.SR.XmlNoNameAllowed, nodeKind.ToString());
					}
					return false;
				}
				break;
			}
		}
		return true;
	}

	private static string CreateName(string prefix, string localName)
	{
		if (prefix.Length == 0)
		{
			return localName;
		}
		return prefix + ":" + localName;
	}

	internal static void SplitQName(string name, out string prefix, out string lname)
	{
		int num = name.IndexOf(':');
		if (-1 == num)
		{
			prefix = string.Empty;
			lname = name;
			return;
		}
		if (num == 0 || name.Length - 1 == num)
		{
			string xml_BadNameChar = System.SR.Xml_BadNameChar;
			object[] args = XmlException.BuildCharExceptionArgs(':', '\0');
			throw new ArgumentException(System.SR.Format(xml_BadNameChar, args), "name");
		}
		prefix = name.Substring(0, num);
		num++;
		lname = name.Substring(num, name.Length - num);
	}
}
