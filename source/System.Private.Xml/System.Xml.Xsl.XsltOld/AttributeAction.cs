using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class AttributeAction : ContainerAction
{
	private Avt _nameAvt;

	private Avt _nsAvt;

	private InputScopeManager _manager;

	private string _name;

	private string _nsUri;

	private PrefixQName _qname;

	private static PrefixQName CreateAttributeQName(string name, string nsUri, InputScopeManager manager)
	{
		if (name == "xmlns")
		{
			return null;
		}
		if (nsUri == "http://www.w3.org/2000/xmlns/")
		{
			throw XsltException.Create(System.SR.Xslt_ReservedNS, nsUri);
		}
		PrefixQName prefixQName = new PrefixQName();
		prefixQName.SetQName(name);
		prefixQName.Namespace = ((nsUri != null) ? nsUri : manager.ResolveXPathNamespace(prefixQName.Prefix));
		if (prefixQName.Prefix.StartsWith("xml", StringComparison.Ordinal))
		{
			if (prefixQName.Prefix.Length == 3)
			{
				if (!(prefixQName.Namespace == "http://www.w3.org/XML/1998/namespace") || (!(prefixQName.Name == "lang") && !(prefixQName.Name == "space")))
				{
					prefixQName.ClearPrefix();
				}
			}
			else if (prefixQName.Prefix == "xmlns")
			{
				if (prefixQName.Namespace == "http://www.w3.org/2000/xmlns/")
				{
					throw XsltException.Create(System.SR.Xslt_InvalidPrefix, prefixQName.Prefix);
				}
				prefixQName.ClearPrefix();
			}
		}
		return prefixQName;
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckRequiredAttribute(compiler, _nameAvt, "name");
		_name = CompiledAction.PrecalculateAvt(ref _nameAvt);
		_nsUri = CompiledAction.PrecalculateAvt(ref _nsAvt);
		if (_nameAvt == null && _nsAvt == null)
		{
			if (_name != "xmlns")
			{
				_qname = CreateAttributeQName(_name, _nsUri, compiler.CloneScopeManager());
			}
		}
		else
		{
			_manager = compiler.CloneScopeManager();
		}
		if (compiler.Recurse())
		{
			CompileTemplate(compiler);
			compiler.ToParent();
		}
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Name))
		{
			_nameAvt = Avt.CompileAvt(compiler, value);
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.Namespace))
			{
				return false;
			}
			_nsAvt = Avt.CompileAvt(compiler, value);
		}
		return true;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			if (_qname != null)
			{
				frame.CalulatedName = _qname;
			}
			else
			{
				frame.CalulatedName = CreateAttributeQName((_nameAvt == null) ? _name : _nameAvt.Evaluate(processor, frame), (_nsAvt == null) ? _nsUri : _nsAvt.Evaluate(processor, frame), _manager);
				if (frame.CalulatedName == null)
				{
					frame.Finished();
					break;
				}
			}
			goto case 2;
		case 2:
		{
			PrefixQName calulatedName = frame.CalulatedName;
			if (!processor.BeginEvent(XPathNodeType.Attribute, calulatedName.Prefix, calulatedName.Name, calulatedName.Namespace, empty: false))
			{
				frame.State = 2;
				break;
			}
			processor.PushActionFrame(frame);
			frame.State = 1;
			break;
		}
		case 1:
			if (!processor.EndEvent(XPathNodeType.Attribute))
			{
				frame.State = 1;
			}
			else
			{
				frame.Finished();
			}
			break;
		}
	}
}
