using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Xslt;

internal sealed class Scripts
{
	internal sealed class TrimSafeDictionary
	{
		private readonly Dictionary<string, Type> _backingDictionary = new Dictionary<string, Type>();

		public Type this[string key]
		{
			[UnconditionalSuppressMessage("TrimAnalysis", "IL2073:MissingDynamicallyAccessedMembers", Justification = "The getter of the dictionary is not annotated to preserve the constructor, but the sources that are adding the items to the dictionary are annotated so we can supress the message as we know the constructor will be preserved.")]
			[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			get
			{
				return _backingDictionary[key];
			}
			[param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			set
			{
				_backingDictionary[key] = value;
			}
		}

		public ICollection<string> Keys => _backingDictionary.Keys;

		public int Count => _backingDictionary.Count;

		public bool ContainsKey(string key)
		{
			return _backingDictionary.ContainsKey(key);
		}

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out Type value)
		{
			return _backingDictionary.TryGetValue(key, out value);
		}
	}

	private readonly Compiler _compiler;

	private readonly TrimSafeDictionary _nsToType = new TrimSafeDictionary();

	private readonly XmlExtensionFunctionTable _extFuncs = new XmlExtensionFunctionTable();

	public TrimSafeDictionary ScriptClasses => _nsToType;

	public Scripts(Compiler compiler)
	{
		_compiler = compiler;
	}

	[RequiresUnreferencedCode("The extension function referenced will be called from the stylesheet which cannot be statically analyzed.")]
	public XmlExtensionFunction ResolveFunction(string name, string ns, int numArgs, IErrorHelper errorHelper)
	{
		if (_nsToType.TryGetValue(ns, out var value))
		{
			try
			{
				return _extFuncs.Bind(name, ns, numArgs, value, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			}
			catch (XslTransformException ex)
			{
				errorHelper.ReportError(ex.Message);
			}
		}
		return null;
	}
}
