using System.Collections;

namespace System.Xml.Xsl.XsltOld;

internal sealed class HtmlAttributeProps
{
	private bool _abr;

	private bool _uri;

	private bool _name;

	private static readonly Hashtable s_table = CreatePropsTable();

	public bool Abr => _abr;

	public bool Uri => _uri;

	public bool Name => _name;

	public static HtmlAttributeProps Create(bool abr, bool uri, bool name)
	{
		HtmlAttributeProps htmlAttributeProps = new HtmlAttributeProps();
		htmlAttributeProps._abr = abr;
		htmlAttributeProps._uri = uri;
		htmlAttributeProps._name = name;
		return htmlAttributeProps;
	}

	public static HtmlAttributeProps GetProps(string name)
	{
		return (HtmlAttributeProps)s_table[name];
	}

	private static Hashtable CreatePropsTable()
	{
		bool flag = false;
		bool flag2 = true;
		Hashtable hashtable = new Hashtable(26, StringComparer.OrdinalIgnoreCase);
		hashtable.Add("action", Create(flag, flag2, flag));
		hashtable.Add("checked", Create(flag2, flag, flag));
		hashtable.Add("cite", Create(flag, flag2, flag));
		hashtable.Add("classid", Create(flag, flag2, flag));
		hashtable.Add("codebase", Create(flag, flag2, flag));
		hashtable.Add("compact", Create(flag2, flag, flag));
		hashtable.Add("data", Create(flag, flag2, flag));
		hashtable.Add("datasrc", Create(flag, flag2, flag));
		hashtable.Add("declare", Create(flag2, flag, flag));
		hashtable.Add("defer", Create(flag2, flag, flag));
		hashtable.Add("disabled", Create(flag2, flag, flag));
		hashtable.Add("for", Create(flag, flag2, flag));
		hashtable.Add("href", Create(flag, flag2, flag));
		hashtable.Add("ismap", Create(flag2, flag, flag));
		hashtable.Add("longdesc", Create(flag, flag2, flag));
		hashtable.Add("multiple", Create(flag2, flag, flag));
		hashtable.Add("name", Create(flag, flag, flag2));
		hashtable.Add("nohref", Create(flag2, flag, flag));
		hashtable.Add("noresize", Create(flag2, flag, flag));
		hashtable.Add("noshade", Create(flag2, flag, flag));
		hashtable.Add("nowrap", Create(flag2, flag, flag));
		hashtable.Add("profile", Create(flag, flag2, flag));
		hashtable.Add("readonly", Create(flag2, flag, flag));
		hashtable.Add("selected", Create(flag2, flag, flag));
		hashtable.Add("src", Create(flag, flag2, flag));
		hashtable.Add("usemap", Create(flag, flag2, flag));
		return hashtable;
	}
}
