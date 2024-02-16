using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal static class XsltMethods
{
	public static readonly MethodInfo FormatMessage = typeof(XsltLibrary).GetMethod("FormatMessage");

	public static readonly MethodInfo EnsureNodeSet = typeof(XsltConvert).GetMethod("EnsureNodeSet", new Type[1] { typeof(IList<XPathItem>) });

	public static readonly MethodInfo EqualityOperator = typeof(XsltLibrary).GetMethod("EqualityOperator");

	public static readonly MethodInfo RelationalOperator = typeof(XsltLibrary).GetMethod("RelationalOperator");

	public static readonly MethodInfo StartsWith = typeof(XsltFunctions).GetMethod("StartsWith");

	public static readonly MethodInfo Contains = typeof(XsltFunctions).GetMethod("Contains");

	public static readonly MethodInfo SubstringBefore = typeof(XsltFunctions).GetMethod("SubstringBefore");

	public static readonly MethodInfo SubstringAfter = typeof(XsltFunctions).GetMethod("SubstringAfter");

	public static readonly MethodInfo Substring2 = typeof(XsltFunctions).GetMethod("Substring", new Type[2]
	{
		typeof(string),
		typeof(double)
	});

	public static readonly MethodInfo Substring3 = typeof(XsltFunctions).GetMethod("Substring", new Type[3]
	{
		typeof(string),
		typeof(double),
		typeof(double)
	});

	public static readonly MethodInfo NormalizeSpace = typeof(XsltFunctions).GetMethod("NormalizeSpace");

	public static readonly MethodInfo Translate = typeof(XsltFunctions).GetMethod("Translate");

	public static readonly MethodInfo Lang = typeof(XsltFunctions).GetMethod("Lang");

	public static readonly MethodInfo Floor = typeof(Math).GetMethod("Floor", new Type[1] { typeof(double) });

	public static readonly MethodInfo Ceiling = typeof(Math).GetMethod("Ceiling", new Type[1] { typeof(double) });

	public static readonly MethodInfo Round = typeof(XsltFunctions).GetMethod("Round");

	public static readonly MethodInfo SystemProperty = typeof(XsltFunctions).GetMethod("SystemProperty");

	public static readonly MethodInfo BaseUri = typeof(XsltFunctions).GetMethod("BaseUri");

	public static readonly MethodInfo OuterXml = typeof(XsltFunctions).GetMethod("OuterXml");

	public static readonly MethodInfo OnCurrentNodeChanged = typeof(XmlQueryRuntime).GetMethod("OnCurrentNodeChanged");

	public static readonly MethodInfo MSFormatDateTime = typeof(XsltFunctions).GetMethod("MSFormatDateTime");

	public static readonly MethodInfo MSStringCompare = typeof(XsltFunctions).GetMethod("MSStringCompare");

	public static readonly MethodInfo MSUtc = typeof(XsltFunctions).GetMethod("MSUtc");

	public static readonly MethodInfo MSNumber = typeof(XsltFunctions).GetMethod("MSNumber");

	public static readonly MethodInfo MSLocalName = typeof(XsltFunctions).GetMethod("MSLocalName");

	public static readonly MethodInfo MSNamespaceUri = typeof(XsltFunctions).GetMethod("MSNamespaceUri");

	public static readonly MethodInfo EXslObjectType = typeof(XsltFunctions).GetMethod("EXslObjectType");

	public static readonly MethodInfo CheckScriptNamespace = typeof(XsltLibrary).GetMethod("CheckScriptNamespace");

	public static readonly MethodInfo FunctionAvailable = GetFunctionAvailableMethod();

	public static readonly MethodInfo ElementAvailable = typeof(XsltLibrary).GetMethod("ElementAvailable");

	public static readonly MethodInfo RegisterDecimalFormat = typeof(XsltLibrary).GetMethod("RegisterDecimalFormat");

	public static readonly MethodInfo RegisterDecimalFormatter = typeof(XsltLibrary).GetMethod("RegisterDecimalFormatter");

	public static readonly MethodInfo FormatNumberStatic = typeof(XsltLibrary).GetMethod("FormatNumberStatic");

	public static readonly MethodInfo FormatNumberDynamic = typeof(XsltLibrary).GetMethod("FormatNumberDynamic");

	public static readonly MethodInfo IsSameNodeSort = typeof(XsltLibrary).GetMethod("IsSameNodeSort");

	public static readonly MethodInfo LangToLcid = typeof(XsltLibrary).GetMethod("LangToLcid");

	public static readonly MethodInfo NumberFormat = typeof(XsltLibrary).GetMethod("NumberFormat");

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Supressing warning about not having the RequiresUnreferencedCode attribute since this code path will only be emitting IL that will later be called by Transform() method which is already annotated as RequiresUnreferencedCode")]
	private static MethodInfo GetFunctionAvailableMethod()
	{
		return typeof(XsltLibrary).GetMethod("FunctionAvailable");
	}
}
