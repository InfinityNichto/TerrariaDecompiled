using System.Collections;

namespace System.Xml.Xsl.XsltOld;

internal sealed class HtmlElementProps
{
	private bool _empty;

	private bool _abrParent;

	private bool _uriParent;

	private bool _noEntities;

	private bool _blockWS;

	private bool _head;

	private bool _nameParent;

	private static readonly Hashtable s_table = CreatePropsTable();

	public bool Empty => _empty;

	public bool AbrParent => _abrParent;

	public bool UriParent => _uriParent;

	public bool NoEntities => _noEntities;

	public bool Head => _head;

	public bool NameParent => _nameParent;

	public static HtmlElementProps Create(bool empty, bool abrParent, bool uriParent, bool noEntities, bool blockWS, bool head, bool nameParent)
	{
		HtmlElementProps htmlElementProps = new HtmlElementProps();
		htmlElementProps._empty = empty;
		htmlElementProps._abrParent = abrParent;
		htmlElementProps._uriParent = uriParent;
		htmlElementProps._noEntities = noEntities;
		htmlElementProps._blockWS = blockWS;
		htmlElementProps._head = head;
		htmlElementProps._nameParent = nameParent;
		return htmlElementProps;
	}

	public static HtmlElementProps GetProps(string name)
	{
		return (HtmlElementProps)s_table[name];
	}

	private static Hashtable CreatePropsTable()
	{
		bool flag = false;
		bool flag2 = true;
		Hashtable hashtable = new Hashtable(71, StringComparer.OrdinalIgnoreCase);
		hashtable.Add("a", Create(flag, flag, flag2, flag, flag, flag, flag2));
		hashtable.Add("address", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("applet", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("area", Create(flag2, flag2, flag2, flag, flag2, flag, flag));
		hashtable.Add("base", Create(flag2, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("basefont", Create(flag2, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("blockquote", Create(flag, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("body", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("br", Create(flag2, flag, flag, flag, flag, flag, flag));
		hashtable.Add("button", Create(flag, flag2, flag, flag, flag, flag, flag));
		hashtable.Add("caption", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("center", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("col", Create(flag2, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("colgroup", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("dd", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("del", Create(flag, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("dir", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("div", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("dl", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("dt", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("fieldset", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("font", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("form", Create(flag, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("frame", Create(flag2, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("frameset", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("h1", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("h2", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("h3", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("h4", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("h5", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("h6", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("head", Create(flag, flag, flag2, flag, flag2, flag2, flag));
		hashtable.Add("hr", Create(flag2, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("html", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("iframe", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("img", Create(flag2, flag2, flag2, flag, flag, flag, flag));
		hashtable.Add("input", Create(flag2, flag2, flag2, flag, flag, flag, flag));
		hashtable.Add("ins", Create(flag, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("isindex", Create(flag2, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("legend", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("li", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("link", Create(flag2, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("map", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("menu", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("meta", Create(flag2, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("noframes", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("noscript", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("object", Create(flag, flag2, flag2, flag, flag, flag, flag));
		hashtable.Add("ol", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("optgroup", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("option", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("p", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("param", Create(flag2, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("pre", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("q", Create(flag, flag, flag2, flag, flag, flag, flag));
		hashtable.Add("s", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("script", Create(flag, flag2, flag2, flag2, flag, flag, flag));
		hashtable.Add("select", Create(flag, flag2, flag, flag, flag, flag, flag));
		hashtable.Add("strike", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("style", Create(flag, flag, flag, flag2, flag2, flag, flag));
		hashtable.Add("table", Create(flag, flag, flag2, flag, flag2, flag, flag));
		hashtable.Add("tbody", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("td", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("textarea", Create(flag, flag2, flag, flag, flag, flag, flag));
		hashtable.Add("tfoot", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("th", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("thead", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("title", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("tr", Create(flag, flag, flag, flag, flag2, flag, flag));
		hashtable.Add("ul", Create(flag, flag2, flag, flag, flag2, flag, flag));
		hashtable.Add("xmp", Create(flag, flag, flag, flag, flag, flag, flag));
		return hashtable;
	}
}
