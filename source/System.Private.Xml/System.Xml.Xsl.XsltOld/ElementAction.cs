using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class ElementAction : ContainerAction
{
	private Avt _nameAvt;

	private Avt _nsAvt;

	private bool _empty;

	private InputScopeManager _manager;

	private string _name;

	private string _nsUri;

	private PrefixQName _qname;

	internal ElementAction()
	{
	}

	private static PrefixQName CreateElementQName(string name, string nsUri, InputScopeManager manager)
	{
		if (nsUri == "http://www.w3.org/2000/xmlns/")
		{
			throw XsltException.Create(System.SR.Xslt_ReservedNS, nsUri);
		}
		PrefixQName prefixQName = new PrefixQName();
		prefixQName.SetQName(name);
		if (nsUri == null)
		{
			prefixQName.Namespace = manager.ResolveXmlNamespace(prefixQName.Prefix);
		}
		else
		{
			prefixQName.Namespace = nsUri;
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
				_qname = CreateElementQName(_name, _nsUri, compiler.CloneScopeManager());
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
		_empty = containedActions == null;
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Name))
		{
			_nameAvt = Avt.CompileAvt(compiler, value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Namespace))
		{
			_nsAvt = Avt.CompileAvt(compiler, value);
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.UseAttributeSets))
			{
				return false;
			}
			AddAction(compiler.CreateUseAttributeSetsAction());
		}
		return true;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		default:
			return;
		case 0:
			if (_qname != null)
			{
				frame.CalulatedName = _qname;
			}
			else
			{
				frame.CalulatedName = CreateElementQName((_nameAvt == null) ? _name : _nameAvt.Evaluate(processor, frame), (_nsAvt == null) ? _nsUri : _nsAvt.Evaluate(processor, frame), _manager);
			}
			goto case 2;
		case 2:
		{
			PrefixQName calulatedName = frame.CalulatedName;
			if (!processor.BeginEvent(XPathNodeType.Element, calulatedName.Prefix, calulatedName.Name, calulatedName.Namespace, _empty))
			{
				frame.State = 2;
				return;
			}
			if (!_empty)
			{
				processor.PushActionFrame(frame);
				frame.State = 1;
				return;
			}
			break;
		}
		case 1:
			break;
		}
		if (!processor.EndEvent(XPathNodeType.Element))
		{
			frame.State = 1;
		}
		else
		{
			frame.Finished();
		}
	}
}
